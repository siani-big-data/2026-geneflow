using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetFeaturedStudies;

/// <summary>
/// Handler for GetFeaturedStudiesQuery.
/// </summary>
public sealed class GetFeaturedStudiesQueryHandler
    : IQueryHandler<GetFeaturedStudiesQuery, Result<IReadOnlyList<StudySummaryDto>>>
{
    private readonly IStudyRepository _studyRepository;

    public GetFeaturedStudiesQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<Result<IReadOnlyList<StudySummaryDto>>> Handle(
        GetFeaturedStudiesQuery request,
        CancellationToken cancellationToken)
    {
        var studies = await _studyRepository.GetFeaturedAsync(request.Limit, cancellationToken);
        return Result.Success(studies.ToSummaryDtos());
    }
}
