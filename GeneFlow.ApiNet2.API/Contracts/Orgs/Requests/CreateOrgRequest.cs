namespace GeneFlow.ApiNet2.API.Contracts.Orgs.Requests;

/// <summary>
/// Payload for creating a new organisation.
/// </summary>
public sealed record CreateOrgRequest(
    string Handle,
    string Name,
    string? Description,
    string? Visibility);
