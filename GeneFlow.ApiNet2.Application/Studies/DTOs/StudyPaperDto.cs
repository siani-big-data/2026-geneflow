namespace GeneFlow.ApiNet2.Application.Studies.DTOs;

/// <summary>
/// Study paper data transfer object.
/// </summary>
public sealed record StudyPaperDto
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Authors { get; init; }
    public string? Doi { get; init; }
    public string? Abstract { get; init; }
    public string? Journal { get; init; }
    public int? PublicationYear { get; init; }
    public string? FileId { get; init; }
    public string? FileName { get; init; }
    public long? FileSizeBytes { get; init; }
    public required bool HasFile { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}
