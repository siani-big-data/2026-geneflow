using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetExecutionById;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;

namespace GeneFlow.ApiNet2.Tests.Application.Pipelines.Queries;

/// <summary>
/// Unit tests for GetExecutionByIdQueryHandler.
/// </summary>
public class GetExecutionByIdQueryHandlerTests
{
    private readonly IPipelineRepository _pipelineRepository = Substitute.For<IPipelineRepository>();
    private readonly IPipelineExecutionRepository _executionRepository = Substitute.For<IPipelineExecutionRepository>();
    private readonly GetExecutionByIdQueryHandler _handler;

    public GetExecutionByIdQueryHandlerTests()
    {
        _handler = new GetExecutionByIdQueryHandler(_pipelineRepository, _executionRepository);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidExecutionId_ShouldReturnExecutionDto()
    {
        // Arrange
        var pipeline = CreateActivePipelineWithSteps();
        var execution = CreateExecution(pipeline);
        var query = new GetExecutionByIdQuery("U00000001", execution.Id.ToString());

        _executionRepository.GetByIdWithStepsAsync(Arg.Any<PipelineExecutionId>(), Arg.Any<CancellationToken>())
            .Returns(execution);
        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(execution.Id.ToString());
        result.Value.PipelineId.Should().Be(pipeline.Id.ToString());
        result.Value.PipelineName.Should().Be(pipeline.Name.Value);
    }

    [Fact]
    public async Task Handle_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var pipeline = CreateActivePipelineWithSteps();
        var execution = CreateExecution(pipeline);
        var query = new GetExecutionByIdQuery("U00000001", execution.Id.ToString());

        _executionRepository.GetByIdWithStepsAsync(Arg.Any<PipelineExecutionId>(), Arg.Any<CancellationToken>())
            .Returns(execution);
        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(execution.Id.ToString());
        result.Value.PipelineId.Should().Be(execution.PipelineId.ToString());
        result.Value.TraceId.Should().Be(execution.TraceId.ToString());
        result.Value.StartedById.Should().Be(execution.StartedBy.ToString());
        result.Value.StatusId.Should().Be(execution.Status.Id);
        result.Value.StatusName.Should().Be(execution.Status.DisplayName);
        result.Value.TotalSteps.Should().Be(execution.TotalSteps);
        result.Value.CompletedSteps.Should().Be(execution.CompletedSteps);
        result.Value.StepExecutions.Should().HaveCount(execution.StepExecutions.Count);
    }

    [Fact]
    public async Task Handle_WhenPipelineNotFound_ShouldReturnUnknownPipelineName()
    {
        // Arrange
        var pipeline = CreateActivePipelineWithSteps();
        var execution = CreateExecution(pipeline);
        var query = new GetExecutionByIdQuery("U00000001", execution.Id.ToString());

        _executionRepository.GetByIdWithStepsAsync(Arg.Any<PipelineExecutionId>(), Arg.Any<CancellationToken>())
            .Returns(execution);
        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns((Pipeline?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PipelineName.Should().Be("Unknown");
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidExecutionId_ShouldReturnNotFoundError()
    {
        // Arrange
        var query = new GetExecutionByIdQuery("U00000001", "invalid-execution-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ExecutionNotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentExecution_ShouldReturnNotFoundError()
    {
        // Arrange
        var executionId = PipelineExecutionId.FromSequence(999);
        var query = new GetExecutionByIdQuery("U00000001", executionId.ToString());

        _executionRepository.GetByIdWithStepsAsync(Arg.Any<PipelineExecutionId>(), Arg.Any<CancellationToken>())
            .Returns((PipelineExecution?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ExecutionNotFound");
    }

    #endregion

    #region Helper Methods

    private static Pipeline CreateActivePipelineWithSteps(PipelineId? id = null)
    {
        var pipelineId = id ?? PipelineId.FromSequence(1);
        var studyId = StudyId.FromSequence(1);
        var userId = UserId.Parse("U00000001");
        var name = PipelineName.Create("Test Pipeline").Value;
        var description = PipelineDescription.Create("Test description").Value;

        var pipeline = Pipeline.Create(pipelineId, studyId, userId, name, description).Value;

        // Add a step
        var config = StepConfiguration.Create("{}", StepType.Quality).Value;
        pipeline.AddStep(StepType.Quality, config, "Quality Check", true, userId);

        // Activate
        pipeline.Activate(userId);

        return pipeline;
    }

    private static PipelineExecution CreateExecution(Pipeline pipeline)
    {
        var executionId = PipelineExecutionId.FromSequence(1);
        var traceId = TraceId.New();
        var userId = UserId.Parse("U00000001");

        return pipeline.StartExecution(executionId, traceId, userId).Value;
    }

    #endregion
}
