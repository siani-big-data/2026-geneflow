using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.RecordStudyView;

/// <summary>
/// Handler for RecordStudyViewCommand.
/// </summary>
public sealed class RecordStudyViewCommandHandler
    : ICommandHandler<RecordStudyViewCommand, Result>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;
    private static readonly TimeSpan ViewDeduplicationWindow = TimeSpan.FromHours(24);

    public RecordStudyViewCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        RecordStudyViewCommand request,
        CancellationToken cancellationToken)
    {
        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure(StudyErrors.NotFound);

        // Parse user ID if provided
        UserId? userId = null;
        if (!string.IsNullOrEmpty(request.UserId))
        {
            if (!UserId.TryParse(request.UserId, out userId))
                return Result.Failure(StudyErrors.InvalidUserId);
        }

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure(StudyErrors.NotFound);

        // Check for duplicate view within time window
        var cutoffTime = DateTime.UtcNow.Subtract(ViewDeduplicationWindow);
        bool isDuplicate;

        if (userId is not null)
        {
            isDuplicate = await _studyRepository.HasRecentViewByUserAsync(
                studyId, userId, cutoffTime, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(request.IpHash))
        {
            isDuplicate = await _studyRepository.HasRecentViewByIpHashAsync(
                studyId, request.IpHash, cutoffTime, cancellationToken);
        }
        else
        {
            // No deduplication possible without user or IP
            isDuplicate = false;
        }

        if (isDuplicate)
        {
            // Don't count as new view but don't return error
            return Result.Success();
        }

        // Create and add view
        StudyView view;
        if (userId is not null)
        {
            view = StudyView.CreateForUser(studyId, userId, request.UserAgent);
        }
        else
        {
            view = StudyView.CreateForAnonymous(studyId, request.IpHash ?? "unknown", request.UserAgent);
        }

        await _studyRepository.AddViewAsync(view, cancellationToken);

        // Increment view count
        study.IncrementViews();

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
