using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.EditComment;

/// <summary>
/// Edit a comment. The Comment aggregate enforces author-only.
/// We can't use IRequireStudyMembership here because the comment may
/// belong to a non-Discussion parent in the future; the handler does its
/// own authentication check.
/// </summary>
public sealed record EditCommentCommand(
    Guid CommentId,
    string UserId,
    string BodyMarkdown) : ICommand<Result<CommentDto>>, IRequireAuthentication;
