using System.Text;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.ExportStudy;

/// <summary>
/// Builds an <see cref="ExportStudyDto"/> projection for a given study.
/// File bytes are NOT loaded here — the API endpoint streams them directly
/// from <c>IFileStorageService</c> while writing the ZIP. This keeps the
/// command handler in-memory cheap and avoids materialising potentially
/// large payloads inside MediatR's pipeline.
/// </summary>
public sealed class ExportStudyCommandHandler
    : ICommandHandler<ExportStudyCommand, Result<ExportStudyDto>>
{
    /// <summary>Cap for the suggested archive filename, to keep it filesystem-friendly.</summary>
    private const int MaxArchiveFileNameLength = 80;

    private readonly IStudyRepository _studyRepository;
    private readonly ITraceRepository _traceRepository;

    public ExportStudyCommandHandler(
        IStudyRepository studyRepository,
        ITraceRepository traceRepository)
    {
        _studyRepository = studyRepository;
        _traceRepository = traceRepository;
    }

    public async Task<Result<ExportStudyDto>> Handle(
        ExportStudyCommand request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<ExportStudyDto>(StudyErrors.InvalidUserId);

        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<ExportStudyDto>(StudyErrors.NotFound);

        var study = await _studyRepository.GetByIdWithMembersAsync(studyId, cancellationToken)
                    ?? await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<ExportStudyDto>(StudyErrors.NotFound);

        // Any member of the study can export. Non-members get NotAMember (403/404 mapped).
        if (!study.IsMember(userId))
            return Result.Failure<ExportStudyDto>(StudyErrors.NotAMember);

        var traces = await _traceRepository.GetByStudyIdAsync(studyId, cancellationToken);

        var metadata = new StudyMetadataExport(
            Id: study.Id.ToString()!,
            Title: study.Title.Value,
            Description: study.Description.Value,
            ResearchField: study.ResearchField.Name,
            Status: study.Status.Name,
            Institution: study.Institution,
            PrincipalInvestigator: study.PrincipalInvestigator,
            IsFeatured: study.IsFeatured,
            Settings: new StudySettingsExport(
                study.Settings.AllowPublicComments,
                study.Settings.AllowDataDownload,
                study.Settings.RequireApprovalToJoin),
            Metrics: new StudyMetricsExport(study.Metrics.ViewsCount, study.Metrics.StarsCount),
            Tags: study.Tags.ToList(),
            CreatedAt: study.CreatedAt,
            ModifiedAt: study.ModifiedAt);

        var members = study.Members
            .Select(m => new MemberExportInfo(
                UserId: m.UserId.ToString()!,
                Role: m.Role.Name,
                JoinedAt: m.JoinedAt,
                InvitedBy: m.InvitedBy?.ToString()))
            .ToList();

        var traceEntries = traces
            .Select(t => new TraceExportEntry(
                Id: t.Id.ToString()!,
                FileName: t.File.FileName,
                ContentType: t.File.ContentType,
                StoragePath: t.File.StoragePath,
                SizeBytes: t.File.SizeBytes,
                Format: t.Format.Name,
                Status: t.Status.Name,
                ProcessedAt: t.ProcessedAt,
                FailureReason: t.FailureReason,
                QualityMetrics: t.QualityMetrics is null
                    ? null
                    : new TraceQualityMetricsExport(
                        t.QualityMetrics.AverageQualityScore,
                        t.QualityMetrics.TotalBases,
                        t.QualityMetrics.QualityAboveQ20Percentage,
                        t.QualityMetrics.QualityAboveQ30Percentage,
                        t.QualityMetrics.TrimmedLength,
                        t.QualityMetrics.GcContentPercentage)))
            .ToList();

        var paperEntries = study.Papers
            .Select(p => BuildPaperEntry(study.Id, p))
            .ToList();

        var archiveName = BuildArchiveFileName(study.Title.Value, study.Id.ToString()!);

        var dto = new ExportStudyDto(
            ArchiveFileName: archiveName,
            Study: metadata,
            Members: members,
            Traces: traceEntries,
            Papers: paperEntries);

        return Result.Success(dto);
    }

    private static PaperExportEntry BuildPaperEntry(StudyId studyId, StudyPaper paper)
    {
        // Paper file convention used by the upload pipeline:
        //   studies/{studyId}/papers/{paperId}/{fileId}
        // FileId already encodes the extension when present.
        string? storagePath = null;
        if (paper.HasFile && !string.IsNullOrEmpty(paper.FileId))
        {
            storagePath = $"studies/{studyId}/papers/{paper.Id}/{paper.FileId}";
        }

        return new PaperExportEntry(
            Id: paper.Id.ToString()!,
            Title: paper.Title,
            Authors: paper.Authors,
            Doi: paper.Doi,
            Journal: paper.Journal,
            PublicationYear: paper.PublicationYear,
            Abstract: paper.Abstract,
            HasFile: paper.HasFile,
            FileName: paper.FileName,
            FileSizeBytes: paper.FileSizeBytes,
            StoragePath: storagePath);
    }

    /// <summary>
    /// Returns a filesystem-safe archive name like
    /// <c>study-my-research-S00000001.zip</c>, capped to 80 chars.
    /// </summary>
    public static string BuildArchiveFileName(string title, string studyId)
    {
        var slug = Sanitise(title);
        var raw = string.IsNullOrEmpty(slug)
            ? $"study-{studyId}.zip"
            : $"study-{slug}-{studyId}.zip";

        if (raw.Length <= MaxArchiveFileNameLength)
            return raw;

        // Trim the slug part while keeping the studyId + extension.
        var suffix = $"-{studyId}.zip";
        var prefixBudget = MaxArchiveFileNameLength - suffix.Length - "study-".Length;
        if (prefixBudget < 1) prefixBudget = 1;
        var trimmedSlug = slug.Length > prefixBudget ? slug[..prefixBudget] : slug;
        return $"study-{trimmedSlug}{suffix}";
    }

    /// <summary>
    /// Lower-cased ASCII slug: alphanumerics and hyphens only, runs of
    /// non-alphanumerics collapsed to a single hyphen, trimmed.
    /// </summary>
    private static string Sanitise(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);
        var lastWasHyphen = false;
        foreach (var raw in input.Trim().ToLowerInvariant())
        {
            var c = raw;
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(c);
                lastWasHyphen = false;
            }
            else
            {
                if (!lastWasHyphen && sb.Length > 0)
                {
                    sb.Append('-');
                    lastWasHyphen = true;
                }
            }
        }

        // Trim trailing hyphen.
        if (sb.Length > 0 && sb[^1] == '-') sb.Length--;
        return sb.ToString();
    }
}
