using GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceCountsByStatus;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Queries;

/// <summary>
/// Unit tests for GetTraceCountsByStatusQueryHandler.
/// </summary>
public class GetTraceCountsByStatusQueryHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly GetTraceCountsByStatusQueryHandler _handler;

    public GetTraceCountsByStatusQueryHandlerTests()
    {
        _handler = new GetTraceCountsByStatusQueryHandler(_traceRepository);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidStudyId_ShouldReturnCounts()
    {
        // Arrange
        var studyId = StudyId.FromSequence(1);
        var query = new GetTraceCountsByStatusQuery("U00000001", studyId.ToString());
        var counts = new Dictionary<TraceStatus, int>
        {
            { TraceStatus.Uploaded, 5 },
            { TraceStatus.Validating, 2 },
            { TraceStatus.Processing, 3 },
            { TraceStatus.Processed, 10 },
            { TraceStatus.Failed, 1 },
            { TraceStatus.Archived, 4 }
        };

        _traceRepository.GetCountsByStatusAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(counts);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Uploaded.Should().Be(5);
        result.Value.Validating.Should().Be(2);
        result.Value.Processing.Should().Be(3);
        result.Value.Processed.Should().Be(10);
        result.Value.Failed.Should().Be(1);
        result.Value.Archived.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotal()
    {
        // Arrange
        var studyId = StudyId.FromSequence(1);
        var query = new GetTraceCountsByStatusQuery("U00000001", studyId.ToString());
        var counts = new Dictionary<TraceStatus, int>
        {
            { TraceStatus.Uploaded, 5 },
            { TraceStatus.Processed, 10 },
            { TraceStatus.Failed, 2 }
        };

        _traceRepository.GetCountsByStatusAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(counts);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(17); // 5 + 10 + 2
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var query = new GetTraceCountsByStatusQuery("invalid-user-id", "S00000001");

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
        var query = new GetTraceCountsByStatusQuery("U00000001", "invalid-study-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStudyId");
    }

    #endregion
}
