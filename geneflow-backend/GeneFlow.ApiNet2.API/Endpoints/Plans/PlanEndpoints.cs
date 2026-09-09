using GeneFlow.ApiNet2.API.Contracts.Plans.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Plans.Queries.GetAllPlans;
using GeneFlow.ApiNet2.Application.Plans.Queries.GetPlanById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Plans;

/// <summary>
/// Plan management endpoints.
/// </summary>
public sealed class PlanEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/plans")
            .WithTags("Plans")
            .WithOpenApi();

        group.MapGet("/", GetAllPlans)
            .WithName("Plans_GetAll")
            .WithSummary("Get all active plans")
            .WithDescription("Returns all active subscription plans.")
            .Produces<IReadOnlyList<PlanResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{planId}", GetPlanById)
            .WithName("Plans_GetById")
            .WithSummary("Get plan by ID")
            .WithDescription("Returns a specific plan by its ID.")
            .Produces<PlanResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllPlans(
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetAllPlansQuery();
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponses());
    }

    private static async Task<IResult> GetPlanById(
        [FromRoute] string planId,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetPlanByIdQuery(planId);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }
}
