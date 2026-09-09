using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Discussions.Requests;
using GeneFlow.ApiNet2.API.Contracts.Discussions.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Discussions.Commands.CreateDiscussion;
using GeneFlow.ApiNet2.Application.Discussions.Commands.LockDiscussion;
using GeneFlow.ApiNet2.Application.Discussions.Queries.GetDiscussion;
using GeneFlow.ApiNet2.Application.Discussions.Queries.GetStudyDiscussions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Discussions;

/// <summary>
/// Discussion endpoints. Discussions live under a study and own a thread
/// of comments. Comments and reactions are handled by sibling endpoints.
/// </summary>
public sealed class DiscussionEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Study-scoped routes (list + create).
        var studyGroup = app.MapGroup("/api/v1/studies/{studyId}/discussions")
            .WithTags("Discussions")
            .WithOpenApi()
            .RequireAuthorization();

        studyGroup.MapGet("/", GetStudyDiscussions)
            .WithName("Discussions_GetByStudy")
            .WithSummary("List study discussions")
            .WithDescription("Returns a paginated list of discussions for the given study.")
            .Produces<PagedResponse<DiscussionResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden);

        studyGroup.MapPost("/", CreateDiscussion)
            .WithName("Discussions_Create")
            .WithSummary("Create a study discussion")
            .WithDescription("Starts a new discussion in the given study with an initial comment body.")
            .Produces<DiscussionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden);

        // Discussion-scoped routes (detail + lock).
        var discussionGroup = app.MapGroup("/api/v1/discussions")
            .WithTags("Discussions")
            .WithOpenApi()
            .RequireAuthorization();

        discussionGroup.MapGet("/{discussionId}", GetDiscussion)
            .WithName("Discussions_GetById")
            .WithSummary("Get a discussion with comments")
            .WithDescription("Returns a discussion plus its non-deleted comments and reaction summaries.")
            .Produces<DiscussionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        discussionGroup.MapPost("/{discussionId}/lock", LockDiscussion)
            .WithName("Discussions_Lock")
            .WithSummary("Lock or unlock a discussion")
            .WithDescription("Toggles the locked state of a discussion. Admin only.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetStudyDiscussions(
        [FromRoute] string studyId,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromQuery] string? searchTerm,
        [FromQuery] string? category,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetStudyDiscussionsQuery(
            studyId,
            currentUser.UserId.ToString(),
            pageNumber > 0 ? pageNumber : 1,
            pageSize > 0 ? pageSize : PagedRequest.DefaultPageSize,
            searchTerm,
            category);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToPagedResponse(dto => dto.ToResponse()));
    }

    private static async Task<IResult> CreateDiscussion(
        [FromRoute] string studyId,
        [FromBody] CreateDiscussionRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new CreateDiscussionCommand(
            studyId,
            currentUser.UserId.ToString()!,
            request.Title,
            request.Category,
            request.FirstCommentBody);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var response = result.Value.ToResponse();
        return Results.Created($"/api/v1/discussions/{response.Id}", response);
    }

    private static async Task<IResult> GetDiscussion(
        [FromRoute] string discussionId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var query = new GetDiscussionQuery(discussionId, currentUser.UserId?.ToString());
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// The LockDiscussion command requires both StudyId (for the membership
    /// guard) and DiscussionId. We fetch the discussion first via the query
    /// to find its StudyId before issuing the lock command.
    /// </summary>
    private static async Task<IResult> LockDiscussion(
        [FromRoute] string discussionId,
        [FromBody] LockDiscussionRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var userIdString = currentUser.UserId.ToString()!;

        var detail = await sender.Send(new GetDiscussionQuery(discussionId, userIdString), cancellationToken);
        if (detail.IsFailure)
            return detail.ToHttpResult();

        var command = new LockDiscussionCommand(
            detail.Value.StudyId,
            discussionId,
            userIdString,
            request.Lock);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
