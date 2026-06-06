using GeneFlow.ApiNet2.API.Contracts.Orgs.Requests;
using GeneFlow.ApiNet2.API.Contracts.Orgs.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Commands.AcceptOrgInvitation;
using GeneFlow.ApiNet2.Application.Orgs.Commands.DeclineOrgInvitation;
using GeneFlow.ApiNet2.Application.Orgs.Commands.InviteOrgMember;
using GeneFlow.ApiNet2.Application.Orgs.Queries.ListMyInvitations;
using GeneFlow.ApiNet2.Domain.Orgs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Orgs;

/// <summary>
/// REST endpoints for org invitations. The invite-by-org route uses the
/// org handle; the accept/decline routes use the opaque invitation token
/// (so a recipient who is not yet authenticated against the SPA can still
/// follow the link from email).
/// </summary>
public sealed class OrgInvitationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var orgInvites = app.MapGroup("/api/v1/orgs/{handle}/invitations")
            .WithTags("OrgInvitations")
            .WithOpenApi()
            .RequireAuthorization();

        orgInvites.MapPost("/", Invite)
            .WithName("OrgInvitations_Invite")
            .WithSummary("Invite a user to join an org by email")
            .Produces<OrgInvitationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        var tokenInvites = app.MapGroup("/api/v1/invitations")
            .WithTags("OrgInvitations")
            .WithOpenApi()
            .RequireAuthorization();

        tokenInvites.MapPost("/{token}/accept", Accept)
            .WithName("OrgInvitations_Accept")
            .WithSummary("Accept an invitation by token")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        tokenInvites.MapPost("/{token}/decline", Decline)
            .WithName("OrgInvitations_Decline")
            .WithSummary("Decline an invitation by token")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        var me = app.MapGroup("/api/v1/me")
            .WithTags("OrgInvitations")
            .WithOpenApi()
            .RequireAuthorization();

        me.MapGet("/invitations", ListMine)
            .WithName("OrgInvitations_ListMine")
            .WithSummary("List the current user's pending invitations")
            .Produces<IReadOnlyList<OrgInvitationResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Invite(
        [FromRoute] string handle,
        [FromBody] InviteOrgMemberRequest request,
        [FromServices] ISender sender,
        [FromServices] IOrgRepository orgRepository,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var org = await orgRepository.GetByHandleAsync(handle, cancellationToken);
        if (org is null)
            return OrgErrors.NotFound.ToApiResult();

        var command = new InviteOrgMemberCommand(
            org.Id.ToString(),
            request.Email,
            request.Role);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var response = result.Value.ToResponse();
        return Results.Created($"/api/v1/invitations/{response.Id}", response);
    }

    private static async Task<IResult> Accept(
        [FromRoute] string token,
        [FromServices] ISender sender,
        [FromServices] IOrgInvitationRepository invitationRepository,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var invitation = await invitationRepository.GetByTokenAsync(token, cancellationToken);
        if (invitation is null)
            return OrgErrors.OrgInvitation.NotFound.ToApiResult();

        var result = await sender.Send(
            new AcceptOrgInvitationCommand(invitation.Id.ToString()),
            cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> Decline(
        [FromRoute] string token,
        [FromServices] ISender sender,
        [FromServices] IOrgInvitationRepository invitationRepository,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var invitation = await invitationRepository.GetByTokenAsync(token, cancellationToken);
        if (invitation is null)
            return OrgErrors.OrgInvitation.NotFound.ToApiResult();

        var result = await sender.Send(
            new DeclineOrgInvitationCommand(invitation.Id.ToString()),
            cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> ListMine(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(new ListMyInvitationsQuery(), cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.Select(dto => dto.ToResponse()).ToList());
    }
}
