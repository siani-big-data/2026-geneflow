using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Entities;
using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using IDomainEventDispatcher = GeneFlow.ApiNet2.SharedKernel.Infrastructure.IDomainEventDispatcher;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.UpdatePinnedStudies;

public sealed class UpdatePinnedStudiesCommandHandler
    : ICommandHandler<UpdatePinnedStudiesCommand, Result>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UpdatePinnedStudiesCommandHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result> Handle(
        UpdatePinnedStudiesCommand request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(ProfileErrors.InvalidUserId);

        var studyIds = request.StudyIds ?? Array.Empty<string>();

        if (studyIds.Count > PinnedStudy.MaxPinnedPerUser)
            return Result.Failure(ProfileErrors.TooManyPinned);

        if (studyIds.Distinct(StringComparer.Ordinal).Count() != studyIds.Count)
            return Result.Failure(ProfileErrors.DuplicatePinned);

        var parsed = new List<StudyId>(studyIds.Count);
        foreach (var raw in studyIds)
        {
            if (!StudyId.TryParse(raw, out var studyId) || studyId is null)
                return Result.Failure(ProfileErrors.InvalidStudyId);

            parsed.Add(studyId);
        }

        var previous = await _profileRepository.ReplacePinnedStudiesAsync(
            userId,
            parsed,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Emit events: unpins for previous-not-in-new, pins for new-not-in-previous.
        var previousSet = previous.Select(s => s.ToString()).ToHashSet(StringComparer.Ordinal);
        var newSet = parsed.Select(s => s.ToString()).ToHashSet(StringComparer.Ordinal);

        var events = new List<SharedKernel.Application.EventNotifications.IDomainEvent>();

        foreach (var prev in previous)
        {
            if (!newSet.Contains(prev.ToString()))
                events.Add(new StudyUnpinnedEvent(userId, prev));
        }

        for (var i = 0; i < parsed.Count; i++)
        {
            if (!previousSet.Contains(parsed[i].ToString()))
                events.Add(new StudyPinnedEvent(userId, parsed[i], i));
        }

        if (events.Count > 0)
            await _eventDispatcher.DispatchAsync(events, cancellationToken);

        return Result.Success();
    }
}
