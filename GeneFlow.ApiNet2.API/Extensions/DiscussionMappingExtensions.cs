using GeneFlow.ApiNet2.API.Contracts.Discussions.Responses;
using GeneFlow.ApiNet2.Application.Discussions.DTOs;

namespace GeneFlow.ApiNet2.API.Extensions;

public static class DiscussionMappingExtensions
{
    public static DiscussionResponse ToResponse(this DiscussionDto dto) => new(
        dto.Id,
        dto.StudyId,
        dto.AuthorId,
        dto.Title,
        dto.Category,
        dto.IsLocked,
        dto.LockedAt,
        dto.LockedBy,
        dto.CreatedAt,
        dto.ModifiedAt,
        dto.CommentCount,
        dto.Comments?.Select(c => c.ToResponse()).ToList());

    public static CommentResponse ToResponse(this CommentDto dto) => new(
        Guid.Parse(dto.Id),
        dto.ParentType,
        dto.ParentId,
        dto.AuthorId,
        dto.BodyMarkdown,
        dto.CreatedAt,
        dto.EditedAt,
        dto.IsDeleted,
        dto.CanEdit,
        dto.CanDelete,
        dto.Reactions.Select(r => r.ToResponse()).ToList());

    public static ReactionSummaryResponse ToResponse(this ReactionSummaryDto dto) =>
        new(dto.Emoji, dto.Count, dto.ReactedByMe);
}
