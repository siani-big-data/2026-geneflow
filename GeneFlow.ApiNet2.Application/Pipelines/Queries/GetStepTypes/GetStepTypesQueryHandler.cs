using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetStepTypes;

/// <summary>
/// Handler for GetStepTypesQuery.
/// </summary>
public sealed class GetStepTypesQueryHandler
    : IQueryHandler<GetStepTypesQuery, Result<IReadOnlyList<StepTypeDto>>>
{
    public Task<Result<IReadOnlyList<StepTypeDto>>> Handle(
        GetStepTypesQuery request,
        CancellationToken cancellationToken)
    {
        var stepTypes = StepType.GetAll().ToDtos();
        return Task.FromResult(Result.Success(stepTypes));
    }
}
