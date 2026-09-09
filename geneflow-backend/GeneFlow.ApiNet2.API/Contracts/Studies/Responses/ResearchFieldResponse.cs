namespace GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

/// <summary>
/// Response model for research field information.
/// </summary>
public sealed record ResearchFieldResponse(
    int Id,
    string Name,
    string DisplayName);
