using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.AddStudyPaper;

/// <summary>
/// Handler for AddStudyPaperCommand.
/// </summary>
public sealed class AddStudyPaperCommandHandler
    : ICommandHandler<AddStudyPaperCommand, Result<StudyPaperDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;
    private readonly ISequenceGenerator _sequenceGenerator;

    public AddStudyPaperCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork,
        ISequenceGenerator sequenceGenerator)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<Result<StudyPaperDto>> Handle(
        AddStudyPaperCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyPaperDto>(StudyErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<StudyPaperDto>(StudyErrors.InvalidUserId);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyPaperDto>(StudyErrors.NotFound);

        // Check permission (must be editor or higher)
        var member = study.GetMember(userId);
        if (member is null || !member.Role.CanEditContent)
            return Result.Failure<StudyPaperDto>(StudyErrors.InsufficientPermissions);

        // Generate paper ID
        var sequenceId = await _sequenceGenerator.NextAsync(StudyPaperId.SequenceName, cancellationToken);
        var paperId = StudyPaperId.FromSequence(sequenceId);

        // Create paper
        var paperResult = StudyPaper.Create(
            paperId,
            request.Title,
            request.Authors,
            request.Doi,
            request.Abstract,
            request.Journal,
            request.PublicationYear,
            request.FileId,
            request.FileName,
            request.FileSizeBytes,
            userId);

        if (paperResult.IsFailure)
            return Result.Failure<StudyPaperDto>(paperResult.Error);

        // Add paper to study
        var addResult = study.AddPaper(paperResult.Value, userId);
        if (addResult.IsFailure)
            return Result.Failure<StudyPaperDto>(addResult.Error);

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(paperResult.Value.ToDto());
    }
}
