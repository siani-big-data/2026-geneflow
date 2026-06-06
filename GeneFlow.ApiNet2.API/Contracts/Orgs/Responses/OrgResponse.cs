namespace GeneFlow.ApiNet2.API.Contracts.Orgs.Responses;

/// <summary>
/// API response describing an organisation. Mirrors the application
/// <c>OrgDto</c> shape.
/// </summary>
public sealed record OrgResponse(
    string Id,
    string Handle,
    string Name,
    string? Description,
    string? AvatarUrl,
    string? WebsiteUrl,
    string? Location,
    string Visibility,
    DateTime CreatedAt);
