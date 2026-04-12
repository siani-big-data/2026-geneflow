using GeneFlow.ApiNet2.API.Contracts.Studies.Requests;
using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Studies.Commands.AddStudyMember;
using GeneFlow.ApiNet2.Application.Studies.Commands.ChangeMemberRole;
using GeneFlow.ApiNet2.Application.Studies.Commands.LeaveStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.RemoveStudyMember;
using GeneFlow.ApiNet2.Application.Studies.Commands.TransferOwnership;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyMembers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Studies;

/// <summary>
/// Study member management endpoints.
/// </summary>
public sealed class StudyMemberEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/studies/{studyId}/members")
            .WithTags("Study Members")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", GetStudyMembers)
            .WithName("StudyMembers_Get")
            .WithSummary("Get study members")
            .WithDescription("Returns the list of members in a study.")
            .Produces<IReadOnlyList<StudyMemberResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/", AddStudyMember)
            .WithName("StudyMembers_Add")
            .WithSummary("Add a member to a study")
            .WithDescription("Adds a new member to the study with the specified role.")
            .Produces<IReadOnlyList<StudyMemberResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        group.MapDelete("/{userId}", RemoveStudyMember)
            .WithName("StudyMembers_Remove")
            .WithSummary("Remove a member from a study")
            .WithDescription("Removes a member from the study.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPatch("/{userId}/role", ChangeMemberRole)
            .WithName("StudyMembers_ChangeRole")
            .WithSummary("Change member role")
            .WithDescription("Changes the role of a study member.")
            .Produces<IReadOnlyList<StudyMemberResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/leave", LeaveStudy)
            .WithName("StudyMembers_Leave")
            .WithSummary("Leave a study")
            .WithDescription("Allows the current user to leave a study.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/transfer-ownership", TransferOwnership)
            .WithName("StudyMembers_TransferOwnership")
            .WithSummary("Transfer study ownership")
            .WithDescription("Transfers ownership of the study to another admin member.")
            .Produces<StudyResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetStudyMembers(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetStudyMembersQuery(studyId, currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponses());
    }

    private static async Task<IResult> AddStudyMember(
        [FromRoute] string studyId,
        [FromBody] AddStudyMemberRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new AddStudyMemberCommand(
            studyId,
            currentUser.UserId.ToString()!,
            request.UserId,
            request.RoleId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> RemoveStudyMember(
        [FromRoute] string studyId,
        [FromRoute] string userId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new RemoveStudyMemberCommand(
            studyId,
            currentUser.UserId.ToString()!,
            userId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> ChangeMemberRole(
        [FromRoute] string studyId,
        [FromRoute] string userId,
        [FromBody] ChangeMemberRoleRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new ChangeMemberRoleCommand(
            studyId,
            currentUser.UserId.ToString()!,
            userId,
            request.NewRoleId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> LeaveStudy(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new LeaveStudyCommand(studyId, currentUser.UserId.ToString()!);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> TransferOwnership(
        [FromRoute] string studyId,
        [FromBody] TransferOwnershipRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new TransferOwnershipCommand(
            studyId,
            currentUser.UserId.ToString()!,
            request.NewOwnerId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }
}
