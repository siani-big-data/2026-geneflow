using GeneFlow.ApiNet2.Domain.Traces.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.Enumerations;

/// <summary>
/// Unit tests for TraceStatus enumeration.
/// </summary>
public class TraceStatusTests
{
    #region Status Values

    [Fact]
    public void AllStatuses_ShouldHaveUniqueIds()
    {
        // Arrange
        var statuses = TraceStatus.GetAll();

        // Act
        var ids = statuses.Select(s => s.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllStatuses_ShouldHaveUniqueNames()
    {
        // Arrange
        var statuses = TraceStatus.GetAll();

        // Act
        var names = statuses.Select(s => s.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Uploaded_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceStatus.Uploaded.Id.Should().Be(1);
        TraceStatus.Uploaded.Name.Should().Be("Uploaded");
        TraceStatus.Uploaded.DisplayName.Should().Be("Uploaded");
        TraceStatus.Uploaded.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public void Validating_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceStatus.Validating.Id.Should().Be(2);
        TraceStatus.Validating.Name.Should().Be("Validating");
        TraceStatus.Validating.IsProcessing.Should().BeTrue();
    }

    [Fact]
    public void Processing_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceStatus.Processing.Id.Should().Be(3);
        TraceStatus.Processing.Name.Should().Be("Processing");
        TraceStatus.Processing.IsProcessing.Should().BeTrue();
    }

    [Fact]
    public void Processed_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceStatus.Processed.Id.Should().Be(4);
        TraceStatus.Processed.Name.Should().Be("Processed");
        TraceStatus.Processed.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public void Failed_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceStatus.Failed.Id.Should().Be(5);
        TraceStatus.Failed.Name.Should().Be("Failed");
        TraceStatus.Failed.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public void Archived_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceStatus.Archived.Id.Should().Be(6);
        TraceStatus.Archived.Name.Should().Be("Archived");
        TraceStatus.Archived.IsProcessing.Should().BeFalse();
    }

    #endregion

    #region CanTransitionTo - From Uploaded

    [Fact]
    public void Uploaded_CanTransitionTo_Validating()
    {
        TraceStatus.Uploaded.CanTransitionTo(TraceStatus.Validating).Should().BeTrue();
    }

    [Fact]
    public void Uploaded_CanTransitionTo_Archived()
    {
        TraceStatus.Uploaded.CanTransitionTo(TraceStatus.Archived).Should().BeTrue();
    }

    [Fact]
    public void Uploaded_CannotTransitionTo_Processing()
    {
        TraceStatus.Uploaded.CanTransitionTo(TraceStatus.Processing).Should().BeFalse();
    }

    [Fact]
    public void Uploaded_CannotTransitionTo_Processed()
    {
        TraceStatus.Uploaded.CanTransitionTo(TraceStatus.Processed).Should().BeFalse();
    }

    [Fact]
    public void Uploaded_CannotTransitionTo_Failed()
    {
        TraceStatus.Uploaded.CanTransitionTo(TraceStatus.Failed).Should().BeFalse();
    }

    #endregion

    #region CanTransitionTo - From Validating

    [Fact]
    public void Validating_CanTransitionTo_Processing()
    {
        TraceStatus.Validating.CanTransitionTo(TraceStatus.Processing).Should().BeTrue();
    }

    [Fact]
    public void Validating_CanTransitionTo_Failed()
    {
        TraceStatus.Validating.CanTransitionTo(TraceStatus.Failed).Should().BeTrue();
    }

    [Fact]
    public void Validating_CannotTransitionTo_Processed()
    {
        TraceStatus.Validating.CanTransitionTo(TraceStatus.Processed).Should().BeFalse();
    }

    [Fact]
    public void Validating_CannotTransitionTo_Uploaded()
    {
        TraceStatus.Validating.CanTransitionTo(TraceStatus.Uploaded).Should().BeFalse();
    }

    #endregion

    #region CanTransitionTo - From Processing

    [Fact]
    public void Processing_CanTransitionTo_Processed()
    {
        TraceStatus.Processing.CanTransitionTo(TraceStatus.Processed).Should().BeTrue();
    }

    [Fact]
    public void Processing_CanTransitionTo_Failed()
    {
        TraceStatus.Processing.CanTransitionTo(TraceStatus.Failed).Should().BeTrue();
    }

    [Fact]
    public void Processing_CannotTransitionTo_Uploaded()
    {
        TraceStatus.Processing.CanTransitionTo(TraceStatus.Uploaded).Should().BeFalse();
    }

    [Fact]
    public void Processing_CannotTransitionTo_Archived()
    {
        TraceStatus.Processing.CanTransitionTo(TraceStatus.Archived).Should().BeFalse();
    }

    #endregion

    #region CanTransitionTo - From Processed

    [Fact]
    public void Processed_CanTransitionTo_Archived()
    {
        TraceStatus.Processed.CanTransitionTo(TraceStatus.Archived).Should().BeTrue();
    }

    [Fact]
    public void Processed_CannotTransitionTo_Uploaded()
    {
        TraceStatus.Processed.CanTransitionTo(TraceStatus.Uploaded).Should().BeFalse();
    }

    [Fact]
    public void Processed_CannotTransitionTo_Failed()
    {
        TraceStatus.Processed.CanTransitionTo(TraceStatus.Failed).Should().BeFalse();
    }

    #endregion

    #region CanTransitionTo - From Failed

    [Fact]
    public void Failed_CanTransitionTo_Uploaded()
    {
        // Retry
        TraceStatus.Failed.CanTransitionTo(TraceStatus.Uploaded).Should().BeTrue();
    }

    [Fact]
    public void Failed_CanTransitionTo_Archived()
    {
        TraceStatus.Failed.CanTransitionTo(TraceStatus.Archived).Should().BeTrue();
    }

    [Fact]
    public void Failed_CannotTransitionTo_Processed()
    {
        TraceStatus.Failed.CanTransitionTo(TraceStatus.Processed).Should().BeFalse();
    }

    #endregion

    #region CanTransitionTo - From Archived

    [Fact]
    public void Archived_CanTransitionTo_Uploaded()
    {
        // Restore
        TraceStatus.Archived.CanTransitionTo(TraceStatus.Uploaded).Should().BeTrue();
    }

    [Fact]
    public void Archived_CannotTransitionTo_Processed()
    {
        TraceStatus.Archived.CanTransitionTo(TraceStatus.Processed).Should().BeFalse();
    }

    #endregion

    #region CanEdit

    [Fact]
    public void Processed_CanEdit_ShouldBeTrue()
    {
        TraceStatus.Processed.CanEdit.Should().BeTrue();
    }

    [Theory]
    [InlineData(nameof(TraceStatus.Uploaded))]
    [InlineData(nameof(TraceStatus.Validating))]
    [InlineData(nameof(TraceStatus.Processing))]
    [InlineData(nameof(TraceStatus.Failed))]
    [InlineData(nameof(TraceStatus.Archived))]
    public void NonProcessed_CanEdit_ShouldBeFalse(string statusName)
    {
        // Arrange
        var status = TraceStatus.FromName(statusName);

        // Act & Assert
        if (status != TraceStatus.Processed)
        {
            status.CanEdit.Should().BeFalse();
        }
    }

    #endregion

    #region CanRetry

    [Fact]
    public void Failed_CanRetry_ShouldBeTrue()
    {
        TraceStatus.Failed.CanRetry.Should().BeTrue();
    }

    [Theory]
    [InlineData(nameof(TraceStatus.Uploaded))]
    [InlineData(nameof(TraceStatus.Validating))]
    [InlineData(nameof(TraceStatus.Processing))]
    [InlineData(nameof(TraceStatus.Processed))]
    [InlineData(nameof(TraceStatus.Archived))]
    public void NonFailed_CanRetry_ShouldBeFalse(string statusName)
    {
        // Arrange
        var status = TraceStatus.FromName(statusName);

        // Act & Assert
        if (status != TraceStatus.Failed)
        {
            status.CanRetry.Should().BeFalse();
        }
    }

    #endregion

    #region CanDelete

    [Fact]
    public void Processing_CanDelete_ShouldBeFalse()
    {
        TraceStatus.Processing.CanDelete.Should().BeFalse();
    }

    [Fact]
    public void Validating_CanDelete_ShouldBeFalse()
    {
        TraceStatus.Validating.CanDelete.Should().BeFalse();
    }

    [Theory]
    [InlineData(nameof(TraceStatus.Uploaded))]
    [InlineData(nameof(TraceStatus.Processed))]
    [InlineData(nameof(TraceStatus.Failed))]
    [InlineData(nameof(TraceStatus.Archived))]
    public void NonProcessingStatuses_CanDelete_ShouldBeTrue(string statusName)
    {
        // Arrange
        var status = TraceStatus.FromName(statusName);

        // Act & Assert
        status.CanDelete.Should().BeTrue();
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Uploaded")]
    [InlineData(2, "Validating")]
    [InlineData(3, "Processing")]
    [InlineData(4, "Processed")]
    [InlineData(5, "Failed")]
    [InlineData(6, "Archived")]
    public void FromId_ShouldReturnCorrectStatus(int id, string expectedName)
    {
        // Act
        var status = TraceStatus.FromId(id);

        // Assert
        status.Name.Should().Be(expectedName);
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Uploaded", 1)]
    [InlineData("Validating", 2)]
    [InlineData("Processing", 3)]
    [InlineData("Processed", 4)]
    [InlineData("Failed", 5)]
    [InlineData("Archived", 6)]
    public void FromName_ShouldReturnCorrectStatus(string name, int expectedId)
    {
        // Act
        var status = TraceStatus.FromName(name);

        // Assert
        status.Id.Should().Be(expectedId);
    }

    #endregion
}
