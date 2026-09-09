using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.ValueObjects;

/// <summary>
/// Unit tests for TraceFile value object.
/// </summary>
public class TraceFileTests
{
    #region Create - Success Cases

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var fileName = "sample_001.ab1";
        var contentType = "application/octet-stream";
        var storagePath = "traces/2024/01/sample_001.ab1";
        var sizeBytes = 1024L;
        var checksum = "abc123def456";

        // Act
        var result = TraceFile.Create(fileName, contentType, storagePath, sizeBytes, checksum);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be(fileName);
        result.Value.ContentType.Should().Be(contentType);
        result.Value.StoragePath.Should().Be(storagePath);
        result.Value.SizeBytes.Should().Be(sizeBytes);
        result.Value.Checksum.Should().Be(checksum);
    }

    [Fact]
    public void Create_WithMaxFileSize_ShouldSucceed()
    {
        // Arrange
        var maxSize = TraceFile.MaxFileSizeBytes;

        // Act
        var result = TraceFile.Create("file.ab1", "application/octet-stream", "/path", maxSize, "checksum");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SizeBytes.Should().Be(maxSize);
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValues()
    {
        // Arrange
        var fileName = "  sample.ab1  ";
        var contentType = "  application/octet-stream  ";
        var storagePath = "  /path/to/file  ";
        var checksum = "  abc123  ";

        // Act
        var result = TraceFile.Create(fileName, contentType, storagePath, 1024, checksum);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("sample.ab1");
        result.Value.ContentType.Should().Be("application/octet-stream");
        result.Value.StoragePath.Should().Be("/path/to/file");
        result.Value.Checksum.Should().Be("abc123");
    }

    #endregion

    #region Create - FileName Failures

    [Fact]
    public void Create_WithNullFileName_ShouldFail()
    {
        // Act
        var result = TraceFile.Create(null!, "type", "/path", 1024, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FileNameRequired");
    }

    [Fact]
    public void Create_WithEmptyFileName_ShouldFail()
    {
        // Act
        var result = TraceFile.Create(string.Empty, "type", "/path", 1024, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FileNameRequired");
    }

    [Fact]
    public void Create_WithFileNameTooLong_ShouldFail()
    {
        // Arrange
        var longFileName = new string('a', TraceFile.MaxFileNameLength + 1);

        // Act
        var result = TraceFile.Create(longFileName, "type", "/path", 1024, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FileNameTooLong");
    }

    #endregion

    #region Create - ContentType Failures

    [Fact]
    public void Create_WithNullContentType_ShouldFail()
    {
        // Act
        var result = TraceFile.Create("file.ab1", null!, "/path", 1024, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ContentTypeRequired");
    }

    [Fact]
    public void Create_WithEmptyContentType_ShouldFail()
    {
        // Act
        var result = TraceFile.Create("file.ab1", string.Empty, "/path", 1024, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ContentTypeRequired");
    }

    [Fact]
    public void Create_WithContentTypeTooLong_ShouldFail()
    {
        // Arrange
        var longContentType = new string('a', TraceFile.MaxContentTypeLength + 1);

        // Act
        var result = TraceFile.Create("file.ab1", longContentType, "/path", 1024, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ContentTypeTooLong");
    }

    #endregion

    #region Create - StoragePath Failures

    [Fact]
    public void Create_WithNullStoragePath_ShouldFail()
    {
        // Act
        var result = TraceFile.Create("file.ab1", "type", null!, 1024, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StoragePathRequired");
    }

    [Fact]
    public void Create_WithEmptyStoragePath_ShouldFail()
    {
        // Act
        var result = TraceFile.Create("file.ab1", "type", string.Empty, 1024, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StoragePathRequired");
    }

    [Fact]
    public void Create_WithStoragePathTooLong_ShouldFail()
    {
        // Arrange
        var longPath = new string('a', TraceFile.MaxStoragePathLength + 1);

        // Act
        var result = TraceFile.Create("file.ab1", "type", longPath, 1024, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StoragePathTooLong");
    }

    #endregion

    #region Create - Size Failures

    [Fact]
    public void Create_WithZeroSize_ShouldFail()
    {
        // Act
        var result = TraceFile.Create("file.ab1", "type", "/path", 0, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidFileSize");
    }

    [Fact]
    public void Create_WithNegativeSize_ShouldFail()
    {
        // Act
        var result = TraceFile.Create("file.ab1", "type", "/path", -1, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidFileSize");
    }

    [Fact]
    public void Create_WithSizeExceedingMax_ShouldFail()
    {
        // Act
        var result = TraceFile.Create("file.ab1", "type", "/path", TraceFile.MaxFileSizeBytes + 1, "checksum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FileTooLarge");
    }

    #endregion

    #region Create - Checksum Failures

    [Fact]
    public void Create_WithNullChecksum_ShouldFail()
    {
        // Act
        var result = TraceFile.Create("file.ab1", "type", "/path", 1024, null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ChecksumRequired");
    }

    [Fact]
    public void Create_WithEmptyChecksum_ShouldFail()
    {
        // Act
        var result = TraceFile.Create("file.ab1", "type", "/path", 1024, string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ChecksumRequired");
    }

    [Fact]
    public void Create_WithChecksumTooLong_ShouldFail()
    {
        // Arrange
        var longChecksum = new string('a', TraceFile.MaxChecksumLength + 1);

        // Act
        var result = TraceFile.Create("file.ab1", "type", "/path", 1024, longChecksum);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ChecksumTooLong");
    }

    #endregion

    #region GetExtension

    [Theory]
    [InlineData("sample.ab1", ".ab1")]
    [InlineData("sample.AB1", ".ab1")]
    [InlineData("sample.fastq", ".fastq")]
    [InlineData("sample.FASTA", ".fasta")]
    [InlineData("sample.tar.gz", ".gz")]
    [InlineData("noextension", "")]
    public void GetExtension_ShouldReturnCorrectExtension(string fileName, string expectedExtension)
    {
        // Arrange
        var traceFile = TraceFile.Create(fileName, "type", "/path", 1024, "checksum").Value;

        // Act
        var extension = traceFile.GetExtension();

        // Assert
        extension.Should().Be(expectedExtension);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var file1 = TraceFile.Create("file.ab1", "type", "/path", 1024, "checksum").Value;
        var file2 = TraceFile.Create("file.ab1", "type", "/path", 1024, "checksum").Value;

        // Act & Assert
        file1.Should().Be(file2);
    }

    [Fact]
    public void Equals_WithDifferentFileName_ShouldNotBeEqual()
    {
        // Arrange
        var file1 = TraceFile.Create("file1.ab1", "type", "/path", 1024, "checksum").Value;
        var file2 = TraceFile.Create("file2.ab1", "type", "/path", 1024, "checksum").Value;

        // Act & Assert
        file1.Should().NotBe(file2);
    }

    [Fact]
    public void Equals_WithDifferentSize_ShouldNotBeEqual()
    {
        // Arrange
        var file1 = TraceFile.Create("file.ab1", "type", "/path", 1024, "checksum").Value;
        var file2 = TraceFile.Create("file.ab1", "type", "/path", 2048, "checksum").Value;

        // Act & Assert
        file1.Should().NotBe(file2);
    }

    #endregion

    #region Constants

    [Fact]
    public void MaxFileSizeBytes_ShouldBeTenMegabytes()
    {
        TraceFile.MaxFileSizeBytes.Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void MaxFileNameLength_ShouldBe255()
    {
        TraceFile.MaxFileNameLength.Should().Be(255);
    }

    [Fact]
    public void MaxContentTypeLength_ShouldBe100()
    {
        TraceFile.MaxContentTypeLength.Should().Be(100);
    }

    [Fact]
    public void MaxStoragePathLength_ShouldBe500()
    {
        TraceFile.MaxStoragePathLength.Should().Be(500);
    }

    [Fact]
    public void MaxChecksumLength_ShouldBe64()
    {
        TraceFile.MaxChecksumLength.Should().Be(64);
    }

    #endregion
}
