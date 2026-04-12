using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.UpdateStudySettings;

/// <summary>
/// Command to update study settings.
/// </summary>
public sealed record UpdateStudySettingsCommand(
    string StudyId,
    string UserId,
    bool AllowPublicComments,
    bool AllowDataDownload,
    bool RequireApprovalToJoin) : ICommand<Result<StudyDto>>;
