using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetResearchFields;

/// <summary>
/// Handler for GetResearchFieldsQuery.
/// </summary>
public sealed class GetResearchFieldsQueryHandler
    : IQueryHandler<GetResearchFieldsQuery, Result<IReadOnlyList<ResearchFieldDto>>>
{
    public Task<Result<IReadOnlyList<ResearchFieldDto>>> Handle(
        GetResearchFieldsQuery request,
        CancellationToken cancellationToken)
    {
        var fields = ResearchField.GetAll().ToDtos();
        return Task.FromResult(Result.Success(fields));
    }
}
