using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.AcceptInvitation;

/// <summary>
/// Handler for AcceptInvitationCommand.
/// </summary>
public sealed class AcceptInvitationCommandHandler
    : ICommandHandler<AcceptInvitationCommand, Result<StudyDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyInvitationRepository _invitationRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public AcceptInvitationCommandHandler(
        IStudyRepository studyRepository,
        IStudyInvitationRepository invitationRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StudyDto>> Handle(
        AcceptInvitationCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidUserId);

        // Get invitation by token
        var invitation = await _invitationRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (invitation is null)
            return Result.Failure<StudyDto>(StudyErrors.InvitationNotFound);

        // Accept invitation
        var acceptResult = invitation.Accept();
        if (acceptResult.IsFailure)
            return Result.Failure<StudyDto>(acceptResult.Error);

        // Get study with members loaded so AddMember's permission check finds
        // the inviter and EF's owned-collection snapshot matches the DB state
        // (study_members is mapped to a separate table and is NOT auto-included).
        var study = await _studyRepository.GetByIdWithMembersAsync(invitation.StudyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        // Add user to study
        var addResult = study.AddMember(userId, invitation.Role, invitation.InvitedBy);
        if (addResult.IsFailure)
            return Result.Failure<StudyDto>(addResult.Error);

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(study.ToDto());
    }
}
