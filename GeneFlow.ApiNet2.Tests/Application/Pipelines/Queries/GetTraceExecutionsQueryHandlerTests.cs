using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetTraceExecutions;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Tests.Application.Pipelines.Queries;

/// <summary>
/// Unit tests for GetTraceExecutionsQueryHandler.
/// </summary>
public class GetTraceExecutionsQueryHandlerTests
{
    private readonly IPipelineRepository _pipelineRepository = Substitute.For<IPipelineRepository>();
    private readonly IPipelineExecutionRepository _executionRepository = Substitute.For<IPipelineExecutionRepository>();
    private readonly GetTraceExecutionsQueryHandler _handler;

    public GetTraceExecutionsQueryHandlerTests()
    {
        _handler = new GetTraceExecutionsQueryHandler(_pipelineRepository, _executionRepository);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidTraceId_ShouldReturnPagedList()
    {
        // Arrange
        var traceId = TraceId.New();
        var pipeline = CreateActivePipelineWithSteps();
        var executions = new List<PipelineExecution>
        {
            CreateExecutionForTrace(pipeline, traceId, PipelineExecutionId.FromSequence(1)),
            CreateExecutionForTrace(pipeline, traceId, PipelineExecutionId.FromSequence(2))
        };
        var pagedList = PagedList<PipelineExecution>.Create(executions, 1, 20, 2);
        var query = new GetTraceExecutionsQuery("U00000001", pipeline.StudyId.ToString(), traceId.ToString());

        _executionRepository.GetByTraceAsync(
            Arg.Any<TraceId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<ExecutionStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);
        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldIncludePipelineNameInEachExecution()
    {
        // Arrange
        var traceId = TraceId.New();
        var pipeline = CreateActivePipelineWithSteps();
        var executions = new List<PipelineExecution>
        {
            CreateExecutionForTrace(pipeline, traceId, PipelineExecutionId.FromSequence(1))
        };
        var pagedList = PagedList<PipelineExecution>.Create(executions, 1, 20, 1);
        var query = new GetTraceExecutionsQuery("U00000001", pipeline.StudyId.ToString(), traceId.ToString());

        _executionRepository.GetByTraceAsync(
            Arg.Any<TraceId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<ExecutionStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);
        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.First().PipelineName.Should().Be(pipeline.Name.Value);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassStatusToRepository()
    {
        // Arrange
        var traceId = TraceId.New();
        var pipeline = CreateActivePipelineWithSteps();
        var executions = new List<PipelineExecution>
        {
            CreateCompletedExecutionForTrace(pipeline, traceId)
        };
        var pagedList = PagedList<PipelineExecution>.Create(executions, 1, 20, 1);
        var query = new GetTraceExecutionsQuery(
            "U00000001",
            pipeline.StudyId.ToString(),
            traceId.ToString(),
            StatusId: ExecutionStatus.Completed.Id);

        _executionRepository.GetByTraceAsync(
            Arg.Any<TraceId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<ExecutionStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);
        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _executionRepository.Received(1).GetByTraceAsync(
            Arg.Any<TraceId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<ExecutionStatus?>(s => s == ExecutionStatus.Completed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPipelineNotFound_ShouldReturnUnknownPipelineName()
    {
        // Arrange
        var traceId = TraceId.New();
        var pipeline = CreateActivePipelineWithSteps();
        var executions = new List<PipelineExecution>
        {
            CreateExecutionForTrace(pipeline, traceId, PipelineExecutionId.FromSequence(1))
        };
        var pagedList = PagedList<PipelineExecution>.Create(executions, 1, 20, 1);
        var query = new GetTraceExecutionsQuery("U00000001", pipeline.StudyId.ToString(), traceId.ToString());

        _executionRepository.GetByTraceAsync(
            Arg.Any<TraceId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<ExecutionStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);
        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns((Pipeline?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.First().PipelineName.Should().Be("Unknown");
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyPagedList()
    {
        // Arrange
        var traceId = TraceId.New();
        var pagedList = PagedList<PipelineExecution>.Create(new List<PipelineExecution>(), 1, 20, 0);
        var query = new GetTraceExecutionsQuery("U00000001", "S00000001", traceId.ToString());

        _executionRepository.GetByTraceAsync(
            Arg.Any<TraceId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<ExecutionStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldReturnError()
    {
        // Arrange
        var query = new GetTraceExecutionsQuery("U00000001", "S00000001", "invalid-trace-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TraceNotProcessed");
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

    private static PipelineExecution CreateExecutionForTrace(Pipeline pipeline, TraceId traceId, PipelineExecutionId executionId)
    {
        var userId = UserId.Parse("U00000001");
        return pipeline.StartExecution(executionId, traceId, userId).Value;
    }

    private static PipelineExecution CreateCompletedExecutionForTrace(Pipeline pipeline, TraceId traceId)
    {
        var execution = CreateExecutionForTrace(pipeline, traceId, PipelineExecutionId.FromSequence(1));
        execution.Start();
        execution.Complete();
        return execution;
    }

    #endregion
}
