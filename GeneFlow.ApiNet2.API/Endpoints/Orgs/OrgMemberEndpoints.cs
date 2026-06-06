using GeneFlow.ApiNet2.API.Contracts.Orgs.Requests;
using GeneFlow.ApiNet2.API.Contracts.Orgs.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Commands.ChangeOrgMemberRole;
using GeneFlow.ApiNet2.Application.Orgs.Commands.RemoveOrgMember;
using GeneFlow.ApiNet2.Application.Orgs.Queries.ListOrgMembers;
using GeneFlow.ApiNet2.Domain.Orgs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Orgs;

/// <summary>
/// REST endpoints for managing the membership of an org. Routes use the
/// org's handle (matching the FE service contract); the handle is
/// resolved to an OrgId server-side before dispatching the command.
/// </summary>
public sealed class OrgMemberEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orgs/{handle}/members")
            .WithTags("Orgs")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", ListMembers)
            .WithName("Orgs_ListMembers")
            .WithSummary("List members of an org")
            .Produces<IReadOnlyList<OrgMemberResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // Frontend uses PATCH for role change; we accept both PATCH and PUT
        // to be lenient with any in-flight clients.
        group.MapMethods("/{userId}", new[] { "PATCH", "PUT" }, ChangeRole)
            .WithName("Orgs_ChangeMemberRole")
            .WithSummary("Change a member's role")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{userId}", RemoveMember)
            .WithName("Orgs_RemoveMember")
            .WithSummary("Remove a member from an org")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListMembers(
        [FromRoute] string handle,
        [FromServices] ISender sender,
        [FromServices] IOrgRepository orgRepository,
        CancellationToken cancellationToken)
    {
        var org = await orgRepository.GetByHandleAsync(handle, cancellationToken);
        if (org is null)
            return OrgErrors.NotFound.ToApiResult();

        var result = await sender.Send(
            new ListOrgMembersQuery(org.Id.ToString()),
            cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.Select(dto => dto.ToResponse()).ToList());
    }

    private static async Task<IResult> ChangeRole(
        [FromRoute] string handle,
        [FromRoute] string userId,
        [FromBody] ChangeOrgMemberRoleRequest request,
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

        var command = new ChangeOrgMemberRoleCommand(
            org.Id.ToString(),
            userId,
            request.Role);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> RemoveMember(
        [FromRoute] string handle,
        [FromRoute] string userId,
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

        var command = new RemoveOrgMemberCommand(org.Id.ToString(), userId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
