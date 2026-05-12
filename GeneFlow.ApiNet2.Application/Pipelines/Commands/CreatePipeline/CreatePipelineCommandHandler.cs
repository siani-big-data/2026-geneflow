using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.CreatePipeline;

/// <summary>
/// Handler for CreatePipelineCommand.
/// </summary>
public sealed class CreatePipelineCommandHandler
    : ICommandHandler<CreatePipelineCommand, Result<PipelineDto>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;
    private readonly ISequenceGenerator _sequenceGenerator;

    public CreatePipelineCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineUnitOfWork unitOfWork,
        ISequenceGenerator sequenceGenerator)
    {
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<Result<PipelineDto>> Handle(
        CreatePipelineCommand request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.InvalidUserId);

        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.InvalidStudyId);

        var nameResult = PipelineName.Create(request.Name);
        if (nameResult.IsFailure)
            return Result.Failure<PipelineDto>(nameResult.Error);

        var descriptionResult = PipelineDescription.Create(request.Description);
        if (descriptionResult.IsFailure)
            return Result.Failure<PipelineDto>(descriptionResult.Error);

        if (await _pipelineRepository.NameExistsInStudyAsync(studyId, request.Name, null, cancellationToken))
            return Result.Failure<PipelineDto>(PipelineErrors.NameRequired);

        var sequenceValue = await _sequenceGenerator.NextAsync(
            PipelineId.SequenceName,
            cancellationToken);
        var pipelineId = PipelineId.FromSequence(sequenceValue);

        var pipelineResult = Pipeline.Create(
            pipelineId,
            studyId,
            userId,
            nameResult.Value,
            descriptionResult.Value);

        if (pipelineResult.IsFailure)
            return Result.Failure<PipelineDto>(pipelineResult.Error);

        var pipeline = pipelineResult.Value;

        await _pipelineRepository.AddAsync(pipeline, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(pipeline.ToDto());
    }
}
