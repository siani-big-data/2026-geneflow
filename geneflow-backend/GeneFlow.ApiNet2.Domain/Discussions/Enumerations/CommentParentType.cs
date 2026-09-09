using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Discussions.Enumerations;

/// <summary>
/// Type of entity a <see cref="Entities.Comment"/> belongs to.
/// Comments are polymorphic — they can hang off a Discussion now, and
/// future contexts (annotations on uploaded papers, comments on individual
/// pipeline traces) without requiring schema changes.
/// </summary>
public sealed class CommentParentType : Enumeration<CommentParentType>
{
    /// <summary>
    /// Comment attached to a <see cref="Discussion"/> thread.
    /// </summary>
    public static readonly CommentParentType Discussion = new(1, nameof(Discussion));

    /// <summary>
    /// Reserved for a future annotation context (paper / figure).
    /// Not wired up in Phase 4 — kept here so persistence + queries don't need
    /// migration when we light it up.
    /// </summary>
    public static readonly CommentParentType Annotation = new(2, nameof(Annotation));

    /// <summary>
    /// Reserved for comments attached to a pipeline trace.
    /// Not wired up in Phase 4.
    /// </summary>
    public static readonly CommentParentType Trace = new(3, nameof(Trace));

    private CommentParentType(int id, string name) : base(id, name) { }
}
