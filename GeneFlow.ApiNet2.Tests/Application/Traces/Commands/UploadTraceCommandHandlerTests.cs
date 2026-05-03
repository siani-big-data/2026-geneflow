using GeneFlow.ApiNet2.Application.Traces.Commands.UploadTrace;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for UploadTraceCommandHandler.
/// </summary>
public class UploadTraceCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly IJobPublisher _jobPublisher = Substitute.For<IJobPublisher>();
    private readonly UploadTraceCommandHandler _handler;

    public UploadTraceCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new UploadTraceCommandHandler(_unitOfWork, _jobPublisher);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateTrace()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sample_001.ab1");
        result.Value.Description.Should().Be("Test trace for BRCA1 analysis");
        result.Value.FileName.Should().Be("sample.ab1");
        result.Value.Status.Should().Be("Uploaded");
    }

    [Fact]
    public async Task Handle_ShouldPersistTrace()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _traceRepository.Received(1).AddAsync(
            Arg.Is<Trace>(t => t.Name.Value == "Sample_001.ab1"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAB1Format_ShouldSetHasChromatogramData()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Format.Should().Be("AB1");
        result.Value.HasChromatogramData.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFASTQFormat_ShouldNotSetHasChromatogramData()
    {
        // Arrange
        var command = new UploadTraceCommand(
            Guid.NewGuid().ToString(),
            "U00000001",
            "S00000001",
            "Sample_001.fastq",
            "Test trace",
            "sample.fastq",
            "text/plain",
            "traces/test-id/original.fastq",
            1024,
            "abc123def456");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Format.Should().Be("FASTQ");
        result.Value.HasChromatogramData.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithoutDescription_ShouldCreateTrace()
    {
        // Arrange
        var command = new UploadTraceCommand(
            Guid.NewGuid().ToString(),
            "U00000001",
            "S00000001",
            "Sample_001.ab1",
            null,
            "sample.ab1",
            "application/octet-stream",
            "traces/test-id/original.ab1",
            1024,
            "abc123def456");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("sample.ab1", "AB1")]
    [InlineData("sample.AB1", "AB1")]
    [InlineData("sample.scf", "SCF")]
    [InlineData("sample.fastq", "FASTQ")]
    [InlineData("sample.fq", "FASTQ")]
    [InlineData("sample.fasta", "FASTA")]
    [InlineData("sample.fa", "FASTA")]
    [InlineData("sample.gb", "GenBank")]
    public async Task Handle_WithVariousFormats_ShouldDetectFormat(string fileName, string expectedFormat)
    {
        // Arrange
        var command = new UploadTraceCommand(
            Guid.NewGuid().ToString(),
            "U00000001",
            "S00000001",
            $"Sample_{fileName}",
            "Test",
            fileName,
            "application/octet-stream",
            $"traces/test-id/original{Path.GetExtension(fileName)}",
            1024,
            "abc123def456");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Format.Should().Be(expectedFormat);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { UserId = "invalid-user-id" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { StudyId = "invalid-study-id" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStudyId");
    }

    [Fact]
    public async Task Handle_WithEmptyName_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = "" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameRequired");
    }

    [Fact]
    public async Task Handle_WithNameTooShort_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = "AB" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameTooShort");
    }

    [Fact]
    public async Task Handle_WithEmptyFileName_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { FileName = "" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FileNameRequired");
    }

    [Fact]
    public async Task Handle_WithInvalidFileSize_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { SizeBytes = 0 };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidFileSize");
    }

    [Fact]
    public async Task Handle_WithFileTooLarge_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { SizeBytes = 100 * 1024 * 1024 }; // 100 MB

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FileTooLarge");
    }

    [Fact]
    public async Task Handle_WithUnsupportedFormat_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { FileName = "sample.pdf" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("UnsupportedFormat");
    }

    [Fact]
    public async Task Handle_WithEmptyChecksum_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Checksum = "" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ChecksumRequired");
    }

    [Fact]
    public async Task Handle_WithEmptyStoragePath_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { StoragePath = "" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StoragePathRequired");
    }

    #endregion

    #region Helper Methods

    private static UploadTraceCommand CreateValidCommand()
    {
        return new UploadTraceCommand(
            TraceId: Guid.NewGuid().ToString(),
            UserId: "U00000001",
            StudyId: "S00000001",
            Name: "Sample_001.ab1",
            Description: "Test trace for BRCA1 analysis",
            FileName: "sample.ab1",
            ContentType: "application/octet-stream",
            StoragePath: "traces/test-trace-id/original.ab1",
            SizeBytes: 1024,
            Checksum: "abc123def456");
    }

    #endregion
}
