using Battles.Features.GetBattle;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Battles.Features.StartBattle;

/// <summary>
/// <c>POST /arenas/{arenaId}/battles</c>: starts a battle against
/// the bot, or opens one for a human, and answers with its first state;
/// 404 if the arena is not in the Catalog.
/// </summary>
internal static class StartBattleEndpoint
{
    /// <summary>
    /// Maps the endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapStartBattleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/arenas/{arenaId}/battles", HandleAsync);

        return endpoints;
    }

    private static async Task<Results<Created<BattleResponse>, NotFound, ValidationProblem, ProblemHttpResult>> HandleAsync(
        string arenaId,
        StartBattleRequest request,
        StartBattleService service)
    {
        if (request.Validate() is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        try
        {
            return await service.StartAsync(arenaId, request) is { } battle
                ? TypedResults.Created($"/battles/{battle.BattleId}", battle)
                : TypedResults.NotFound();
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
