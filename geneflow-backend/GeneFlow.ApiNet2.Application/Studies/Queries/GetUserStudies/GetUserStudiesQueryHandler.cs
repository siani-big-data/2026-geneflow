using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetUserStudies;

/// <summary>
/// Handler for GetUserStudiesQuery.
/// </summary>
public sealed class GetUserStudiesQueryHandler
    : IQueryHandler<GetUserStudiesQuery, Result<PagedList<StudySummaryDto>>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly ILogger<GetUserStudiesQueryHandler> _logger;

    public GetUserStudiesQueryHandler(
        IStudyRepository studyRepository,
        ILogger<GetUserStudiesQueryHandler> logger)
    {
        _studyRepository = studyRepository;
        _logger = logger;
    }

    public async Task<Result<PagedList<StudySummaryDto>>> Handle(
        GetUserStudiesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetUserStudies: Requested for UserId={UserId}", request.UserId);

        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
        {
            _logger.LogWarning("GetUserStudies: Invalid UserId format: {UserId}", request.UserId);
            return Result.Failure<PagedList<StudySummaryDto>>(StudyErrors.InvalidUserId);
        }

        // Validate status if provided
        StudyStatus? status = null;
        if (request.StatusId.HasValue)
        {
            status = StudyStatus.FromId(request.StatusId.Value);
            if (status is null)
                return Result.Failure<PagedList<StudySummaryDto>>(StudyErrors.InvalidStatus);
        }

        // Validate research field if provided
        ResearchField? researchField = null;
        if (request.ResearchFieldId.HasValue)
        {
            researchField = ResearchField.FromId(request.ResearchFieldId.Value);
            if (researchField is null)
                return Result.Failure<PagedList<StudySummaryDto>>(StudyErrors.InvalidResearchField);
        }

        // Get studies
        var studies = await _studyRepository.GetByMemberAsync(
            userId,
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            status,
            researchField,
            cancellationToken);

        _logger.LogInformation(
            "GetUserStudies: Found {Count} studies for UserId={UserId}. StudyIds: [{StudyIds}]",
            studies.TotalCount,
            userId,
            string.Join(", ", studies.Items.Select(s => s.Id.ToString())));

        // Map to DTOs
        var dtos = studies.Items.ToSummaryDtos();

        return Result.Success(PagedList<StudySummaryDto>.Create(
            dtos,
            studies.PageNumber,
            studies.PageSize,
            studies.TotalCount));
    }
}
