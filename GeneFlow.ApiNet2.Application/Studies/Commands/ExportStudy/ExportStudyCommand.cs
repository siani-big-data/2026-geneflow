using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.ExportStudy;

/// <summary>
/// Builds the projection used to stream a ZIP archive containing the full
/// study (metadata, members, traces, papers). Any member of the study can
/// trigger the export; non-members get a 404-equivalent failure.
/// </summary>
public sealed record ExportStudyCommand(
    string StudyId,
    string UserId) : ICommand<Result<ExportStudyDto>>;
