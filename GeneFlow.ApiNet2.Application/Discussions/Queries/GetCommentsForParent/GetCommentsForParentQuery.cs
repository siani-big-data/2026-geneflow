using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Queries.GetCommentsForParent;

/// <summary>
/// Generic query reserved for future Annotation/Trace contexts. Returns
/// the comments hanging off any (parentType, parentId) pair. Authorization
/// is the caller's responsibility — the future endpoints for those contexts
/// will check read access on the parent before invoking.
/// </summary>
public sealed record GetCommentsForParentQuery(
    string ParentType,
    string ParentId,
    string? UserId) : IQuery<Result<IReadOnlyList<CommentDto>>>, IRequireAuthentication;
