using Battles.Features.GetBattle;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Battles.Features.JoinBattle;

/// <summary>
/// <c>POST /battles/{battleId}/participants</c> lets a second human
/// join an open battle; <c>POST /battles/{battleId}/bot</c> seats the
/// bot instead. Both answer with the battle's first state.
/// </summary>
internal static class JoinBattleEndpoint
{
    /// <summary>
    /// Maps the endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapJoinBattleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/battles/{battleId:guid}/participants", HandleJoinAsync);
        endpoints.MapPost("/battles/{battleId:guid}/bot", HandleJoinBotAsync);

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

    private static Task<Results<Ok<BattleResponse>, NotFound, ValidationProblem, ProblemHttpResult>> HandleJoinBotAsync(
        Guid battleId,
        JoinBattleService service) =>
        ToResultAsync(() => service.JoinBotAsync(battleId));

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
