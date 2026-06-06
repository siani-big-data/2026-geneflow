using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.Application.Orgs.Mappings;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.CreateOrg;

public sealed class CreateOrgCommandHandler
    : ICommandHandler<CreateOrgCommand, Result<OrgDto>>
{
    private readonly IOrgRepository _orgRepository;
    private readonly IOrgUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateOrgCommandHandler(
        IOrgRepository orgRepository,
        IOrgUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<OrgDto>> Handle(
        CreateOrgCommand request,
        CancellationToken cancellationToken)
    {
        var creator = _currentUser.UserId;
        if (creator is null)
            return Result.Failure<OrgDto>(OrgErrors.Forbidden);

        var normalizedHandle = (request.Handle ?? string.Empty).Trim().ToLowerInvariant();

        if (await _orgRepository.HandleExistsAsync(normalizedHandle, cancellationToken))
            return Result.Failure<OrgDto>(OrgErrors.HandleTaken);

        var sequence = await _orgRepository.GetNextSequenceValueAsync(cancellationToken);
        var orgId = OrgId.FromSequence(sequence);

        var createResult = Org.Create(orgId, request.Handle, request.Name, creator);
        if (createResult.IsFailure)
            return Result.Failure<OrgDto>(createResult.Error);

        var org = createResult.Value;

        if (!string.IsNullOrWhiteSpace(request.Visibility))
        {
            var vis = Enumeration<OrgVisibility>.FromName(request.Visibility);
            if (vis is not null)
            {
                org.ChangeVisibility(vis, creator);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            org.UpdateProfile(org.Name, request.Description, null, null, null, creator);
        }

        await _orgRepository.AddAsync(org, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(org.ToDto(creator));
    }
}
