using GeneFlow.ApiNet2.API.Contracts.Profiles.Requests;
using GeneFlow.ApiNet2.API.Contracts.Profiles.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.API.Routes;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Profiles.Commands.DeleteProfilePhoto;
using GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateProfile;
using GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateProfilePhoto;
using GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateResearchIdentifiers;
using GeneFlow.ApiNet2.Application.Profiles.Commands.UploadProfilePhoto;
using GeneFlow.ApiNet2.Application.Profiles.Queries.GetCurrentUserProfile;
using GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfileByUserId;
using GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfilesByUserIds;
using GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfileStats;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Profiles;

/// <summary>
/// User profile management endpoints.
/// </summary>
public sealed class ProfileEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Profiles.Base)
            .WithTags("Profiles")
            .WithOpenApi();

        // Current user profile endpoints
        group.MapGet("/me", GetCurrentUserProfile)
            .WithName("Profiles_GetCurrent")
            .WithSummary("Get current user's profile")
            .WithDescription("Returns the profile of the currently authenticated user.")
            .RequireAuthorization()
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/me/stats", GetCurrentUserStats)
            .WithName("Profiles_GetCurrentStats")
            .WithSummary("Get current user's profile statistics")
            .WithDescription("Returns statistics for the currently authenticated user's profile.")
            .RequireAuthorization()
            .Produces<ProfileStatsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/me", UpdateCurrentUserProfile)
            .WithName("Profiles_UpdateCurrent")
            .WithSummary("Update current user's profile")
            .WithDescription("Updates basic profile information for the currently authenticated user.")
            .RequireAuthorization()
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/me/research-identifiers", UpdateResearchIdentifiers)
            .WithName("Profiles_UpdateResearchIdentifiers")
            .WithSummary("Update research identifiers")
            .WithDescription("Updates ORCID and website for the currently authenticated user.")
            .RequireAuthorization()
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/me/photo", UpdateProfilePhoto)
            .WithName("Profiles_UpdatePhoto")
            .WithSummary("Update profile photo")
            .WithDescription("Updates the profile photo for the currently authenticated user.")
            .RequireAuthorization()
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/me/photo", DeleteProfilePhoto)
            .WithName("Profiles_DeletePhoto")
            .WithSummary("Delete profile photo")
            .WithDescription("Removes the profile photo for the currently authenticated user.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/me/photo/upload", UploadProfilePhoto)
            .WithName("Profiles_UploadPhoto")
            .WithSummary("Upload profile photo")
            .WithDescription("Uploads a profile photo for the currently authenticated user. Max size: 10MB. Supported formats: JPEG, PNG, GIF, WebP.")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        // Profile by user ID endpoints
        group.MapGet("/{userId}", GetProfileByUserId)
            .WithName("Profiles_GetByUserId")
            .WithSummary("Get profile by user ID")
            .WithDescription("Returns the profile for a specific user.")
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Batch profile retrieval
        group.MapPost("/batch", GetProfilesByUserIds)
            .WithName("Profiles_GetBatch")
            .WithSummary("Get multiple profiles by user IDs")
            .WithDescription("Returns profiles for multiple users in a single request (max 100).")
            .Produces<IReadOnlyList<ProfileSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> GetCurrentUserProfile(
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetCurrentUserProfileQuery();
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> GetCurrentUserStats(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetProfileStatsQuery(currentUser.UserId.Value.ToString());
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> UpdateCurrentUserProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new UpdateProfileCommand(
            currentUser.UserId.Value.ToString(),
            request.FirstName,
            request.LastName,
            request.Bio,
            request.Location,
            request.ProfessionalRole,
            request.InstitutionName,
            request.InstitutionDepartment,
            request.ResearchField);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> UpdateResearchIdentifiers(
        [FromBody] UpdateResearchIdentifiersRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new UpdateResearchIdentifiersCommand(
            currentUser.UserId.Value.ToString(),
            request.OrcidId,
            request.Website);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> UpdateProfilePhoto(
        [FromBody] UpdateProfilePhotoRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new UpdateProfilePhotoCommand(
            currentUser.UserId.Value.ToString(),
            request.PhotoUrl,
            request.ThumbnailUrl,
            request.SizeBytes);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> DeleteProfilePhoto(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new DeleteProfilePhotoCommand(currentUser.UserId.Value.ToString());
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> UploadProfilePhoto(
        IFormFile file,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "No file provided" });

        // Read file to base64
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var photoDataBase64 = Convert.ToBase64String(memoryStream.ToArray());

        var command = new UploadProfilePhotoCommand(
            currentUser.UserId.Value.ToString(),
            photoDataBase64,
            file.FileName,
            file.ContentType,
            file.Length);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> GetProfileByUserId(
        [FromRoute] string userId,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetProfileByUserIdQuery(userId);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> GetProfilesByUserIds(
        [FromBody] GetProfilesByUserIdsRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetProfilesByUserIdsQuery(request.UserIds);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponses());
    }
}
