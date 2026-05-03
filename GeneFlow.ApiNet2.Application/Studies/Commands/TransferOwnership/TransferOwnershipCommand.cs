using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.TransferOwnership;

/// <summary>
/// Command to transfer study ownership to another member.
/// Requires Owner role.
/// </summary>
public sealed record TransferOwnershipCommand(
    string StudyId,
    string CurrentOwnerId,
    string NewOwnerId) : ICommand<Result<StudyDto>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Owner;
}
