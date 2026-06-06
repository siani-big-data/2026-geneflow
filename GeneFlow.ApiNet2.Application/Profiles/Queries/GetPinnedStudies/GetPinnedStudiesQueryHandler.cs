using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetPinnedStudies;

public sealed class GetPinnedStudiesQueryHandler
    : IQueryHandler<GetPinnedStudiesQuery, Result<IReadOnlyList<PinnedStudyDto>>>
{
    private readonly IProfileRepository _profileRepository;

    public GetPinnedStudiesQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<Result<IReadOnlyList<PinnedStudyDto>>> Handle(
        GetPinnedStudiesQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<IReadOnlyList<PinnedStudyDto>>(ProfileErrors.InvalidUserId);

        var pinned = await _profileRepository.GetPinnedStudiesAsync(userId, cancellationToken);

        IReadOnlyList<PinnedStudyDto> dtos = pinned
            .OrderBy(p => p.Order)
            .Select(p => new PinnedStudyDto(p.StudyId.ToString(), p.Order, p.PinnedAt))
            .ToList();

        return Result.Success(dtos);
    }
}
