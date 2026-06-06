using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.Application.Discussions.Mappings;

public static class DiscussionMappings
{
    public static DiscussionDto ToDto(
        this Discussion discussion,
        int commentCount,
        IReadOnlyList<CommentDto>? comments = null) => new()
        {
            Id = discussion.Id.ToString(),
            StudyId = discussion.StudyId.ToString(),
            AuthorId = discussion.AuthorId.ToString(),
            Title = discussion.Title,
            Category = discussion.Category,
            IsLocked = discussion.IsLocked,
            LockedAt = discussion.LockedAt,
            LockedBy = discussion.LockedBy?.ToString(),
            CreatedAt = discussion.CreatedAt,
            ModifiedAt = discussion.ModifiedAt,
            CommentCount = commentCount,
            Comments = comments
        };

    public static CommentDto ToDto(
        this Comment comment,
        UserId? currentUser,
        bool isAdmin,
        IReadOnlyList<ReactionSummaryDto> reactions) => new()
        {
            Id = comment.Id.ToString(),
            ParentType = comment.ParentType.Name,
            ParentId = comment.ParentId,
            AuthorId = comment.AuthorId.ToString(),
            BodyMarkdown = comment.IsDeleted ? string.Empty : comment.BodyMarkdown,
            CreatedAt = comment.CreatedAt,
            EditedAt = comment.EditedAt,
            IsDeleted = comment.IsDeleted,
            CanEdit = !comment.IsDeleted && currentUser is not null && currentUser == comment.AuthorId,
            CanDelete = !comment.IsDeleted && currentUser is not null && (isAdmin || currentUser == comment.AuthorId),
            Reactions = reactions
        };

    /// <summary>
    /// Aggregates a flat list of reactions across many comments into a
    /// per-comment summary keyed by emoji.
    /// </summary>
    public static IReadOnlyDictionary<Guid, IReadOnlyList<ReactionSummaryDto>> SummariseReactions(
        IEnumerable<Reaction> reactions,
        UserId? currentUser)
    {
        var byComment = reactions
            .GroupBy(r => r.CommentId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ReactionSummaryDto>)g
                    .GroupBy(r => r.Emoji)
                    .Select(eg => new ReactionSummaryDto(
                        eg.Key,
                        eg.Count(),
                        currentUser is not null && eg.Any(r => r.UserId == currentUser)))
                    .OrderByDescending(s => s.Count)
                    .ToList());

        return byComment;
    }
}
