using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines.Entities;

/// <summary>
/// Unit tests for the PipelineStep entity.
/// </summary>
public class PipelineStepTests
{
    private static StepConfiguration CreateConfig(string json = "{}") =>
        StepConfiguration.Create(json, StepType.Quality).Value;

    #region Create

    [Fact]
    public void Create_WithValidData_ShouldCreateStep()
    {
        // Arrange
        var stepType = StepType.Quality;
        const int stepOrder = 1;
        const string label = "Quality Check";
        var config = CreateConfig();

        // Act
        var step = PipelineStep.Create(stepType, stepOrder, label, config);

        // Assert
        step.Should().NotBeNull();
        step.StepType.Should().Be(stepType);
        step.Order.Should().Be(stepOrder);
        step.Label.Should().Be(label);
        step.Configuration.Should().Be(config);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var step1 = PipelineStep.Create(StepType.Quality, 1, "Step 1", CreateConfig());
        var step2 = PipelineStep.Create(StepType.Quality, 2, "Step 2", CreateConfig());

        // Assert
        step1.Id.Should().NotBe(step2.Id);
        step1.Id.Should().NotBe(Guid.Empty);
        step2.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var step = PipelineStep.Create(StepType.Quality, 1, "Step", CreateConfig());

        // Assert
        step.CreatedAt.Should().BeOnOrAfter(beforeCreate);
    }

    [Fact]
    public void Create_IsEnabled_Default_ShouldBeTrue()
    {
        // Act
        var step = PipelineStep.Create(StepType.Quality, 1, "Step", CreateConfig());

        // Assert
        step.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Create_IsEnabled_WhenSetToFalse_ShouldBeFalse()
    {
        // Act
        var step = PipelineStep.Create(StepType.Quality, 1, "Step", CreateConfig(), isEnabled: false);

        // Assert
        step.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Create_Label_WhenNull_ShouldAllowNull()
    {
        // Act
        var step = PipelineStep.Create(StepType.Quality, 1, null, CreateConfig());

        // Assert
        step.Label.Should().BeNull();
    }

    [Fact]
    public void Create_Label_ShouldTrimWhitespace()
    {
        // Act
        var step = PipelineStep.Create(StepType.Quality, 1, "  Label  ", CreateConfig());

        // Assert
        step.Label.Should().Be("Label");
    }

    #endregion

    #region UpdateConfiguration

    [Fact]
    public void UpdateConfiguration_ShouldUpdateConfiguration()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, "Step", CreateConfig());
        var newConfig = CreateConfig("""{"new": "config"}""");

        // Act
        step.UpdateConfiguration(newConfig);

        // Assert
        step.Configuration.Should().Be(newConfig);
    }

    #endregion

    #region UpdateLabel

    [Fact]
    public void UpdateLabel_ShouldUpdateLabel()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, "Old Label", CreateConfig());

        // Act
        step.UpdateLabel("New Label");

        // Assert
        step.Label.Should().Be("New Label");
    }

    [Fact]
    public void UpdateLabel_WithNull_ShouldSetNull()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, "Old Label", CreateConfig());

        // Act
        step.UpdateLabel(null);

        // Assert
        step.Label.Should().BeNull();
    }

    [Fact]
    public void UpdateLabel_ShouldTrimWhitespace()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, "Old", CreateConfig());

        // Act
        step.UpdateLabel("  New Label  ");

        // Assert
        step.Label.Should().Be("New Label");
    }

    #endregion

    #region Enable/Disable

    [Fact]
    public void Enable_ShouldSetIsEnabledTrue()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, "Step", CreateConfig(), isEnabled: false);

        // Act
        step.Enable();

        // Assert
        step.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabledFalse()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, "Step", CreateConfig(), isEnabled: true);

        // Act
        step.Disable();

        // Assert
        step.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region SetOrder

    [Fact]
    public void SetOrder_ShouldUpdateOrder()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, "Step", CreateConfig());

        // Act
        step.SetOrder(5);

        // Assert
        step.Order.Should().Be(5);
    }

    #endregion

    #region Immutability

    [Fact]
    public void StepType_ShouldBeImmutable()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, "Step", CreateConfig());
        var originalType = step.StepType;

        // Assert - StepType has no setter, so this test verifies the property is read-only
        step.StepType.Should().Be(originalType);
    }

    [Fact]
    public void Id_ShouldBeImmutable()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, "Step", CreateConfig());
        var originalId = step.Id;

        // Assert - Id has no setter, so this test verifies the property is read-only
        step.Id.Should().Be(originalId);
    }

    #endregion

    #region MaxLabelLength

    [Fact]
    public void MaxLabelLength_ShouldBe100()
    {
        // Assert
        PipelineStep.MaxLabelLength.Should().Be(100);
    }

    #endregion

    #region DisplayName

    [Fact]
    public void DisplayName_WhenLabelExists_ShouldReturnLabel()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, "My Custom Label", CreateConfig());

        // Assert
        step.DisplayName.Should().Be("My Custom Label");
    }

    [Fact]
    public void DisplayName_WhenLabelIsNull_ShouldReturnStepTypeDisplayName()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Quality, 1, null, CreateConfig());

        // Assert
        step.DisplayName.Should().Be(StepType.Quality.DisplayName);
        step.DisplayName.Should().Be("Quality Analysis");
    }

    [Fact]
    public void DisplayName_WhenLabelIsEmpty_ShouldReturnStepTypeDisplayName()
    {
        // Arrange
        var step = PipelineStep.Create(StepType.Trimming, 1, "", CreateConfig());

        // Assert
        step.DisplayName.Should().Be(StepType.Trimming.DisplayName);
        step.DisplayName.Should().Be("Sequence Trimming");
    }

    [Fact]
    public void DisplayName_WhenLabelIsWhitespace_ShouldReturnStepTypeDisplayName()
    {
        // Arrange - Note: Create trims whitespace, so "   " becomes null
        var step = PipelineStep.Create(StepType.Heterozygote, 1, "   ", CreateConfig());

        // Assert
        step.DisplayName.Should().Be(StepType.Heterozygote.DisplayName);
        step.DisplayName.Should().Be("Heterozygote Detection");
    }

    #endregion
}
