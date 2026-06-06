using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Discussions.Events;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Discussions.Entities;

/// <summary>
/// A user-authored comment attached polymorphically to a parent
/// (currently only <see cref="CommentParentType.Discussion"/>; Annotation
/// and Trace are reserved).
///
/// Comment is an independent aggregate root in its own right — it does
/// not live inside the Discussion aggregate. This lets us reuse the same
/// table and queries across multiple parent contexts.
/// </summary>
public sealed class Comment : AggregateRoot<Guid>
{
    public const int MaxBodyLength = 10_000;

    public CommentParentType ParentType { get; private set; } = null!;

    /// <summary>
    /// String form of the parent ID (e.g. "D00000001"). Stored as a
    /// string so the same column can reference any parent context.
    /// </summary>
    public string ParentId { get; private set; } = null!;

    public UserId AuthorId { get; private set; } = null!;

    public string BodyMarkdown { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public DateTime? EditedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    private Comment() : base() { }

    private Comment(
        Guid id,
        CommentParentType parentType,
        string parentId,
        UserId authorId,
        string bodyMarkdown) : base(id)
    {
        ParentType = parentType;
        ParentId = parentId;
        AuthorId = authorId;
        BodyMarkdown = bodyMarkdown;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Comment> Create(
        CommentParentType parentType,
        string parentId,
        UserId authorId,
        string bodyMarkdown)
    {
        if (string.IsNullOrWhiteSpace(bodyMarkdown))
            return Result.Failure<Comment>(CommentErrors.BodyRequired);

        if (bodyMarkdown.Length > MaxBodyLength)
            return Result.Failure<Comment>(CommentErrors.BodyTooLong);

        var comment = new Comment(Guid.NewGuid(), parentType, parentId, authorId, bodyMarkdown);

        comment.RaiseDomainEvent(new CommentCreatedEvent(
            comment.Id,
            comment.ParentType,
            comment.ParentId,
            comment.AuthorId,
            comment.BodyMarkdown));

        return comment;
    }

    public Result Edit(UserId editor, string newBody)
    {
        if (IsDeleted)
            return Result.Failure(CommentErrors.AlreadyDeleted);

        if (editor != AuthorId)
            return Result.Failure(CommentErrors.NotAuthor);

        if (string.IsNullOrWhiteSpace(newBody))
            return Result.Failure(CommentErrors.BodyRequired);

        if (newBody.Length > MaxBodyLength)
            return Result.Failure(CommentErrors.BodyTooLong);

        BodyMarkdown = newBody;
        EditedAt = DateTime.UtcNow;

        RaiseDomainEvent(new CommentEditedEvent(Id, editor, EditedAt.Value));
        return Result.Success();
    }

    /// <summary>
    /// Soft-deletes the comment. Admins may delete any comment;
    /// non-admin callers must be the original author.
    /// </summary>
    public Result Delete(UserId deletedBy, bool isAdmin)
    {
        if (IsDeleted)
            return Result.Failure(CommentErrors.AlreadyDeleted);

        if (!isAdmin && deletedBy != AuthorId)
            return Result.Failure(CommentErrors.NotAuthor);

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        // We keep the body so admins / audit can still read it;
        // the application layer is responsible for redacting in DTOs.

        RaiseDomainEvent(new CommentDeletedEvent(Id, deletedBy));
        return Result.Success();
    }
}
