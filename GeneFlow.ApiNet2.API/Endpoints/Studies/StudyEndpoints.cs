using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Studies.Requests;
using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Studies.Commands.ChangeStudyStatus;
using GeneFlow.ApiNet2.Application.Studies.Commands.CreateStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.DeleteStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.DuplicateStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.ExportStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.UpdateReadme;
using GeneFlow.ApiNet2.Application.Studies.Commands.UpdateStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.UpdateStudySettings;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetFeaturedStudies;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetPublicStudies;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetResearchFields;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyById;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetUserStudies;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

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
            .Produces<IReadOnlyList<ResearchFieldResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        group.MapGet("/public", GetPublicStudies)
            .WithName("Studies_GetPublic")
            .WithSummary("Get public studies")
            .WithDescription("Returns a paginated list of published/public studies.")
            .Produces<PagedResponse<StudySummaryResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        group.MapGet("/featured", GetFeaturedStudies)
            .WithName("Studies_GetFeatured")
            .WithSummary("Get featured studies")
            .WithDescription("Returns the list of featured studies.")
            .Produces<IReadOnlyList<StudySummaryResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous();

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

        authGroup.MapPut("/{studyId}/readme", UpdateReadme)
            .WithName("Studies_UpdateReadme")
            .WithSummary("Update study README")
            .WithDescription("Updates the README markdown content of a study.")
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

        authGroup.MapGet("/{studyId}/export", ExportStudy)
            .WithName("Studies_Export")
            .WithSummary("Export study as a ZIP archive")
            .WithDescription("Streams a ZIP containing study metadata, members, all trace files and all paper PDFs. Any member can trigger.")
            .Produces(StatusCodes.Status200OK, contentType: "application/zip")
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

    private static async Task<IResult> UpdateReadme(
        [FromRoute] string studyId,
        [FromBody] UpdateReadmeRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new UpdateReadmeCommand(
            studyId,
            currentUser.UserId.ToString()!,
            request.Markdown);

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

    /// <summary>
    /// Streams a ZIP archive of the study (metadata + members + traces + papers)
    /// directly into the response. The command builds the projection; this
    /// endpoint owns the ZIP-writing because <see cref="Results.Stream(Func{Stream, Task}, string?, string?, DateTimeOffset?, EntityTagHeaderValue?, bool)"/>
    /// is an API-layer concern.
    /// </summary>
    private static async Task<IResult> ExportStudy(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new ExportStudyCommand(studyId, currentUser.UserId.ToString()!);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var dto = result.Value;

        async Task WriteZip(Stream output)
        {
            // ZipArchive performs synchronous writes when disposing (central
            // directory). ASP.NET Core may forbid sync I/O on the response
            // body, so we buffer to a MemoryStream and copy asynchronously.
            using var buffer = new MemoryStream();
            using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                await PopulateArchive(archive);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(output, cancellationToken);
            return;

            async Task PopulateArchive(ZipArchive archive)
            {

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            };

            // Track which trace/paper files were actually included so
            // {*}.json can record missing files honestly.
            var traceMissing = new Dictionary<string, string>();
            var paperMissing = new Dictionary<string, string>();

            // 1) trace + paper file blobs first so we know what's missing
            //    by the time we write the JSON indices.
            foreach (var trace in dto.Traces)
            {
                var bytes = await fileStorage.GetFileAsync(trace.StoragePath, cancellationToken);
                if (bytes is null)
                {
                    traceMissing[trace.Id] = "file not found in storage";
                    continue;
                }

                var entryName = $"traces/files/{SafeEntryName($"{trace.Id}-{trace.FileName}")}";
                var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(bytes, cancellationToken);
            }

            foreach (var paper in dto.Papers)
            {
                if (!paper.HasFile || string.IsNullOrEmpty(paper.StoragePath))
                    continue;

                var bytes = await fileStorage.GetFileAsync(paper.StoragePath, cancellationToken);
                if (bytes is null)
                {
                    paperMissing[paper.Id] = "file not found in storage";
                    continue;
                }

                var displayName = string.IsNullOrEmpty(paper.FileName)
                    ? $"{paper.Id}.pdf"
                    : $"{paper.Id}-{paper.FileName}";
                var entryName = $"papers/files/{SafeEntryName(displayName)}";
                var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(bytes, cancellationToken);
            }

            // 2) JSON indices
            await WriteJsonEntry(archive, "study.json", dto.Study, jsonOptions, cancellationToken);
            await WriteJsonEntry(archive, "members.json", dto.Members, jsonOptions, cancellationToken);

            var traceIndex = dto.Traces.Select(t => new
            {
                t.Id,
                t.FileName,
                t.ContentType,
                t.SizeBytes,
                t.Format,
                t.Status,
                t.ProcessedAt,
                t.FailureReason,
                t.QualityMetrics,
                FileBytesPath = traceMissing.ContainsKey(t.Id)
                    ? null
                    : $"traces/files/{SafeEntryName($"{t.Id}-{t.FileName}")}",
                MissingReason = traceMissing.TryGetValue(t.Id, out var r) ? r : null,
            }).ToList();
            await WriteJsonEntry(archive, "traces/traces.json", traceIndex, jsonOptions, cancellationToken);

            var paperIndex = dto.Papers.Select(p =>
            {
                string? fileBytesPath = null;
                if (p.HasFile && !paperMissing.ContainsKey(p.Id))
                {
                    var displayName = string.IsNullOrEmpty(p.FileName) ? $"{p.Id}.pdf" : $"{p.Id}-{p.FileName}";
                    fileBytesPath = $"papers/files/{SafeEntryName(displayName)}";
                }
                return new
                {
                    p.Id,
                    p.Title,
                    p.Authors,
                    p.Doi,
                    p.Journal,
                    p.PublicationYear,
                    p.Abstract,
                    p.HasFile,
                    p.FileName,
                    p.FileSizeBytes,
                    FileBytesPath = fileBytesPath,
                    MissingReason = paperMissing.TryGetValue(p.Id, out var r) ? r : null,
                };
            }).ToList();
            await WriteJsonEntry(archive, "papers/papers.json", paperIndex, jsonOptions, cancellationToken);

            // 3) human-readable README
            var readme = BuildReadme(dto, traceMissing.Count, paperMissing.Count);
            var readmeEntry = archive.CreateEntry("README.txt", CompressionLevel.Optimal);
            using (var readmeStream = readmeEntry.Open())
            {
                var bytes = Encoding.UTF8.GetBytes(readme);
                await readmeStream.WriteAsync(bytes, cancellationToken);
            }
            }
        }

        return Results.Stream(WriteZip, "application/zip", fileDownloadName: dto.ArchiveFileName);
    }

    private static async Task WriteJsonEntry<T>(
        ZipArchive archive,
        string entryName,
        T payload,
        JsonSerializerOptions options,
        CancellationToken ct)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        await JsonSerializer.SerializeAsync(stream, payload, options, ct);
    }

    private static string BuildReadme(ExportStudyDto dto, int missingTraces, int missingPapers)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"GeneFlow Study Export");
        sb.AppendLine(new string('=', 40));
        sb.AppendLine();
        sb.AppendLine($"Study:        {dto.Study.Title}");
        sb.AppendLine($"ID:           {dto.Study.Id}");
        sb.AppendLine($"Status:       {dto.Study.Status}");
        sb.AppendLine($"Field:        {dto.Study.ResearchField}");
        sb.AppendLine($"Created:      {dto.Study.CreatedAt:u}");
        sb.AppendLine();
        sb.AppendLine($"Members:      {dto.Members.Count}");
        sb.AppendLine($"Traces:       {dto.Traces.Count} ({missingTraces} file(s) missing)");
        sb.AppendLine($"Papers:       {dto.Papers.Count} ({missingPapers} file(s) missing)");
        sb.AppendLine();
        sb.AppendLine("Layout:");
        sb.AppendLine("  study.json              - full study metadata");
        sb.AppendLine("  members.json            - member roster");
        sb.AppendLine("  traces/traces.json      - trace metadata index");
        sb.AppendLine("  traces/files/           - original trace files");
        sb.AppendLine("  papers/papers.json      - paper metadata index");
        sb.AppendLine("  papers/files/           - paper PDFs (when available)");
        return sb.ToString();
    }

    /// <summary>
    /// Strips ZIP-unsafe characters from a logical entry name. We rebuild it
    /// from scratch (alphanumerics + a small set of safe punctuation) so the
    /// archive is portable across Windows, macOS and Linux.
    /// </summary>
    private static string SafeEntryName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "file";
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c is '.' or '-' or '_')
                sb.Append(c);
            else if (c is ' ' or '/' or '\\')
                sb.Append('-');
            // drop everything else
        }
        var cleaned = sb.ToString().Trim('.', '-', '_');
        return string.IsNullOrEmpty(cleaned) ? "file" : cleaned;
    }
}
