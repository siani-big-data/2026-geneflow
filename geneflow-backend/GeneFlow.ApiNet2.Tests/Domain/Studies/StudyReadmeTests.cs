using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies;

/// <summary>
/// Unit tests for Study.UpdateReadme.
/// </summary>
public class StudyReadmeTests
{
    private static Study CreateStudy(UserId? ownerId = null)
    {
        var id = new StudyId(1);
        var owner = ownerId ?? new UserId(1);
        var title = StudyTitle.Create("README Study").Value;
        var description = StudyDescription.Create("desc").Value;
        return Study.Create(id, owner, title, description, ResearchField.Genomics).Value;
    }

    [Fact]
    public void UpdateReadme_WithValidMarkdown_ShouldSetValueAndRaiseEvent()
    {
        // Arrange
        var owner = new UserId(1);
        var study = CreateStudy(owner);
        study.ClearDomainEvents();
        const string markdown = "# Hello\n\nSome content.";

        // Act
        var result = study.UpdateReadme(markdown, owner);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.ReadmeMarkdown.Should().Be(markdown);
        study.DomainEvents.Should().ContainSingle(e => e is StudyReadmeUpdatedEvent);
    }

    [Fact]
    public void UpdateReadme_WithEmptyOrWhitespace_ShouldNormalizeToNull()
    {
        // Arrange
        var owner = new UserId(1);
        var study = CreateStudy(owner);

        // Act
        var result = study.UpdateReadme("   \n  ", owner);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.ReadmeMarkdown.Should().BeNull();
    }

    [Fact]
    public void UpdateReadme_FromMemberWithoutEditRights_ShouldFail()
    {
        // Arrange
        var owner = new UserId(1);
        var viewer = new UserId(2);
        var study = CreateStudy(owner);
        study.AddMember(viewer, StudyRole.Viewer, owner);

        // Act
        var result = study.UpdateReadme("# hi", viewer);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StudyErrors.InsufficientPermissions);
        study.ReadmeMarkdown.Should().BeNull();
    }

    [Fact]
    public void UpdateReadme_FromNonMember_ShouldFail()
    {
        // Arrange
        var owner = new UserId(1);
        var stranger = new UserId(99);
        var study = CreateStudy(owner);

        // Act
        var result = study.UpdateReadme("# hi", stranger);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StudyErrors.InsufficientPermissions);
    }

    [Fact]
    public void UpdateReadme_ExceedingMaxLength_ShouldFail()
    {
        // Arrange
        var owner = new UserId(1);
        var study = CreateStudy(owner);
        var tooLong = new string('a', Study.MaxReadmeLength + 1);

        // Act
        var result = study.UpdateReadme(tooLong, owner);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Study.ReadmeTooLong");
        study.ReadmeMarkdown.Should().BeNull();
    }

    [Fact]
    public void UpdateReadme_AtExactlyMaxLength_ShouldSucceed()
    {
        // Arrange
        var owner = new UserId(1);
        var study = CreateStudy(owner);
        var exact = new string('b', Study.MaxReadmeLength);

        // Act
        var result = study.UpdateReadme(exact, owner);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.ReadmeMarkdown.Should().Be(exact);
    }

    [Fact]
    public void UpdateReadme_FromAdminMember_ShouldSucceed()
    {
        // Arrange
        var owner = new UserId(1);
        var admin = new UserId(2);
        var study = CreateStudy(owner);
        study.AddMember(admin, StudyRole.Admin, owner);

        // Act
        var result = study.UpdateReadme("# admin wrote this", admin);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.ReadmeMarkdown.Should().Be("# admin wrote this");
    }
}
