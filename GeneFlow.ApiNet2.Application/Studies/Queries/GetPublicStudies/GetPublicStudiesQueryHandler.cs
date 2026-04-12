using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetPublicStudies;

/// <summary>
/// Handler for GetPublicStudiesQuery.
/// </summary>
public sealed class GetPublicStudiesQueryHandler
    : IQueryHandler<GetPublicStudiesQuery, Result<PagedList<StudySummaryDto>>>
{
    private readonly IStudyRepository _studyRepository;

    public GetPublicStudiesQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<Result<PagedList<StudySummaryDto>>> Handle(
        GetPublicStudiesQuery request,
        CancellationToken cancellationToken)
    {
        // Validate research field if provided
        ResearchField? researchField = null;
        if (request.ResearchFieldId.HasValue)
        {
            researchField = ResearchField.FromId(request.ResearchFieldId.Value);
            if (researchField is null)
                return Result.Failure<PagedList<StudySummaryDto>>(StudyErrors.InvalidResearchField);
        }

        // Get published studies
        var studies = await _studyRepository.GetPublishedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            researchField,
            request.Tags,
            request.SortBy,
            request.SortDescending,
            cancellationToken);

        // Map to DTOs
        var dtos = studies.Items.ToSummaryDtos();

        return Result.Success(PagedList<StudySummaryDto>.Create(
            dtos,
            studies.PageNumber,
            studies.PageSize,
            studies.TotalCount));
    }
}
