using GeneFlow.ApiNet2.API.Contracts.PaymentMethods.Requests;
using GeneFlow.ApiNet2.API.Contracts.PaymentMethods.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.PaymentMethods.Commands.AddPaymentMethod;
using GeneFlow.ApiNet2.Application.PaymentMethods.Commands.RemovePaymentMethod;
using GeneFlow.ApiNet2.Application.PaymentMethods.Commands.SetDefaultPaymentMethod;
using GeneFlow.ApiNet2.Application.PaymentMethods.Queries.CreateSetupIntent;
using GeneFlow.ApiNet2.Application.PaymentMethods.Queries.GetDefaultPaymentMethod;
using GeneFlow.ApiNet2.Application.PaymentMethods.Queries.GetPaymentMethods;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.PaymentMethods;

/// <summary>
/// Payment method management endpoints.
/// </summary>
public sealed class PaymentMethodEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payment-methods")
            .WithTags("Payment Methods")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", GetPaymentMethods)
            .WithName("PaymentMethods_GetAll")
            .WithSummary("Get all payment methods")
            .WithDescription("Returns all payment methods for the current user.")
            .Produces<IReadOnlyList<PaymentMethodResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/default", GetDefaultPaymentMethod)
            .WithName("PaymentMethods_GetDefault")
            .WithSummary("Get default payment method")
            .WithDescription("Returns the default payment method for the current user.")
            .Produces<PaymentMethodResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapGet("/setup-intent", CreateSetupIntent)
            .WithName("PaymentMethods_CreateSetupIntent")
            .WithSummary("Create a Stripe SetupIntent")
            .WithDescription("Creates a Stripe SetupIntent for adding a new payment method.")
            .Produces<SetupIntentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/", AddPaymentMethod)
            .WithName("PaymentMethods_Add")
            .WithSummary("Add a payment method")
            .WithDescription("Adds a new payment method after Stripe SetupIntent confirmation.")
            .Produces<PaymentMethodResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        group.MapPost("/{id}/set-default", SetDefaultPaymentMethod)
            .WithName("PaymentMethods_SetDefault")
            .WithSummary("Set default payment method")
            .WithDescription("Sets a payment method as the default.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", RemovePaymentMethod)
            .WithName("PaymentMethods_Remove")
            .WithSummary("Remove a payment method")
            .WithDescription("Removes a payment method.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetPaymentMethods(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetPaymentMethodsQuery(currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var responses = result.Value.Select(pm => new PaymentMethodResponse(
            pm.Id,
            pm.Brand,
            pm.Last4,
            pm.ExpiryMonth,
            pm.ExpiryYear,
            pm.IsDefault,
            pm.CreatedAt
        )).ToList();

        return Results.Ok(responses);
    }

    private static async Task<IResult> GetDefaultPaymentMethod(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetDefaultPaymentMethodQuery(currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        if (result.Value is null)
            return Results.NotFound(new ApiError("PaymentMethod.NotFound", "No default payment method found."));

        var pm = result.Value;
        return Results.Ok(new PaymentMethodResponse(
            pm.Id,
            pm.Brand,
            pm.Last4,
            pm.ExpiryMonth,
            pm.ExpiryYear,
            pm.IsDefault,
            pm.CreatedAt
        ));
    }

    private static async Task<IResult> CreateSetupIntent(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new CreateSetupIntentQuery(currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(new SetupIntentResponse(result.Value.ClientSecret));
    }

    private static async Task<IResult> AddPaymentMethod(
        [FromBody] AddPaymentMethodRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new AddPaymentMethodCommand(
            currentUser.UserId.ToString()!,
            request.StripePaymentMethodId,
            request.SetAsDefault);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var pm = result.Value;
        return Results.Created($"/api/v1/payment-methods/{pm.Id}", new PaymentMethodResponse(
            pm.Id,
            pm.Brand,
            pm.Last4,
            pm.ExpiryMonth,
            pm.ExpiryYear,
            pm.IsDefault,
            pm.CreatedAt
        ));
    }

    private static async Task<IResult> SetDefaultPaymentMethod(
        [FromRoute] string id,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new SetDefaultPaymentMethodCommand(currentUser.UserId.ToString()!, id);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> RemovePaymentMethod(
        [FromRoute] string id,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new RemovePaymentMethodCommand(currentUser.UserId.ToString()!, id);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
