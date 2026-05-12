using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetStudyPipelines;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Tests.Application.Pipelines.Queries;

/// <summary>
/// Unit tests for GetStudyPipelinesQueryHandler.
/// </summary>
public class GetStudyPipelinesQueryHandlerTests
{
    private readonly IPipelineRepository _pipelineRepository = Substitute.For<IPipelineRepository>();
    private readonly GetStudyPipelinesQueryHandler _handler;

    public GetStudyPipelinesQueryHandlerTests()
    {
        _handler = new GetStudyPipelinesQueryHandler(_pipelineRepository);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidStudyId_ShouldReturnPagedList()
    {
        // Arrange
        var studyId = StudyId.FromSequence(1);
        var pipelines = new List<Pipeline>
        {
            CreatePipeline(PipelineId.FromSequence(1), "Pipeline 1"),
            CreatePipeline(PipelineId.FromSequence(2), "Pipeline 2")
        };
        var pagedList = PagedList<Pipeline>.Create(pipelines, 1, 20, 2);
        var query = new GetStudyPipelinesQuery("U00000001", studyId.ToString());

        _pipelineRepository.GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<PipelineStatus?>(),
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
        var studyId = StudyId.FromSequence(1);
        var pipelines = new List<Pipeline> { CreateActivePipeline(PipelineId.FromSequence(1), "Active Pipeline") };
        var pagedList = PagedList<Pipeline>.Create(pipelines, 1, 20, 1);
        var query = new GetStudyPipelinesQuery("U00000001", studyId.ToString(), StatusId: PipelineStatus.Active.Id);

        _pipelineRepository.GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<PipelineStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _pipelineRepository.Received(1).GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Is<PipelineStatus?>(s => s == PipelineStatus.Active),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldPassSearchTermToRepository()
    {
        // Arrange
        var studyId = StudyId.FromSequence(1);
        var pipelines = new List<Pipeline> { CreatePipeline(PipelineId.FromSequence(1), "Quality Pipeline") };
        var pagedList = PagedList<Pipeline>.Create(pipelines, 1, 20, 1);
        var searchTerm = "Quality";
        var query = new GetStudyPipelinesQuery("U00000001", studyId.ToString(), SearchTerm: searchTerm);

        _pipelineRepository.GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<PipelineStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _pipelineRepository.Received(1).GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<string?>(s => s == searchTerm),
            Arg.Any<PipelineStatus?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var studyId = StudyId.FromSequence(1);
        var pipelines = new List<Pipeline> { CreatePipeline(PipelineId.FromSequence(3), "Pipeline 3") };
        var pagedList = PagedList<Pipeline>.Create(pipelines, 2, 2, 5);
        var query = new GetStudyPipelinesQuery("U00000001", studyId.ToString(), PageNumber: 2, PageSize: 2);

        _pipelineRepository.GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<PipelineStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalCount.Should().Be(5);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyPagedList()
    {
        // Arrange
        var studyId = StudyId.FromSequence(1);
        var pagedList = PagedList<Pipeline>.Create(new List<Pipeline>(), 1, 20, 0);
        var query = new GetStudyPipelinesQuery("U00000001", studyId.ToString());

        _pipelineRepository.GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<PipelineStatus?>(),
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
    public async Task Handle_WithInvalidStudyId_ShouldReturnError()
    {
        // Arrange
        var query = new GetStudyPipelinesQuery("U00000001", "invalid-study-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStudyId");
    }

    #endregion

    #region Helper Methods

    private static Pipeline CreatePipeline(PipelineId id, string name)
    {
        var studyId = StudyId.FromSequence(1);
        var userId = UserId.Parse("U00000001");
        var pipelineName = PipelineName.Create(name).Value;
        var description = PipelineDescription.Create("Test description").Value;

        return Pipeline.Create(id, studyId, userId, pipelineName, description).Value;
    }

    private static Pipeline CreateActivePipeline(PipelineId id, string name)
    {
        var pipeline = CreatePipeline(id, name);
        var userId = UserId.Parse("U00000001");

        // Add a step and activate
        var config = StepConfiguration.Create("{}", StepType.Quality).Value;
        pipeline.AddStep(StepType.Quality, config, "Quality Check", true, userId);
        pipeline.Activate(userId);

        return pipeline;
    }

    #endregion
}
