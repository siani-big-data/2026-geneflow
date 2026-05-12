using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;

namespace GeneFlow.ApiNet2.Application.Studies.Mappings;

/// <summary>
/// Mapping extensions for Study domain entities to DTOs.
/// </summary>
public static class StudyMappings
{
    public static StudyDto ToDto(this Study study)
    {
        return study.ToDto(null, null, null);
    }

    public static StudyDto ToDto(
        this Study study,
        Dictionary<string, User>? userLookup,
        Dictionary<string, Profile>? profileLookup)
    {
        return study.ToDto(userLookup, profileLookup, null);
    }

    public static StudyDto ToDto(
        this Study study,
        Dictionary<string, User>? userLookup,
        Dictionary<string, Profile>? profileLookup,
        UserId? currentUserId)
    {
        // Determine current user's permissions
        CurrentUserPermissionsDto permissions;
        if (currentUserId is not null)
        {
            var member = study.GetMember(currentUserId);
            permissions = member is not null
                ? CurrentUserPermissionsDto.FromRole(member.Role)
                : CurrentUserPermissionsDto.ReadOnly;
        }
        else
        {
            permissions = CurrentUserPermissionsDto.ReadOnly;
        }

        return new StudyDto
        {
            Id = study.Id.ToString(),
            OwnerId = study.OwnerId.ToString(),
            Title = study.Title.Value,
            Description = study.Description?.Value,
            ResearchField = study.ResearchField.Name,
            ResearchFieldId = study.ResearchField.Id,
            Status = study.Status.Name,
            StatusId = study.Status.Id,
            AllowPublicComments = study.Settings.AllowPublicComments,
            AllowDataDownload = study.Settings.AllowDataDownload,
            RequireApprovalToJoin = study.Settings.RequireApprovalToJoin,
            Institution = study.Institution,
            PrincipalInvestigator = study.PrincipalInvestigator,
            IsFeatured = study.IsFeatured,
            Tags = study.Tags,
            ViewsCount = study.Metrics.ViewsCount,
            StarsCount = study.Metrics.StarsCount,
            Members = study.Members.Select(m => m.ToDto(userLookup, profileLookup)).ToList(),
            Papers = study.Papers.Where(p => !p.IsDeleted).Select(p => p.ToDto()).ToList(),
            CurrentUserPermissions = permissions,
            CreatedAt = study.CreatedAt,
            CreatedBy = study.CreatedBy,
            ModifiedAt = study.ModifiedAt,
            ModifiedBy = study.ModifiedBy
        };
    }

    public static StudySummaryDto ToSummaryDto(this Study study)
    {
        return new StudySummaryDto
        {
            Id = study.Id.ToString(),
            OwnerId = study.OwnerId.ToString(),
            Title = study.Title.Value,
            Description = study.Description?.Value,
            ResearchField = study.ResearchField.Name,
            ResearchFieldId = study.ResearchField.Id,
            Status = study.Status.Name,
            StatusId = study.Status.Id,
            Institution = study.Institution,
            PrincipalInvestigator = study.PrincipalInvestigator,
            IsFeatured = study.IsFeatured,
            Tags = study.Tags,
            ViewsCount = study.Metrics.ViewsCount,
            StarsCount = study.Metrics.StarsCount,
            MemberCount = study.Members.Count,
            PaperCount = study.Papers.Count(p => !p.IsDeleted),
            CreatedAt = study.CreatedAt,
            ModifiedAt = study.ModifiedAt
        };
    }

    public static IReadOnlyList<StudySummaryDto> ToSummaryDtos(this IEnumerable<Study> studies)
    {
        return studies.Select(s => s.ToSummaryDto()).ToList();
    }

    public static StudyMemberDto ToDto(this StudyMember member)
    {
        return member.ToDto(null, null);
    }

    public static StudyMemberDto ToDto(
        this StudyMember member,
        Dictionary<string, User>? userLookup,
        Dictionary<string, Profile>? profileLookup)
    {
        var memberUserId = member.UserId.ToString();
        User? user = null;
        Profile? profile = null;

        userLookup?.TryGetValue(memberUserId, out user);
        profileLookup?.TryGetValue(memberUserId, out profile);

        var userName = profile is not null
            ? profile.FullName
            : user?.Username.Value;

        return new StudyMemberDto
        {
            UserId = memberUserId,
            Role = member.Role.Name,
            RoleId = member.Role.Id,
            JoinedAt = member.JoinedAt,
            InvitedBy = member.InvitedBy?.ToString(),
            UserName = string.IsNullOrWhiteSpace(userName) ? null : userName,
            UserEmail = user?.Email.Value,
            UserAvatarUrl = profile?.Photo.ThumbnailUrl ?? profile?.Photo.Url
        };
    }

    public static IReadOnlyList<StudyMemberDto> ToDtos(this IEnumerable<StudyMember> members)
    {
        return members.Select(m => m.ToDto()).ToList();
    }

    public static StudyPaperDto ToDto(this StudyPaper paper)
    {
        return new StudyPaperDto
        {
            Id = paper.Id.ToString(),
            Title = paper.Title,
            Authors = paper.Authors,
            Doi = paper.Doi,
            Abstract = paper.Abstract,
            Journal = paper.Journal,
            PublicationYear = paper.PublicationYear,
            FileId = paper.FileId,
            FileName = paper.FileName,
            FileSizeBytes = paper.FileSizeBytes,
            HasFile = paper.HasFile,
            CreatedAt = paper.CreatedAt,
            CreatedBy = paper.CreatedBy
        };
    }

    public static IReadOnlyList<StudyPaperDto> ToDtos(this IEnumerable<StudyPaper> papers)
    {
        return papers.Select(p => p.ToDto()).ToList();
    }

    public static StudyInvitationDto ToDto(this StudyInvitation invitation)
    {
        return new StudyInvitationDto
        {
            Id = invitation.Id.ToString(),
            StudyId = invitation.StudyId.ToString(),
            Email = invitation.Email,
            Role = invitation.Role.Name,
            RoleId = invitation.Role.Id,
            Status = invitation.Status.Name,
            StatusId = invitation.Status.Id,
            InvitedBy = invitation.InvitedBy.ToString(),
            Token = invitation.Token,
            ExpiresAt = invitation.ExpiresAt,
            RespondedAt = invitation.RespondedAt,
            Message = invitation.Message,
            CreatedAt = invitation.CreatedAt,
            ModifiedAt = invitation.ModifiedAt
        };
    }

    public static IReadOnlyList<StudyInvitationDto> ToDtos(this IEnumerable<StudyInvitation> invitations)
    {
        return invitations.Select(i => i.ToDto()).ToList();
    }

    public static ResearchFieldDto ToDto(this ResearchField field)
    {
        return new ResearchFieldDto
        {
            Id = field.Id,
            Name = field.Name,
            DisplayName = field.DisplayName
        };
    }

    public static IReadOnlyList<ResearchFieldDto> ToDtos(this IEnumerable<ResearchField> fields)
    {
        return fields.Select(f => f.ToDto()).ToList();
    }
}
