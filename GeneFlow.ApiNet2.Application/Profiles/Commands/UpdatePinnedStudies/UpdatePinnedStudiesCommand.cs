using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.UpdatePinnedStudies;

/// <summary>
/// Replaces the current user's pinned studies with the provided ordered list.
/// Order matches the index of each study id in <see cref="StudyIds"/>.
/// </summary>
public sealed record UpdatePinnedStudiesCommand(
    string UserId,
    IReadOnlyList<string> StudyIds) : ICommand<Result>, IRequireAuthentication;
