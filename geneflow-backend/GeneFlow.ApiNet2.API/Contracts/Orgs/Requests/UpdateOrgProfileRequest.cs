namespace GeneFlow.ApiNet2.API.Contracts.Orgs.Requests;

/// <summary>
/// Payload for updating an organisation's profile fields.
/// </summary>
public sealed record UpdateOrgProfileRequest(
    string Name,
    string? Description,
    string? AvatarUrl,
    string? WebsiteUrl,
    string? Location);
