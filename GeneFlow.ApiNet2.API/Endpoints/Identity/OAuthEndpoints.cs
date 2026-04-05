using GeneFlow.ApiNet2.API.Contracts.Identity.Requests;
using GeneFlow.ApiNet2.API.Contracts.Identity.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.API.Routes;
using GeneFlow.ApiNet2.Application.Identity.Commands.OAuthLogin;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Identity;

/// <summary>
/// OAuth authentication endpoints.
/// </summary>
public sealed class OAuthEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Auth.OAuth)
            .WithTags("OAuth")
            .WithOpenApi();

        group.MapPost("/{provider}", OAuthLogin)
            .WithName("OAuth_Login")
            .WithSummary("Login or register via OAuth provider")
            .WithDescription("Authenticates a user using an OAuth provider (Google, GitHub). " +
                             "If the user doesn't exist, a new account is created.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> OAuthLogin(
        [FromRoute] string provider,
        [FromBody] OAuthLoginRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new OAuthLoginCommand(provider, request.Token);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var loginResult = result.Value;
        return Results.Ok(new LoginResponse(
            loginResult.User.ToResponse(),
            loginResult.Tokens?.ToResponse(),
            loginResult.RequiresTwoFactor));
    }
}
