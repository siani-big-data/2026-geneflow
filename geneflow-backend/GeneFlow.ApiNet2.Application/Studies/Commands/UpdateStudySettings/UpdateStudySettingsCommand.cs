using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.UpdateStudySettings;

/// <summary>
/// Command to update study settings.
/// Requires Admin role or higher in the study.
/// </summary>
public sealed record UpdateStudySettingsCommand(
    string StudyId,
    string UserId,
    bool AllowPublicComments,
    bool AllowDataDownload,
    bool RequireApprovalToJoin) : ICommand<Result<StudyDto>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Admin;
}
