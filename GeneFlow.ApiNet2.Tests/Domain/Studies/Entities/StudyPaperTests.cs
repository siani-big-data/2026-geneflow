using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.Entities;

/// <summary>
/// Unit tests for the StudyPaper entity.
/// </summary>
public class StudyPaperTests
{
    private static StudyPaperId CreatePaperId(long value = 1) => new(value);
    private static UserId CreateUserId(long value = 1) => new(value);

    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var paperId = CreatePaperId();
        var uploadedBy = CreateUserId();
        const string title = "Analysis of BRCA1 Gene Mutations";

        // Act
        var result = StudyPaper.Create(
            paperId,
            title,
            authors: "Smith J., Doe A.",
            doi: "10.1234/example.2024",
            @abstract: "This paper analyzes...",
            journal: "Nature Genetics",
            publicationYear: 2024,
            fileId: "file123",
            fileName: "paper.pdf",
            fileSizeBytes: 1024000,
            uploadedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(paperId);
        result.Value.Title.Should().Be(title);
        result.Value.Authors.Should().Be("Smith J., Doe A.");
        result.Value.Doi.Should().Be("10.1234/example.2024");
        result.Value.Abstract.Should().Be("This paper analyzes...");
        result.Value.Journal.Should().Be("Nature Genetics");
        result.Value.PublicationYear.Should().Be(2024);
        result.Value.FileId.Should().Be("file123");
        result.Value.FileName.Should().Be("paper.pdf");
        result.Value.FileSizeBytes.Should().Be(1024000);
    }

    [Fact]
    public void Create_WithMinimalData_ShouldReturnSuccess()
    {
        // Arrange
        var paperId = CreatePaperId();
        var uploadedBy = CreateUserId();
        const string title = "Minimal Paper Title";

        // Act
        var result = StudyPaper.Create(
            paperId,
            title,
            authors: null,
            doi: null,
            @abstract: null,
            journal: null,
            publicationYear: null,
            fileId: null,
            fileName: null,
            fileSizeBytes: null,
            uploadedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be(title);
        result.Value.Authors.Should().BeNull();
        result.Value.Doi.Should().BeNull();
        result.Value.Abstract.Should().BeNull();
        result.Value.Journal.Should().BeNull();
        result.Value.PublicationYear.Should().BeNull();
        result.Value.FileId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimTitle()
    {
        // Arrange
        var paperId = CreatePaperId();
        var uploadedBy = CreateUserId();
        const string title = "  Paper Title With Spaces  ";

        // Act
        var result = StudyPaper.Create(
            paperId, title, null, null, null, null, null, null, null, null, uploadedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Paper Title With Spaces");
    }

    [Fact]
    public void Create_ShouldSetCreationAudit()
    {
        // Arrange
        var paperId = CreatePaperId();
        var uploadedBy = CreateUserId(42);
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = StudyPaper.Create(
            paperId, "Test Paper", null, null, null, null, null, null, null, null, uploadedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        result.Value.CreatedBy.Should().Be("42");
    }

    #endregion

    #region Create - Invalid Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_ShouldReturnFailure(string? title)
    {
        // Arrange
        var paperId = CreatePaperId();
        var uploadedBy = CreateUserId();

        // Act
        var result = StudyPaper.Create(
            paperId, title!, null, null, null, null, null, null, null, null, uploadedBy);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithTitleTooLong_ShouldReturnFailure()
    {
        // Arrange
        var paperId = CreatePaperId();
        var uploadedBy = CreateUserId();
        var longTitle = new string('a', StudyPaper.MaxTitleLength + 1);

        // Act
        var result = StudyPaper.Create(
            paperId, longTitle, null, null, null, null, null, null, null, null, uploadedBy);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region HasFile

    [Fact]
    public void HasFile_WithFileId_ShouldReturnTrue()
    {
        // Arrange
        var paper = StudyPaper.Create(
            CreatePaperId(),
            "Test Paper",
            null, null, null, null, null,
            fileId: "file123",
            fileName: "paper.pdf",
            fileSizeBytes: 1024,
            CreateUserId()).Value;

        // Assert
        paper.HasFile.Should().BeTrue();
    }

    [Fact]
    public void HasFile_WithoutFileId_ShouldReturnFalse()
    {
        // Arrange
        var paper = StudyPaper.Create(
            CreatePaperId(),
            "Test Paper",
            null, null, null, null, null,
            fileId: null,
            fileName: null,
            fileSizeBytes: null,
            CreateUserId()).Value;

        // Assert
        paper.HasFile.Should().BeFalse();
    }

    [Fact]
    public void HasFile_WithEmptyFileId_ShouldReturnFalse()
    {
        // Arrange
        var paper = StudyPaper.Create(
            CreatePaperId(),
            "Test Paper",
            null, null, null, null, null,
            fileId: "",
            fileName: null,
            fileSizeBytes: null,
            CreateUserId()).Value;

        // Assert
        paper.HasFile.Should().BeFalse();
    }

    #endregion

    #region SoftDelete

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        // Arrange
        var paper = StudyPaper.Create(
            CreatePaperId(),
            "Test Paper",
            null, null, null, null, null, null, null, null,
            CreateUserId()).Value;
        var deleteTime = DateTime.UtcNow;
        const string deletedBy = "user123";

        // Act
        paper.SoftDelete(deleteTime, deletedBy);

        // Assert
        paper.IsDeleted.Should().BeTrue();
        paper.DeletedAt.Should().Be(deleteTime);
        paper.DeletedBy.Should().Be(deletedBy);
    }

    [Fact]
    public void SoftDelete_AlreadyDeleted_ShouldNotUpdate()
    {
        // Arrange
        var paper = StudyPaper.Create(
            CreatePaperId(),
            "Test Paper",
            null, null, null, null, null, null, null, null,
            CreateUserId()).Value;
        var firstDeleteTime = DateTime.UtcNow;
        paper.SoftDelete(firstDeleteTime, "user1");

        var secondDeleteTime = DateTime.UtcNow.AddMinutes(5);

        // Act
        paper.SoftDelete(secondDeleteTime, "user2");

        // Assert
        paper.DeletedAt.Should().Be(firstDeleteTime);
        paper.DeletedBy.Should().Be("user1");
    }

    #endregion

    #region Restore

    [Fact]
    public void RestoREDACTED()
    {
        // Arrange
        var paper = StudyPaper.Create(
            CreatePaperId(),
            "Test Paper",
            null, null, null, null, null, null, null, null,
            CreateUserId()).Value;
        paper.SoftDelete(DateTime.UtcNow, "user1");

        // Act
        paper.Restore();

        // Assert
        paper.IsDeleted.Should().BeFalse();
        paper.DeletedAt.Should().BeNull();
        paper.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void RestoREDACTED()
    {
        // Arrange
        var paper = StudyPaper.Create(
            CreatePaperId(),
            "Test Paper",
            null, null, null, null, null, null, null, null,
            CreateUserId()).Value;

        // Act
        paper.Restore();

        // Assert
        paper.IsDeleted.Should().BeFalse();
        paper.DeletedAt.Should().BeNull();
    }

    #endregion

    #region Constants

    [Fact]
    public void MaxTitleLength_ShouldBe500()
    {
        // Assert
        StudyPaper.MaxTitleLength.Should().Be(500);
    }

    [Fact]
    public void MaxAuthorsLength_ShouldBe1000()
    {
        // Assert
        StudyPaper.MaxAuthorsLength.Should().Be(1000);
    }

    [Fact]
    public void MaxDoiLength_ShouldBe100()
    {
        // Assert
        StudyPaper.MaxDoiLength.Should().Be(100);
    }

    [Fact]
    public void MaxAbstractLength_ShouldBe5000()
    {
        // Assert
        StudyPaper.MaxAbstractLength.Should().Be(5000);
    }

    [Fact]
    public void MaxJournalLength_ShouldBe200()
    {
        // Assert
        StudyPaper.MaxJournalLength.Should().Be(200);
    }

    #endregion
}
