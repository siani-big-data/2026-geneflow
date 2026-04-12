using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.ValueObjects;

/// <summary>
/// Unit tests for the StudySettings value object.
/// </summary>
public class StudySettingsTests
{
    #region Create

    [Fact]
    public void Create_WithAllParameters_ShouldReturnCorrectSettings()
    {
        // Act
        var settings = StudySettings.Create(
            allowPublicComments: true,
            allowDataDownload: true,
            requireApprovalToJoin: false);

        // Assert
        settings.AllowPublicComments.Should().BeTrue();
        settings.AllowDataDownload.Should().BeTrue();
        settings.RequireApprovalToJoin.Should().BeFalse();
    }

    [Fact]
    public void Create_WithDifferentValues_ShouldReturnCorrectSettings()
    {
        // Act
        var settings = StudySettings.Create(
            allowPublicComments: false,
            allowDataDownload: false,
            requireApprovalToJoin: true);

        // Assert
        settings.AllowPublicComments.Should().BeFalse();
        settings.AllowDataDownload.Should().BeFalse();
        settings.RequireApprovalToJoin.Should().BeTrue();
    }

    #endregion

    #region Default

    [Fact]
    public void Default_ShouldHaveCorrectDefaultValues()
    {
        // Act
        var settings = StudySettings.Default;

        // Assert
        settings.AllowPublicComments.Should().BeTrue();
        settings.AllowDataDownload.Should().BeFalse();
        settings.RequireApprovalToJoin.Should().BeTrue();
    }

    #endregion

    #region With Methods

    [Fact]
    public void WithAllowPublicComments_ShouldReturnNewSettingsWithUpdatedValue()
    {
        // Arrange
        var original = StudySettings.Default;

        // Act
        var updated = original.WithAllowPublicComments(false);

        // Assert
        original.AllowPublicComments.Should().BeTrue();
        updated.AllowPublicComments.Should().BeFalse();
        updated.AllowDataDownload.Should().Be(original.AllowDataDownload);
        updated.RequireApprovalToJoin.Should().Be(original.RequireApprovalToJoin);
    }

    [Fact]
    public void WithAllowDataDownload_ShouldReturnNewSettingsWithUpdatedValue()
    {
        // Arrange
        var original = StudySettings.Default;

        // Act
        var updated = original.WithAllowDataDownload(true);

        // Assert
        original.AllowDataDownload.Should().BeFalse();
        updated.AllowDataDownload.Should().BeTrue();
        updated.AllowPublicComments.Should().Be(original.AllowPublicComments);
        updated.RequireApprovalToJoin.Should().Be(original.RequireApprovalToJoin);
    }

    [Fact]
    public void WithRequireApprovalToJoin_ShouldReturnNewSettingsWithUpdatedValue()
    {
        // Arrange
        var original = StudySettings.Default;

        // Act
        var updated = original.WithRequireApprovalToJoin(false);

        // Assert
        original.RequireApprovalToJoin.Should().BeTrue();
        updated.RequireApprovalToJoin.Should().BeFalse();
        updated.AllowPublicComments.Should().Be(original.AllowPublicComments);
        updated.AllowDataDownload.Should().Be(original.AllowDataDownload);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var settings1 = StudySettings.Create(true, false, true);
        var settings2 = StudySettings.Create(true, false, true);

        // Assert
        settings1.Should().Be(settings2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var settings1 = StudySettings.Create(true, false, true);
        var settings2 = StudySettings.Create(false, false, true);

        // Assert
        settings1.Should().NotBe(settings2);
    }

    [Fact]
    public void Equals_DefaultSettings_ShouldBeEqual()
    {
        // Arrange
        var settings1 = StudySettings.Default;
        var settings2 = StudySettings.Default;

        // Assert
        settings1.Should().Be(settings2);
    }

    #endregion
}
