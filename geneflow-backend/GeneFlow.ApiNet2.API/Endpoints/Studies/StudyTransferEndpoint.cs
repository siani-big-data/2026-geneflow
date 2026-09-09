using GeneFlow.ApiNet2.API.Contracts.Orgs.Requests;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Commands.TransferStudyOwnership;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Studies;

/// <summary>
/// REST endpoint for transferring a study between a User owner and an Org
/// owner. Kept in its own class because the transfer command lives in the
/// Orgs application module, while routing it under <c>/api/v1/studies</c>
/// matches the FE service contract.
/// </summary>
public sealed class StudyTransferEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/studies/{studyId}")
            .WithTags("Studies")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/transfer", TransferOwnership)
            .WithName("Studies_TransferOwnership")
            .WithSummary("Transfer ownership of a study to a User or Org")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> TransferOwnership(
        [FromRoute] string studyId,
        [FromBody] TransferStudyOwnershipRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new TransferStudyOwnershipCommand(
            studyId,
            request.NewOwnerType,
            request.NewOwnerId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
