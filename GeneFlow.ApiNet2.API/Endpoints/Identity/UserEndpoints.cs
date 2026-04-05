using GeneFlow.ApiNet2.API.Contracts.Identity.Requests;
using GeneFlow.ApiNet2.API.Contracts.Identity.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.API.Routes;
using GeneFlow.ApiNet2.Application.Identity.Commands.DisableTwoFactor;
using GeneFlow.ApiNet2.Application.Identity.Commands.EnableTwoFactor;
using GeneFlow.ApiNet2.Application.Identity.Commands.VerifyEmail;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Queries.GetCurrentUser;
using GeneFlow.ApiNet2.Application.Identity.Queries.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Identity;

/// <summary>
/// User management endpoints (profile, verification, 2FA).
/// </summary>
public sealed class UserEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Users.Base)
            .WithTags("Users")
            .WithOpenApi();

        // Current user endpoints
        group.MapGet("/me", GetCurrentUser)
            .WithName("Users_GetCurrent")
            .WithSummary("Get current user profile")
            .WithDescription("Returns the profile of the currently authenticated user.")
            .RequireAuthorization()
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id}", GetUserById)
            .WithName("Users_GetById")
            .WithSummary("Get user by ID")
            .WithDescription("Returns user information by their ID. Requires admin privileges.")
            .RequireAuthorization("RequireAdminRole")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Email verification
        group.MapPost("/verify-email", VerifyEmail)
            .WithName("Users_VerifyEmail")
            .WithSummary("Verify email address")
            .WithDescription("Verifies the user's email address using the provided token.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        group.MapGet("/verify-email", VerifyEmailByLink)
            .WithName("Users_VerifyEmailByLink")
            .WithSummary("Verify email address via link")
            .WithDescription("Verifies the user's email address directly from the email link.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest);

        // Two-Factor Authentication
        var twoFactorGroup = group.MapGroup("/2fa")
            .RequireAuthorization();

        twoFactorGroup.MapPost("/enable", EnableTwoFactor)
            .WithName("Users_EnableTwoFactor")
            .WithSummary("Enable two-factor authentication")
            .WithDescription("Enables email-based 2FA for the user.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        twoFactorGroup.MapPost("/disable", DisableTwoFactor)
            .WithName("Users_DisableTwoFactor")
            .WithSummary("Disable two-factor authentication")
            .WithDescription("Disables 2FA for the user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> GetCurrentUser(
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetCurrentUserQuery();
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> GetUserById(
        [FromRoute] string id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new VerifyEmailCommand(request.Token);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> VerifyEmailByLink(
        [FromQuery] string token,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Results.BadRequest(new ApiError("InvalidToken", "Token is required"));

        var command = new VerifyEmailCommand(token);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return Results.BadRequest(new ApiError(result.Error.Code, result.Error.Message));

        return Results.Ok(new { message = "Email verified successfully" });
    }

    private static async Task<IResult> EnableTwoFactor(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new EnableTwoFactorCommand(currentUser.UserId.Value.ToString());
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> DisableTwoFactor(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new DisableTwoFactorCommand(currentUser.UserId.Value.ToString());
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
