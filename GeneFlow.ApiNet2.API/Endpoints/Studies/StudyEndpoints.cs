using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Studies.Requests;
using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Studies.Commands.ChangeStudyStatus;
using GeneFlow.ApiNet2.Application.Studies.Commands.CreateStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.DeleteStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.DuplicateStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.UpdateStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.UpdateStudySettings;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetFeaturedStudies;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetPublicStudies;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetResearchFields;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyById;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetUserStudies;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.API.Endpoints.Studies;

/// <summary>
/// Study management endpoints.
/// </summary>
public sealed class StudyEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/studies")
            .WithTags("Studies")
            .WithOpenApi();

        // Public endpoints (no auth required)
        group.MapGet("/research-fields", GetResearchFields)
            .WithName("Studies_GetResearchFields")
            .WithSummary("Get all research fields")
            .WithDescription("Returns the list of available research fields.")
            .Produces<IReadOnlyList<ResearchFieldResponse>>(StatusCodes.Status200OK);

        group.MapGet("/public", GetPublicStudies)
            .WithName("Studies_GetPublic")
            .WithSummary("Get public studies")
            .WithDescription("Returns a paginated list of published/public studies.")
            .Produces<PagedResponse<StudySummaryResponse>>(StatusCodes.Status200OK);

        group.MapGet("/featured", GetFeaturedStudies)
            .WithName("Studies_GetFeatured")
            .WithSummary("Get featured studies")
            .WithDescription("Returns the list of featured studies.")
            .Produces<IReadOnlyList<StudySummaryResponse>>(StatusCodes.Status200OK);

        // Authenticated endpoints
        var authGroup = group.RequireAuthorization();

        authGroup.MapGet("/mine", GetUserStudies)
            .WithName("Studies_GetMine")
            .WithSummary("Get my studies")
            .WithDescription("Returns a paginated list of studies where the current user is a member.")
            .Produces<PagedResponse<StudySummaryResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        authGroup.MapGet("/{studyId}", GetStudyById)
            .WithName("Studies_GetById")
            .WithSummary("Get study by ID")
            .WithDescription("Returns a study by its ID.")
            .Produces<StudyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        authGroup.MapPost("/", CreateStudy)
            .WithName("Studies_Create")
            .WithSummary("Create a new study")
            .WithDescription("Creates a new study with the current user as owner.")
            .Produces<StudyResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        authGroup.MapPut("/{studyId}", UpdateStudy)
            .WithName("Studies_Update")
            .WithSummary("Update a study")
            .WithDescription("Updates an existing study.")
            .Produces<StudyResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        authGroup.MapPatch("/{studyId}/status", ChangeStudyStatus)
            .WithName("Studies_ChangeStatus")
            .WithSummary("Change study status")
            .WithDescription("Changes the status of a study.")
            .Produces<StudyResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        authGroup.MapPatch("/{studyId}/settings", UpdateStudySettings)
            .WithName("Studies_UpdateSettings")
            .WithSummary("Update study settings")
            .WithDescription("Updates the settings of a study.")
            .Produces<StudyResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        authGroup.MapDelete("/{studyId}", DeleteStudy)
            .WithName("Studies_Delete")
            .WithSummary("Delete a study")
            .WithDescription("Soft-deletes a study. Only the owner can delete.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        authGroup.MapPost("/{studyId}/duplicate", DuplicateStudy)
            .WithName("Studies_Duplicate")
            .WithSummary("Duplicate a study")
            .WithDescription("Creates a copy of an existing study. The current user becomes the owner of the duplicate.")
            .Produces<StudyResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetResearchFields(
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetResearchFieldsQuery();
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponses());
    }

    private static async Task<IResult> GetPublicStudies(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? researchFieldId = null,
        [FromQuery] string? tags = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true,
        [FromServices] ISender sender = null!,
        CancellationToken cancellationToken = default)
    {
        var tagList = string.IsNullOrWhiteSpace(tags)
            ? null
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        var query = new GetPublicStudiesQuery(
            pageNumber > 0 ? pageNumber : 1,
            pageSize > 0 ? pageSize : PagedRequest.DefaultPageSize,
            searchTerm,
            researchFieldId,
            tagList,
            sortBy,
            sortDescending);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToPagedResponse(dto => dto.ToResponse()));
    }

    private static async Task<IResult> GetFeaturedStudies(
        [FromQuery] int limit = 10,
        [FromServices] ISender sender = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFeaturedStudiesQuery(limit > 0 ? limit : 10);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponses());
    }

    private static async Task<IResult> GetUserStudies(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromQuery] string? searchTerm,
        [FromQuery] int? statusId,
        [FromQuery] int? researchFieldId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] ILogger<StudyEndpoints> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("GetUserStudies endpoint called");

        if (currentUser.UserId is null)
        {
            logger.LogWarning("GetUserStudies: No authenticated user");
            return Results.Unauthorized();
        }

        logger.LogInformation("GetUserStudies: UserId from token = {UserId}", currentUser.UserId);

        var query = new GetUserStudiesQuery(
            currentUser.UserId.ToString()!,
            pageNumber > 0 ? pageNumber : 1,
            pageSize > 0 ? pageSize : PagedRequest.DefaultPageSize,
            searchTerm,
            statusId,
            researchFieldId);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        logger.LogInformation("GetUserStudies: Returning {Count} studies", result.Value.TotalCount);
        return Results.Ok(result.Value.ToPagedResponse(dto => dto.ToResponse()));
    }

    private static async Task<IResult> GetStudyById(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var query = new GetStudyByIdQuery(studyId, currentUser.UserId?.ToString());
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> CreateStudy(
        [FromBody] CreateStudyRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new CreateStudyCommand(
            currentUser.UserId.ToString()!,
            request.Title,
            request.Description,
            request.ResearchFieldId,
            request.Institution,
            request.PrincipalInvestigator,
            request.Tags);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Created($"/api/v1/studies/{result.Value.Id}", result.Value.ToResponse());
    }

    private static async Task<IResult> UpdateStudy(
        [FromRoute] string studyId,
        [FromBody] UpdateStudyRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new UpdateStudyCommand(
            studyId,
            currentUser.UserId.ToString()!,
            request.Title,
            request.Description,
            request.ResearchFieldId,
            request.Institution,
            request.PrincipalInvestigator,
            request.Tags);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> ChangeStudyStatus(
        [FromRoute] string studyId,
        [FromBody] ChangeStudyStatusRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new ChangeStudyStatusCommand(
            studyId,
            currentUser.UserId.ToString()!,
            request.NewStatusId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> UpdateStudySettings(
        [FromRoute] string studyId,
        [FromBody] UpdateStudySettingsRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new UpdateStudySettingsCommand(
            studyId,
            currentUser.UserId.ToString()!,
            request.AllowPublicComments,
            request.AllowDataDownload,
            request.RequireApprovalToJoin);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> DeleteStudy(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new DeleteStudyCommand(
            studyId,
            currentUser.UserId.ToString()!);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> DuplicateStudy(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new DuplicateStudyCommand(
            studyId,
            currentUser.UserId.ToString()!);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Created($"/api/v1/studies/{result.Value.Id}", result.Value.ToResponse());
    }
}
