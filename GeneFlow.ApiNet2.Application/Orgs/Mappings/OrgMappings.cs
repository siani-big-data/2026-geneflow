using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Entities;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;

namespace GeneFlow.ApiNet2.Application.Orgs.Mappings;

public static class OrgMappings
{
    public static OrgDto ToDto(this Org org, UserId? viewer = null) => new()
    {
        Id = org.Id.ToString(),
        Handle = org.Handle,
        Name = org.Name,
        Description = org.Description,
        AvatarUrl = org.AvatarUrl,
        WebsiteUrl = org.WebsiteUrl,
        Location = org.Location,
        Visibility = org.Visibility.Name,
        MemberCount = org.Members.Count,
        MyRole = viewer is null ? null : org.GetMember(viewer)?.Role.Name,
        CreatedAt = org.CreatedAt
    };

    public static OrgMembershipDto ToMembershipDto(this Org org, UserId currentUser)
    {
        var member = org.GetMember(currentUser);
        return new OrgMembershipDto
        {
            OrgId = org.Id.ToString(),
            Handle = org.Handle,
            Name = org.Name,
            AvatarUrl = org.AvatarUrl,
            Role = member?.Role.Name ?? OrgRole.Member.Name
        };
    }

    public static OrgMemberDto ToDto(this OrgMember member, string? userName = null, string? avatarUrl = null) => new()
    {
        UserId = member.UserId.ToString(),
        UserName = userName,
        AvatarUrl = avatarUrl,
        Role = member.Role.Name,
        JoinedAt = member.JoinedAt
    };

    public static OrgInvitationDto ToDto(
        this OrgInvitation invitation,
        string? orgHandle = null,
        string? orgName = null) => new()
    {
        Id = invitation.Id.ToString(),
        OrgId = invitation.OrgId.ToString(),
        OrgHandle = orgHandle,
        OrgName = orgName,
        InvitedEmail = invitation.InvitedEmail,
        Role = invitation.Role.Name,
        Status = invitation.Status.Name,
        CreatedAt = invitation.CreatedAt,
        ExpiresAt = invitation.ExpiresAt,
        Token = invitation.Token
    };
}
