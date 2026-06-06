using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.TransferStudyOwnership;

public sealed record TransferStudyOwnershipCommand(
    string StudyId,
    string NewOwnerType,
    string NewOwnerId) : ICommand<Result>, IRequireAuthentication;
