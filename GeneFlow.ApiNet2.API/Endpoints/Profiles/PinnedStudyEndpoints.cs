using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.API.Routes;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Profiles.Commands.UpdatePinnedStudies;
using GeneFlow.ApiNet2.Application.Profiles.Queries.GetPinnedStudies;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Profiles;

/// <summary>
/// Endpoints for managing the per-user pinned studies list.
/// </summary>
public sealed class PinnedStudyEndpoints : IEndpoint
{
    /// <summary>
    /// Request body for replacing the pinned studies list.
    /// </summary>
    public sealed record UpdatePinnedStudiesRequest(IReadOnlyList<string> StudyIds);

    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Profiles.Base)
            .WithTags("Profiles")
            .WithOpenApi();

        group.MapGet("/me/pinned", GetMyPinnedStudies)
            .WithName("Profiles_GetMyPinnedStudies")
            .WithSummary("Get current user's pinned studies")
            .WithDescription("Returns the ordered list of studies pinned by the current user.")
            .RequireAuthorization()
            .Produces<IReadOnlyList<PinnedStudyDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/me/pinned", UpdateMyPinnedStudies)
            .WithName("Profiles_UpdateMyPinnedStudies")
            .WithSummary("Replace current user's pinned studies")
            .WithDescription("Replaces the pinned studies set with the supplied ordered list (max 6).")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/{userId}/pinned", GetPinnedStudiesByUserId)
            .WithName("Profiles_GetPinnedStudiesByUserId")
            .WithSummary("Get a user's pinned studies")
            .WithDescription("Returns the ordered list of studies pinned by the specified user (public).")
            .Produces<IReadOnlyList<PinnedStudyDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetMyPinnedStudies(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetPinnedStudiesQuery(currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure
            ? result.ToHttpResult()
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateMyPinnedStudies(
        [FromBody] UpdatePinnedStudiesRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new UpdatePinnedStudiesCommand(
            currentUser.UserId.ToString()!,
            request.StudyIds ?? Array.Empty<string>());

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToHttpResult()
            : Results.NoContent();
    }

    private static async Task<IResult> GetPinnedStudiesByUserId(
        [FromRoute] string userId,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetPinnedStudiesQuery(userId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure
            ? result.ToHttpResult()
            : Results.Ok(result.Value);
    }
}
