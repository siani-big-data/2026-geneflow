using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Orgs.Requests;
using GeneFlow.ApiNet2.API.Contracts.Orgs.Responses;
using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Commands.CreateOrg;
using GeneFlow.ApiNet2.Application.Orgs.Commands.UpdateOrgProfile;
using GeneFlow.ApiNet2.Application.Orgs.Queries.GetOrgByHandle;
using GeneFlow.ApiNet2.Application.Orgs.Queries.ListMyOrgs;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetOrgStudies;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Orgs;

/// <summary>
/// REST endpoints for the <see cref="Org"/> aggregate: create, lookup
/// by handle, list-mine and profile update. Members and invitations
/// live in sibling endpoint classes.
/// </summary>
public sealed class OrgEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var orgs = app.MapGroup("/api/v1/orgs")
            .WithTags("Orgs")
            .WithOpenApi()
            .RequireAuthorization();

        orgs.MapPost("/", CreateOrg)
            .WithName("Orgs_Create")
            .WithSummary("Create a new organisation")
            .Produces<OrgResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        orgs.MapGet("/{handle}", GetByHandle)
            .WithName("Orgs_GetByHandle")
            .WithSummary("Get an organisation by handle")
            .Produces<OrgResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        orgs.MapGet("/{handle}/studies", ListOrgStudies)
            .WithName("Orgs_ListStudies")
            .WithSummary("List studies owned by an organisation")
            .Produces<PagedResponse<StudyResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        orgs.MapPut("/{handle}", UpdateProfile)
            .WithName("Orgs_UpdateProfile")
            .WithSummary("Update an organisation's profile")
            .Produces<OrgResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        // "Me"-scoped routes — the FE service hits /api/v1/me/orgs
        // (not /api/v1/orgs/me) so we register the path explicitly.
        var me = app.MapGroup("/api/v1/me")
            .WithTags("Orgs")
            .WithOpenApi()
            .RequireAuthorization();

        me.MapGet("/orgs", ListMyOrgs)
            .WithName("Orgs_ListMine")
            .WithSummary("List orgs the current user belongs to")
            .Produces<IReadOnlyList<OrgMembershipResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> CreateOrg(
        [FromBody] CreateOrgRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new CreateOrgCommand(
            request.Handle,
            request.Name,
            request.Description,
            request.Visibility);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var response = result.Value.ToResponse();
        return Results.Created($"/api/v1/orgs/{response.Handle}", response);
    }

    private static async Task<IResult> GetByHandle(
        [FromRoute] string handle,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrgByHandleQuery(handle), cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// The FE addresses orgs by handle, so this endpoint accepts a handle
    /// and resolves it to the OrgId server-side before issuing the command.
    /// </summary>
    private static async Task<IResult> UpdateProfile(
        [FromRoute] string handle,
        [FromBody] UpdateOrgProfileRequest request,
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

        var command = new UpdateOrgProfileCommand(
            org.Id.ToString(),
            request.Name,
            request.Description,
            request.AvatarUrl,
            request.WebsiteUrl,
            request.Location);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> ListOrgStudies(
        [FromRoute] string handle,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetOrgStudiesQuery(
            handle,
            pageNumber > 0 ? pageNumber : 1,
            pageSize > 0 ? pageSize : PagedRequest.DefaultPageSize);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToPagedResponse(dto => dto.ToResponse()));
    }

    private static async Task<IResult> ListMyOrgs(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(new ListMyOrgsQuery(), cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.Select(dto => dto.ToResponse()).ToList());
    }
}
