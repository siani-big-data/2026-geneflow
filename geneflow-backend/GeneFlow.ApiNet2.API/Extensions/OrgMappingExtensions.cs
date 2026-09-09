using GeneFlow.ApiNet2.API.Contracts.Orgs.Responses;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Maps application-layer Org DTOs to the API response contracts that
/// are serialised to the wire.
/// </summary>
public static class OrgMappingExtensions
{
    public static OrgResponse ToResponse(this OrgDto dto) => new(
        dto.Id,
        dto.Handle,
        dto.Name,
        dto.Description,
        dto.AvatarUrl,
        dto.WebsiteUrl,
        dto.Location,
        dto.Visibility,
        dto.MemberCount,
        dto.MyRole,
        dto.CreatedAt);

    public static OrgMembershipResponse ToResponse(this OrgMembershipDto dto) => new(
        dto.OrgId,
        dto.Handle,
        dto.Name,
        dto.AvatarUrl,
        dto.Role);

    public static OrgMemberResponse ToResponse(this OrgMemberDto dto) => new(
        dto.UserId,
        dto.UserName,
        dto.AvatarUrl,
        dto.Role,
        dto.JoinedAt);

    public static OrgInvitationResponse ToResponse(this OrgInvitationDto dto) => new(
        dto.Id,
        dto.OrgId,
        dto.OrgHandle,
        dto.OrgName,
        dto.InvitedEmail,
        dto.Role,
        dto.Status,
        dto.CreatedAt,
        dto.ExpiresAt,
        dto.Token);
}
