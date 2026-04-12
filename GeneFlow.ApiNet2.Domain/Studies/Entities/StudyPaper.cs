using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Studies.Entities;

/// <summary>
/// Represents a scientific paper associated with a study.
/// </summary>
public sealed class StudyPaper : FullAuditableEntity<StudyPaperId>
{
    public const int MaxTitleLength = 500;
    public const int MaxAuthorsLength = 1000;
    public const int MaxDoiLength = 100;
    public const int MaxAbstractLength = 5000;
    public const int MaxJournalLength = 200;

    public string Title { get; private set; } = null!;
    public string? Authors { get; private set; }
    public string? Doi { get; private set; }
    public string? Abstract { get; private set; }
    public string? Journal { get; private set; }
    public int? PublicationYear { get; private set; }
    public string? FileId { get; private set; }
    public string? FileName { get; private set; }
    public long? FileSizeBytes { get; private set; }

    public bool HasFile => !string.IsNullOrEmpty(FileId);

    private StudyPaper() : base() { }

    private StudyPaper(
        StudyPaperId id,
        string title,
        string? authors,
        string? doi,
        string? @abstract,
        string? journal,
        int? publicationYear,
        string? fileId,
        string? fileName,
        long? fileSizeBytes,
        UserId uploadedBy) : base(id)
    {
        Title = title;
        Authors = authors;
        Doi = doi;
        Abstract = @abstract;
        Journal = journal;
        PublicationYear = publicationYear;
        FileId = fileId;
        FileName = fileName;
        FileSizeBytes = fileSizeBytes;

        SetCreationAudit(DateTime.UtcNow, uploadedBy.Value.ToString());
    }

    public static Result<StudyPaper> Create(
        StudyPaperId id,
        string title,
        string? authors,
        string? doi,
        string? @abstract,
        string? journal,
        int? publicationYear,
        string? fileId,
        string? fileName,
        long? fileSizeBytes,
        UserId uploadedBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<StudyPaper>(StudyErrors.PaperTitleRequired);

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > MaxTitleLength)
            return Result.Failure<StudyPaper>(StudyErrors.PaperTitleTooLong(MaxTitleLength));

        return new StudyPaper(
            id,
            trimmedTitle,
            authors?.Trim(),
            doi?.Trim(),
            @abstract?.Trim(),
            journal?.Trim(),
            publicationYear,
            fileId,
            fileName,
            fileSizeBytes,
            uploadedBy);
    }
}
