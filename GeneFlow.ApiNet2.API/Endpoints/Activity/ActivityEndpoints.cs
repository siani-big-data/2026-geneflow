using GeneFlow.ApiNet2.API.Contracts.Activity;
using GeneFlow.ApiNet2.API.Contracts.Activity.Responses;
using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Activity.Queries.GetMyActivityFeed;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Activity;

/// <summary>
/// Activity feed endpoints.
/// </summary>
public sealed class ActivityEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/activity")
            .WithTags("Activity")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/me", GetMyActivityFeed)
            .WithName("Activity_GetMyFeed")
            .WithSummary("Get my activity feed")
            .WithDescription(
                "Returns events authored by the current user, public events, "
                + "and study-scoped events for studies the user can see. "
                + "Paginated with an opaque cursor.")
            .Produces<CursorPagedResponse<ActivityEventResponse>>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetMyActivityFeed(
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetMyActivityFeedQuery(
            currentUser.UserId.ToString()!,
            limit ?? GetMyActivityFeedQueryHandler.DefaultLimit,
            cursor);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToCursorPagedResponse(dto => dto.ToResponse()));
    }
}
