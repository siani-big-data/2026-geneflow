using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetOrgStudies;

public sealed class GetOrgStudiesQueryHandler
    : IQueryHandler<GetOrgStudiesQuery, Result<PagedList<StudyDto>>>
{
    private readonly IOrgRepository _orgRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly ICurrentUserService _currentUser;

    public GetOrgStudiesQueryHandler(
        IOrgRepository orgRepository,
        IStudyRepository studyRepository,
        ICurrentUserService currentUser)
    {
        _orgRepository = orgRepository;
        _studyRepository = studyRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<StudyDto>>> Handle(
        GetOrgStudiesQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Handle))
            return Result.Failure<PagedList<StudyDto>>(OrgErrors.NotFound);

        var org = await _orgRepository.GetByHandleAsync(
            request.Handle.Trim().ToLowerInvariant(),
            cancellationToken);
        if (org is null)
            return Result.Failure<PagedList<StudyDto>>(OrgErrors.NotFound);

        // Private orgs are only listable by their members.
        var viewer = _currentUser.UserId;
        var isMember = viewer is not null && org.IsMember(viewer);
        if (org.Visibility == OrgVisibility.Private && !isMember)
            return Result.Failure<PagedList<StudyDto>>(OrgErrors.NotFound);

        var page = await _studyRepository.GetByOrgAsync(
            org.Id,
            includeNonPublished: isMember,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = page.Items.Select(s => s.ToDto()).ToList();
        var dtoPage = PagedList<StudyDto>.Create(
            dtos, page.PageNumber, page.PageSize, page.TotalCount);

        return Result.Success(dtoPage);
    }
}
