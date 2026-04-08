using GeneFlow.ApiNet2.API.Contracts.Subscriptions.Requests;
using GeneFlow.ApiNet2.API.Contracts.Subscriptions.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Subscriptions.Commands.CancelSubscription;
using GeneFlow.ApiNet2.Application.Subscriptions.Commands.ChangePlan;
using GeneFlow.ApiNet2.Application.Subscriptions.Commands.CreateSubscription;
using GeneFlow.ApiNet2.Application.Subscriptions.Queries.GetCurrentSubscription;
using GeneFlow.ApiNet2.Application.Subscriptions.Queries.GetSubscriptionHistory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Subscriptions;

/// <summary>
/// Subscription management endpoints.
/// </summary>
public sealed class SubscriptionEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/subscriptions")
            .WithTags("Subscriptions")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/current", GetCurrentSubscription)
            .WithName("Subscriptions_GetCurrent")
            .WithSummary("Get current subscription")
            .WithDescription("Returns the current user's active subscription.")
            .Produces<SubscriptionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapGet("/history", GetSubscriptionHistory)
            .WithName("Subscriptions_GetHistory")
            .WithSummary("Get subscription history")
            .WithDescription("Returns the user's subscription history.")
            .Produces<IReadOnlyList<SubscriptionSummaryResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", CreateSubscription)
            .WithName("Subscriptions_Create")
            .WithSummary("Create a new subscription")
            .WithDescription("Creates a new subscription for the current user.")
            .Produces<SubscriptionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        group.MapPost("/cancel", CancelSubscription)
            .WithName("Subscriptions_Cancel")
            .WithSummary("Cancel subscription")
            .WithDescription("Cancels the current user's subscription.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/change-plan", ChangePlan)
            .WithName("Subscriptions_ChangePlan")
            .WithSummary("Change subscription plan")
            .WithDescription("Changes the current user's subscription to a different plan.")
            .Produces<SubscriptionResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetCurrentSubscription(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetCurrentSubscriptionQuery(currentUser.UserId.Value.ToString());
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> GetSubscriptionHistory(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetSubscriptionHistoryQuery(currentUser.UserId.Value.ToString());
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponses());
    }

    private static async Task<IResult> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new CreateSubscriptionCommand(
            currentUser.UserId.Value.ToString(),
            request.PlanId.ToString(),
            request.BillingCycleId,
            request.StartWithTrial);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Created($"/api/v1/subscriptions/{result.Value.SubscriptionId}", result.Value.ToResponse());
    }

    private static async Task<IResult> CancelSubscription(
        [FromBody] CancelSubscriptionRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new CancelSubscriptionCommand(
            currentUser.UserId.Value.ToString(),
            request.Reason);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> ChangePlan(
        [FromBody] ChangePlanRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new ChangePlanCommand(
            currentUser.UserId.Value.ToString(),
            request.NewPlanId.ToString(),
            request.BillingCycleId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }
}
