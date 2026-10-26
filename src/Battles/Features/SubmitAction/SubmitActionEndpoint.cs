using Battles.Features.GetBattle;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Battles.Features.SubmitAction;

/// <summary>
/// <c>POST /battles/{battleId}/actions</c>: submits a turn and
/// answers with the battle's state afterwards.
/// </summary>
internal static class SubmitActionEndpoint
{
    /// <summary>
    /// Maps the endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapSubmitActionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/battles/{battleId:guid}/actions", HandleAsync);

        return endpoints;
    }

    private static async Task<Results<Ok<BattleResponse>, NotFound, ProblemHttpResult>> HandleAsync(
        Guid battleId,
        SubmitActionRequest request,
        SubmitActionService service)
    {
        try
        {
            return await service.SubmitAsync(battleId, request) is { } battle
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
