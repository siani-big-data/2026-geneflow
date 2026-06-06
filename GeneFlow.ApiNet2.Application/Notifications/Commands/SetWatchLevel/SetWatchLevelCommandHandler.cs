using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Entities;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Notifications.Commands.SetWatchLevel;

public sealed class SetWatchLevelCommandHandler : ICommandHandler<SetWatchLevelCommand, Result>
{
    private readonly IWatchRepository _watchRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly INotificationUnitOfWork _unitOfWork;

    public SetWatchLevelCommandHandler(
        IWatchRepository watchRepository,
        IStudyRepository studyRepository,
        INotificationUnitOfWork unitOfWork)
    {
        _watchRepository = watchRepository;
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetWatchLevelCommand request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(NotificationErrors.NotRecipient);

        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure(WatchErrors.NotFound);

        if (!WatchLevel.TryFromName(request.Level, out var level) || level is null)
            return Result.Failure(WatchErrors.NotFound);

        // Verify the study exists / is readable. We allow watch for any
        // user with at least public read access — members or public-study viewers.
        var isMember = await _studyRepository.GetMemberRoleAsync(studyId, userId, cancellationToken) is not null;
        if (!isMember)
        {
            var isPublic = await _studyRepository.IsPublicStudyAsync(studyId, cancellationToken);
            if (!isPublic)
                return Result.Failure(NotificationErrors.NotRecipient);
        }

        var existing = await _watchRepository.GetAsync(userId, studyId, cancellationToken);
        if (existing is null)
        {
            var watch = Watch.Create(userId, studyId, level);
            await _watchRepository.AddAsync(watch, cancellationToken);
        }
        else
        {
            existing.SetLevel(level);
            _watchRepository.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
