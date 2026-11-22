using Battles.Features.GetBattle;
using Battles.Features.StartBattle;
using Battles.Grains.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Battles.Features.JoinBattle;

/// <summary>
/// <c>POST /battles/{battleId}/participants</c> lets a second human
/// join an open battle; <c>POST /battles/{battleId}/bot</c> seats the
/// bot instead, on the difficulty its optional <c>difficulty</c> query
/// parameter names (<c>Normal</c> without one, ADR-0013). Both answer
/// with the battle's first state.
/// </summary>
internal static class JoinBattleEndpoint
{
    /// <summary>
    /// Maps the endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapJoinBattleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/battles/{battleId:guid}/participants", HandleJoinAsync)
            .ProducesProblem(StatusCodes.Status409Conflict);
        endpoints.MapPost("/battles/{battleId:guid}/bot", HandleJoinBotAsync)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<Results<Ok<BattleResponse>, NotFound, ValidationProblem, ProblemHttpResult>> HandleJoinAsync(
        Guid battleId,
        JoinBattleRequest request,
        JoinBattleService service)
    {
        if (request.Validate() is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        return await ToResultAsync(() => service.JoinAsync(battleId, request));
    }

    private static async Task<Results<Ok<BattleResponse>, NotFound, ValidationProblem, ProblemHttpResult>> HandleJoinBotAsync(
        Guid battleId,
        JoinBattleService service,
        Difficulty? difficulty)
    {
        // Optional and without a default in the signature: the OpenAPI
        // document would show the default as a number.
        var chosen = difficulty ?? Difficulty.Normal;
        if (StartBattleRequest.ValidateDifficulty(chosen) is { } error)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(difficulty)] = [error],
            });
        }

        return await ToResultAsync(() => service.JoinBotAsync(battleId, chosen));
    }

    private static async Task<Results<Ok<BattleResponse>, NotFound, ValidationProblem, ProblemHttpResult>> ToResultAsync(
        Func<Task<BattleResponse?>> join)
    {
        try
        {
            return await join() is { } battle
                ? TypedResults.Ok(battle)
                : TypedResults.NotFound();
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException exception)
        {
            return TypedResults.Problem(exception.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
