namespace GeneFlow.ApiNet2.Application.Studies.DTOs;

/// <summary>
/// Research field data transfer object.
/// </summary>
public sealed record ResearchFieldDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
}
