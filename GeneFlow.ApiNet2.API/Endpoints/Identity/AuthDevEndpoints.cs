using GeneFlow.ApiNet2.API.Routes;
using GeneFlow.ApiNet2.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Identity;

/// <summary>
/// Development-only authentication helpers used by the E2E test suite.
/// These endpoints are NOT registered when the host environment is not
/// <c>Development</c>, so they are inert in staging / production.
/// </summary>
public sealed class AuthDevEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Resolve the host environment from the application's service provider.
        var env = app.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment())
            return;

        var group = app.MapGroup($"{ApiRoutes.Auth.Base}/dev")
            .WithTags("Authentication (Dev)")
            .ExcludeFromDescription();

        group.MapPost("/confirm-email", ConfirmEmail)
            .WithName("AuthDev_ConfirmEmail")
            .WithSummary("(DEV) Force-confirm a user's email")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>Body for the dev confirm-email endpoint.</summary>
    public sealed record ConfirmEmailDevRequest(string Email);

    private static async Task<IResult> ConfirmEmail(
        [FromBody] ConfirmEmailDevRequest request,
        [FromServices] IUserRepository userRepository,
        [FromServices] IUserUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "email is required" });

        var user = await userRepository.GetByEmailStringAsync(request.Email, cancellationToken);
        if (user is null)
            return Results.NotFound();

        // Already verified → idempotent success.
        if (user.EmailVerified)
            return Results.NoContent();

        // The user always has a verification token after registration. We pull it
        // directly from the aggregate and run the real domain VerifyEmail() path,
        // so all invariants and the UserEmailVerifiedEvent are preserved.
        var token = user.EmailVerificationToken;
        if (string.IsNullOrEmpty(token))
            return Results.Problem("User has no pending verification token.", statusCode: 409);

        var result = user.VerifyEmail(token);
        if (result.IsFailure)
            return Results.Problem(result.Error.Message, statusCode: 400);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}
