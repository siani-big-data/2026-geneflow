namespace GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

/// <summary>
/// Response model for study paper information.
/// </summary>
public sealed record StudyPaperResponse(
    string Id,
    string Title,
    string? Authors,
    string? Doi,
    string? Abstract,
    string? Journal,
    int? PublicationYear,
    string? FileId,
    string? FileName,
    long? FileSizeBytes,
    string UploadedBy,
    DateTime UploadedAt);
