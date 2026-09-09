using GeneFlow.ApiNet2.Application.Traces.Queries.GetStudyTraces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Queries;

/// <summary>
/// Unit tests for GetStudyTracesQueryHandler.
/// </summary>
public class GetStudyTracesQueryHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly GetStudyTracesQueryHandler _handler;

    public GetStudyTracesQueryHandlerTests()
    {
        _handler = new GetStudyTracesQueryHandler(_traceRepository);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidStudyId_ShouldReturnPagedTraces()
    {
        // Arrange
        var studyId = StudyId.FromSequence(1);
        var traces = new List<Trace>
        {
            CreateUploadedTrace(studyId),
            CreateUploadedTrace(studyId),
            CreateUploadedTrace(studyId)
        };
        var pagedTraces = PagedList<Trace>.Create(traces, 1, 20, 3);
        var query = new GetStudyTracesQuery("U00000001", studyId.ToString());

        _traceRepository.GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<TraceStatus?>(),
            Arg.Any<TraceFormat?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedTraces);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var studyId = StudyId.FromSequence(1);
        var traces = new List<Trace> { CreateUploadedTrace(studyId) };
        var pagedTraces = PagedList<Trace>.Create(traces, 2, 10, 15);
        var query = new GetStudyTracesQuery("U00000001", studyId.ToString(), PageNumber: 2, PageSize: 10);

        _traceRepository.GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Is<int>(p => p == 2),
            Arg.Is<int>(s => s == 10),
            Arg.Any<string?>(),
            Arg.Any<TraceStatus?>(),
            Arg.Any<TraceFormat?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedTraces);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalCount.Should().Be(15);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldCallRepositoryWithFilter()
    {
        // Arrange
        var studyId = StudyId.FromSequence(1);
        var traces = new List<Trace>();
        var pagedTraces = PagedList<Trace>.Create(traces, 1, 20, 0);
        var query = new GetStudyTracesQuery("U00000001", studyId.ToString(), StatusId: TraceStatus.Processed.Id);

        _traceRepository.GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Is<TraceStatus?>(s => s == TraceStatus.Processed),
            Arg.Any<TraceFormat?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedTraces);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _traceRepository.Received(1).GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Is<TraceStatus?>(s => s == TraceStatus.Processed),
            Arg.Any<TraceFormat?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        var studyId = StudyId.FromSequence(1);
        var pagedTraces = PagedList<Trace>.Create(new List<Trace>(), 1, 20, 0);
        var query = new GetStudyTracesQuery("U00000001", studyId.ToString());

        _traceRepository.GetByStudyAsync(
            Arg.Any<StudyId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<TraceStatus?>(),
            Arg.Any<TraceFormat?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(pagedTraces);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var query = new GetStudyTracesQuery("invalid-user-id", "S00000001");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldFail()
    {
        // Arrange
        var query = new GetStudyTracesQuery("U00000001", "invalid-study-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStudyId");
    }

    #endregion

    #region Helper Methods

    private static Trace CreateUploadedTrace(StudyId? studyId = null)
    {
        var traceId = TraceId.New();
        var sId = studyId ?? StudyId.FromSequence(1);
        var userId = UserId.Parse("U00000001");
        var traceName = TraceName.Create("Sample_001.ab1").Value;
        var traceDescription = TraceDescription.Create("Test description").Value;
        var traceFile = TraceFile.Create("sample.ab1", "application/octet-stream", "/path", 1024, "checksum").Value;

        return Trace.Create(traceId, sId, userId, traceName, traceDescription, traceFile, TraceFormat.AB1).Value;
    }

    #endregion
}
