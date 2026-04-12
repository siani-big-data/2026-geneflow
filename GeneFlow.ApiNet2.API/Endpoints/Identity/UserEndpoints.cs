using GeneFlow.ApiNet2.API.Contracts.Identity.Requests;
using GeneFlow.ApiNet2.API.Contracts.Identity.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.API.Routes;
using GeneFlow.ApiNet2.Application.Identity.Commands.ConfirmTwoFactorSetup;
using GeneFlow.ApiNet2.Application.Identity.Commands.DisableTwoFactor;
using GeneFlow.ApiNet2.Application.Identity.Commands.EnableTwoFactor;
using GeneFlow.ApiNet2.Application.Identity.Commands.LinkExternalLogin;
using GeneFlow.ApiNet2.Application.Identity.Commands.SetupTwoFactor;
using GeneFlow.ApiNet2.Application.Identity.Commands.UnlinkExternalLogin;
using GeneFlow.ApiNet2.Application.Identity.Commands.VerifyEmail;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Queries.GetCurrentUser;
using GeneFlow.ApiNet2.Application.Identity.Queries.GetUserById;
using GeneFlow.ApiNet2.Application.Identity.Queries.GetUserExternalLogins;
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

        // TOTP (Authenticator App) endpoints
        twoFactorGroup.MapGet("/setup", SetupTwoFactor)
            .WithName("Users_SetupTwoFactor")
            .WithSummary("Setup TOTP-based two-factor authentication")
            .WithDescription("Generates a secret and QR code URI for authenticator app setup.")
            .Produces<TwoFactorSetupResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        twoFactorGroup.MapPost("/confirm", ConfirmTwoFactorSetup)
            .WithName("Users_ConfirmTwoFactorSetup")
            .WithSummary("Confirm TOTP-based two-factor authentication setup")
            .WithDescription("Validates the code from the authenticator app and enables TOTP 2FA.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status401Unauthorized);

        // External Login Management
        var externalLoginsGroup = group.MapGroup("/external-logins")
            .RequireAuthorization();

        externalLoginsGroup.MapGet("/", GetExternalLogins)
            .WithName("Users_GetExternalLogins")
            .WithSummary("Get linked external logins")
            .WithDescription("Returns the list of OAuth providers linked to the current user.")
            .Produces<IReadOnlyList<ExternalLoginResponse>>(StatusCodes.Status200OK);

        externalLoginsGroup.MapPost("/", LinkExternalLogin)
            .WithName("Users_LinkExternalLogin")
            .WithSummary("Link an external OAuth login")
            .WithDescription("Links a new OAuth provider to the current user's account.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        externalLoginsGroup.MapDelete("/{provider}", UnlinkExternalLogin)
            .WithName("Users_UnlinkExternalLogin")
            .WithSummary("Unlink an external OAuth login")
            .WithDescription("Removes an OAuth provider link from the current user. " +
                             "Cannot unlink if it's the only authentication method.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
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

        var command = new EnableTwoFactorCommand(currentUser.UserId.ToString()!);
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

        var command = new DisableTwoFactorCommand(currentUser.UserId.ToString()!);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> SetupTwoFactor(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new SetupTwoFactorCommand(currentUser.UserId.ToString()!);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(new TwoFactorSetupResponse(result.Value.Secret, result.Value.QrCodeUri));
    }

    private static async Task<IResult> ConfirmTwoFactorSetup(
        [FromBody] ConfirmTwoFactorSetupRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new ConfirmTwoFactorSetupCommand(
            currentUser.UserId.ToString()!,
            request.Secret,
            request.Code);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> GetExternalLogins(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetUserExternalLoginsQuery(currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var response = result.Value.Select(e => new ExternalLoginResponse
        {
            Provider = e.Provider,
            DisplayName = e.DisplayName,
            LinkedAt = e.LinkedAt
        }).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> LinkExternalLogin(
        [FromBody] LinkExternalLoginRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new LinkExternalLoginCommand(
            currentUser.UserId.ToString()!,
            request.Provider,
            request.Token);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> UnlinkExternalLogin(
        [FromRoute] string provider,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new UnlinkExternalLoginCommand(
            currentUser.UserId.ToString()!,
            provider);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
