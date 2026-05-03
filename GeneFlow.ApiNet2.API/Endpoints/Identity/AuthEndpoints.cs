using GeneFlow.ApiNet2.API.Contracts.Identity.Requests;
using GeneFlow.ApiNet2.API.Contracts.Identity.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.API.Routes;
using GeneFlow.ApiNet2.Application.Identity.Commands.Login;
using GeneFlow.ApiNet2.Application.Identity.Commands.Logout;
using GeneFlow.ApiNet2.Application.Identity.Commands.RefreshToken;
using GeneFlow.ApiNet2.Application.Identity.Commands.Register;
using GeneFlow.ApiNet2.Application.Identity.Commands.RequestPasswordReset;
using GeneFlow.ApiNet2.Application.Identity.Commands.RequestTwoFactorCode;
using GeneFlow.ApiNet2.Application.Identity.Commands.ResetPassword;
using GeneFlow.ApiNet2.Application.Identity.Commands.ChangePassword;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Identity;

/// <summary>
/// Authentication endpoints (login, register, refresh, logout).
/// </summary>
public sealed class AuthEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Auth.Base)
            .WithTags("Authentication")
            .WithOpenApi();

        group.MapPost("/register", Register)
            .WithName("Auth_Register")
            .WithSummary("Register a new user")
            .WithDescription("Creates a new user account. An email verification will be sent.")
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .WithName("Auth_Login")
            .WithSummary("Login user")
            .WithDescription("Authenticates a user with email/username and password. Returns JWT tokens.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshTokens)
            .WithName("Auth_RefreshToken")
            .WithSummary("Refresh access token")
            .WithDescription("Gets a new access token using a valid refresh token.")
            .Produces<AuthTokensResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", Logout)
            .WithName("Auth_Logout")
            .WithSummary("Logout user")
            .WithDescription("Revokes the refresh token, effectively logging out the user.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized);

        group.MapPost("/2fa/request-code", RequestTwoFactorCode)
            .WithName("Auth_RequestTwoFactorCode")
            .WithSummary("Request a new 2FA code")
            .WithDescription("Requests a new two-factor authentication code to be sent via email.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        group.MapPost("/request-password-reset", RequestPasswordReset)
            .WithName("Auth_RequestPasswordReset")
            .WithSummary("Request password reset")
            .WithDescription("Sends a password reset email to the user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        group.MapPost("/reset-password", ResetPassword)
            .WithName("Auth_ResetPassword")
            .WithSummary("Reset password")
            .WithDescription("Resets the user's password using the provided token.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        group.MapPost("/change-password", ChangePassword)
            .WithName("Auth_ChangePassword")
            .WithSummary("Change password")
            .WithDescription("Changes the authenticated user's password. Requires the current password for verification.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.Username,
            request.Password);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var response = result.Value.ToResponse();
        return Results.Created($"{ApiRoutes.Users.Base}/{response.Id}", response);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.Identifier,
            request.Password,
            request.TwoFactorCode);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var loginResult = result.Value;
        return Results.Ok(new LoginResponse(
            loginResult.User.ToResponse(),
            loginResult.Tokens?.ToResponse(),
            loginResult.RequiresTwoFactor));
    }

    private static async Task<IResult> RefreshTokens(
        [FromBody] RefreshTokenRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> Logout(
        [FromBody] LogoutRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> RequestTwoFactorCode(
        [FromBody] RequestTwoFactorCodeRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RequestTwoFactorCodeCommand(request.EmailOrUsername);
        var result = await sender.Send(command, cancellationToken);

        // Always return success to prevent enumeration
        return Results.NoContent();
    }

    private static async Task<IResult> RequestPasswordReset(
        [FromBody] RequestPasswordResetRequest request,
        [FromServices] ISender sender,
        ILogger<AuthEndpoints> logger,
        CancellationToken cancellationToken)
    {
        var command = new RequestPasswordResetCommand(request.Email);
        var result = await sender.Send(command, cancellationToken);

        // Always return success to prevent email enumeration
        if (result.IsFailure)
        {
            logger.LogWarning(
                "Password reset request failed for email {Email}: {Error}",
                request.Email,
                result.Error.Message);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
            return Results.Unauthorized();

        var command = new ChangePasswordCommand(
            currentUserService.UserId,
            request.CurrentPassword,
            request.NewPassword);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
