using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetPipelineById;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Application.Pipelines.Queries;

/// <summary>
/// Unit tests for GetPipelineByIdQueryHandler.
/// </summary>
public class GetPipelineByIdQueryHandlerTests
{
    private readonly IPipelineRepository _pipelineRepository = Substitute.For<IPipelineRepository>();
    private readonly GetPipelineByIdQueryHandler _handler;

    public GetPipelineByIdQueryHandlerTests()
    {
        _handler = new GetPipelineByIdQueryHandler(_pipelineRepository);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnPipelineDto()
    {
        // Arrange
        var pipeline = CreateDraftPipeline();
        var query = new GetPipelineByIdQuery("U00000001", pipeline.StudyId.ToString(), pipeline.Id.ToString());

        _pipelineRepository.GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(pipeline.Id.ToString());
        result.Value.Name.Should().Be(pipeline.Name.Value);
    }

    [Fact]
    public async Task Handle_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var pipeline = CreateActivePipelineWithSteps();
        var query = new GetPipelineByIdQuery("U00000001", pipeline.StudyId.ToString(), pipeline.Id.ToString());

        _pipelineRepository.GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(pipeline.Id.ToString());
        result.Value.StudyId.Should().Be(pipeline.StudyId.ToString());
        result.Value.OwnerId.Should().Be(pipeline.OwnerId.ToString());
        result.Value.Name.Should().Be(pipeline.Name.Value);
        result.Value.Description.Should().Be(pipeline.Description.Value);
        result.Value.StatusId.Should().Be(pipeline.Status.Id);
        result.Value.StatusName.Should().Be(pipeline.Status.DisplayName);
        result.Value.CanBeEdited.Should().Be(pipeline.CanBeEdited);
        result.Value.CanBeExecuted.Should().Be(pipeline.CanBeExecuted);
        result.Value.StepCount.Should().Be(pipeline.Steps.Count);
        result.Value.EnabledStepCount.Should().Be(pipeline.EnabledStepCount);
        result.Value.Steps.Should().HaveCount(pipeline.Steps.Count);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var pipelineId = PipelineId.FromSequence(123);
        var pipeline = CreateDraftPipeline(pipelineId);
        var query = new GetPipelineByIdQuery("U00000001", pipeline.StudyId.ToString(), pipelineId.ToString());

        _pipelineRepository.GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _pipelineRepository.Received(1).GetByIdWithStepsAsync(
            Arg.Is<PipelineId>(id => id.Value == pipelineId.Value),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidPipelineId_ShouldReturnNotFoundError()
    {
        // Arrange
        var query = new GetPipelineByIdQuery("U00000001", "S00000001", "invalid-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentPipeline_ShouldReturnNotFoundError()
    {
        // Arrange
        var pipelineId = PipelineId.FromSequence(999);
        var query = new GetPipelineByIdQuery("U00000001", "S00000001", pipelineId.ToString());

        _pipelineRepository.GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns((Pipeline?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Helper Methods

    private static Pipeline CreateDraftPipeline(PipelineId? id = null)
    {
        var pipelineId = id ?? PipelineId.FromSequence(1);
        var studyId = StudyId.FromSequence(1);
        var userId = UserId.Parse("U00000001");
        var name = PipelineName.Create("Test Pipeline").Value;
        var description = PipelineDescription.Create("Test description").Value;

        return Pipeline.Create(pipelineId, studyId, userId, name, description).Value;
    }

    private static Pipeline CreateActivePipelineWithSteps(PipelineId? id = null)
    {
        var pipeline = CreateDraftPipeline(id);
        var userId = UserId.Parse("U00000001");

        // Add a step
        var config = StepConfiguration.Create("{}", StepType.Quality).Value;
        pipeline.AddStep(StepType.Quality, config, "Quality Check", true, userId);

        // Activate
        pipeline.Activate(userId);

        return pipeline;
    }

    #endregion
}
