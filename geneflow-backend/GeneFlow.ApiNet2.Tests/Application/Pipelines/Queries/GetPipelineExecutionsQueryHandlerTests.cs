using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetPipelineExecutions;
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
/// Unit tests for GetPipelineExecutionsQueryHandler.
/// </summary>
public class GetPipelineExecutionsQueryHandlerTests
{
    private readonly IPipelineRepository _pipelineRepository = Substitute.For<IPipelineRepository>();
    private readonly IPipelineExecutionRepository _executionRepository = Substitute.For<IPipelineExecutionRepository>();
    private readonly GetPipelineExecutionsQueryHandler _handler;

    public GetPipelineExecutionsQueryHandlerTests()
    {
        _handler = new GetPipelineExecutionsQueryHandler(_pipelineRepository, _executionRepository);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidPipelineId_ShouldReturnPagedList()
    {
        // Arrange
        var pipeline = CreateActivePipelineWithSteps();
        var executions = new List<PipelineExecution>
        {
            CreateExecution(pipeline, PipelineExecutionId.FromSequence(1)),
            CreateExecution(pipeline, PipelineExecutionId.FromSequence(2))
        };
        var pagedList = PagedList<PipelineExecution>.Create(executions, 1, 20, 2);
        var query = new GetPipelineExecutionsQuery("U00000001", pipeline.StudyId.ToString(), pipeline.Id.ToString());

        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);
        _executionRepository.GetByPipelineAsync(
            Arg.Any<PipelineId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<ExecutionStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassStatusToRepository()
    {
        // Arrange
        var pipeline = CreateActivePipelineWithSteps();
        var executions = new List<PipelineExecution> { CreateRunningExecution(pipeline) };
        var pagedList = PagedList<PipelineExecution>.Create(executions, 1, 20, 1);
        var query = new GetPipelineExecutionsQuery(
            "U00000001",
            pipeline.StudyId.ToString(),
            pipeline.Id.ToString(),
            StatusId: ExecutionStatus.Running.Id);

        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);
        _executionRepository.GetByPipelineAsync(
            Arg.Any<PipelineId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<ExecutionStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _executionRepository.Received(1).GetByPipelineAsync(
            Arg.Any<PipelineId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<ExecutionStatus?>(s => s == ExecutionStatus.Running),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var pipeline = CreateActivePipelineWithSteps();
        var executions = new List<PipelineExecution> { CreateExecution(pipeline, PipelineExecutionId.FromSequence(3)) };
        var pagedList = PagedList<PipelineExecution>.Create(executions, 2, 2, 5);
        var query = new GetPipelineExecutionsQuery(
            "U00000001",
            pipeline.StudyId.ToString(),
            pipeline.Id.ToString(),
            PageNumber: 2,
            PageSize: 2);

        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);
        _executionRepository.GetByPipelineAsync(
            Arg.Any<PipelineId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<ExecutionStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ShouldIncludePipelineNameInDto()
    {
        // Arrange
        var pipeline = CreateActivePipelineWithSteps();
        var executions = new List<PipelineExecution> { CreateExecution(pipeline, PipelineExecutionId.FromSequence(1)) };
        var pagedList = PagedList<PipelineExecution>.Create(executions, 1, 20, 1);
        var query = new GetPipelineExecutionsQuery("U00000001", pipeline.StudyId.ToString(), pipeline.Id.ToString());

        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);
        _executionRepository.GetByPipelineAsync(
            Arg.Any<PipelineId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<ExecutionStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.First().PipelineName.Should().Be(pipeline.Name.Value);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyPagedList()
    {
        // Arrange
        var pipeline = CreateActivePipelineWithSteps();
        var pagedList = PagedList<PipelineExecution>.Create(new List<PipelineExecution>(), 1, 20, 0);
        var query = new GetPipelineExecutionsQuery("U00000001", pipeline.StudyId.ToString(), pipeline.Id.ToString());

        _pipelineRepository.GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);
        _executionRepository.GetByPipelineAsync(
            Arg.Any<PipelineId>(),
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
    public async Task Handle_WithInvalidPipelineId_ShouldReturnNotFoundError()
    {
        // Arrange
        var query = new GetPipelineExecutionsQuery("U00000001", "S00000001", "invalid-pipeline-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
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

    private static PipelineExecution CreateExecution(Pipeline pipeline, PipelineExecutionId executionId)
    {
        var traceId = TraceId.New();
        var userId = UserId.Parse("U00000001");

        return pipeline.StartExecution(executionId, traceId, userId).Value;
    }

    private static PipelineExecution CreateRunningExecution(Pipeline pipeline)
    {
        var execution = CreateExecution(pipeline, PipelineExecutionId.FromSequence(1));
        execution.Start();
        return execution;
    }

    #endregion
}
