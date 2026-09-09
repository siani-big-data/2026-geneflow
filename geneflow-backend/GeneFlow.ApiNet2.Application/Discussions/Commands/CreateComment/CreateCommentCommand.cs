using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.CreateComment;

public sealed record CreateCommentCommand(
    string StudyId,
    string DiscussionId,
    string UserId,
    string BodyMarkdown) : ICommand<Result<CommentDto>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Viewer;
}
