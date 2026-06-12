using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Studies.Requests;
using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Studies.Commands.AcceptInvitation;
using GeneFlow.ApiNet2.Application.Studies.Commands.CancelInvitation;
using GeneFlow.ApiNet2.Application.Studies.Commands.DeclineInvitation;
using GeneFlow.ApiNet2.Application.Studies.Commands.ResendInvitation;
using GeneFlow.ApiNet2.Application.Studies.Commands.SendInvitation;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetInvitationByToken;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetMyInvitations;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyInvitations;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Studies;

/// <summary>
/// Study invitation management endpoints.
/// </summary>
public sealed class StudyInvitationEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Study-scoped invitation endpoints
        var studyGroup = app.MapGroup("/api/v1/studies/{studyId}/invitations")
            .WithTags("Study Invitations")
            .WithOpenApi()
            .RequireAuthorization();

        studyGroup.MapPost("/", SendInvitation)
            .WithName("StudyInvitations_Send")
            .WithSummary("Send a study invitation")
            .WithDescription("Sends an invitation to join the study.")
            .Produces<StudyInvitationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        studyGroup.MapGet("/", GetStudyInvitations)
            .WithName("StudyInvitations_GetByStudy")
            .WithSummary("Get study invitations")
            .WithDescription("Returns all invitations for a study.")
            .Produces<PagedResponse<StudyInvitationResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        studyGroup.MapDelete("/{invitationId}", CancelInvitation)
            .WithName("StudyInvitations_Cancel")
            .WithSummary("Cancel an invitation")
            .WithDescription("Cancels a pending invitation.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        studyGroup.MapPost("/{invitationId}/resend", ResendInvitation)
            .WithName("StudyInvitations_Resend")
            .WithSummary("Resend an invitation")
            .WithDescription("Resends an invitation with a new token and expiration.")
            .Produces<StudyInvitationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // User-scoped invitation endpoints
        var userGroup = app.MapGroup("/api/v1/invitations")
            .WithTags("Study Invitations")
            .WithOpenApi()
            .RequireAuthorization();

        userGroup.MapGet("/", GetMyInvitations)
            .WithName("StudyInvitations_GetMine")
            .WithSummary("Get my pending invitations")
            .WithDescription("Returns all pending invitations for the current user's email.")
            .Produces<PagedResponse<StudyInvitationResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        userGroup.MapGet("/{token}", GetInvitationByToken)
            .WithName("StudyInvitations_GetByToken")
            .WithSummary("Get invitation by token")
            .WithDescription("Returns invitation details by its token.")
            .Produces<StudyInvitationResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        userGroup.MapPost("/{token}/accept", AcceptInvitation)
            .WithName("StudyInvitations_Accept")
            .WithSummary("Accept an invitation")
            .WithDescription("Accepts a pending invitation to join a study.")
            .Produces<StudyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        userGroup.MapPost("/{token}/decline", DeclineInvitation)
            .WithName("StudyInvitations_Decline")
            .WithSummary("Decline an invitation")
            .WithDescription("Declines a pending invitation.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> SendInvitation(
        [FromRoute] string studyId,
        [FromBody] SendInvitationRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        if (Email.Create(request.Email).IsFailure)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["Email address format is invalid."]
            });
        }

        var command = new SendInvitationCommand(
            studyId,
            currentUser.UserId.ToString()!,
            request.Email,
            request.RoleId,
            request.Message);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Created($"/api/v1/invitations/{result.Value.Id}", result.Value.ToResponse());
    }

    private static async Task<IResult> GetStudyInvitations(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetStudyInvitationsQuery(
            studyId,
            currentUser.UserId.ToString()!,
            pageNumber > 0 ? pageNumber : 1,
            pageSize > 0 ? pageSize : PagedRequest.DefaultPageSize);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToPagedResponse(dto => dto.ToResponse()));
    }

    private static async Task<IResult> CancelInvitation(
        [FromRoute] string studyId,
        [FromRoute] string invitationId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new CancelInvitationCommand(
            studyId,
            invitationId,
            currentUser.UserId.ToString()!);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> ResendInvitation(
        [FromRoute] string studyId,
        [FromRoute] string invitationId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new ResendInvitationCommand(
            studyId,
            invitationId,
            currentUser.UserId.ToString()!);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> GetMyInvitations(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IUserRepository userRepository,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        // Always resolve the email server-side: accepting it from the query
        // string would let any authenticated user read another user's invitations.
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
            return Results.Unauthorized();

        var query = new GetMyInvitationsQuery(
            user.Email.Value,
            pageNumber > 0 ? pageNumber : 1,
            pageSize > 0 ? pageSize : PagedRequest.DefaultPageSize);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToPagedResponse(dto => dto.ToResponse()));
    }

    private static async Task<IResult> GetInvitationByToken(
        [FromRoute] string token,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetInvitationByTokenQuery(token);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> AcceptInvitation(
        [FromRoute] string token,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new AcceptInvitationCommand(token, currentUser.UserId.ToString()!);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> DeclineInvitation(
        [FromRoute] string token,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new DeclineInvitationCommand(token);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
