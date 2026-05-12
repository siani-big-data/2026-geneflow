using GeneFlow.ApiNet2.Domain.Subscriptions;

namespace GeneFlow.ApiNet2.Tests.Domain.Subscriptions;

/// <summary>
/// Unit tests for the SubscriptionId strongly-typed identifier.
/// </summary>
public class SubscriptionIdTests
{
    #region Constructor

    [Fact]
    public void Constructor_ShouldCreateValidSubscriptionId()
    {
        // Act
        var subscriptionId = new SubscriptionId(1);

        // Assert
        subscriptionId.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithZero_ShouldCreateSubscriptionId()
    {
        // Act
        var subscriptionId = new SubscriptionId(0);

        // Assert
        subscriptionId.Should().NotBeNull();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnFormattedId()
    {
        // Arrange
        var subscriptionId = new SubscriptionId(1);

        // Act
        var result = subscriptionId.ToString();

        // Assert
        result.Should().Be("S00000001");
    }

    [Fact]
    public void ToString_WithLargerNumber_ShouldPadCorrectly()
    {
        // Arrange
        var subscriptionId = new SubscriptionId(12345);

        // Act
        var result = subscriptionId.ToString();

        // Assert
        result.Should().Be("S00012345");
    }

    [Fact]
    public void ToString_WithMaxDigits_ShouldNotPad()
    {
        // Arrange
        var subscriptionId = new SubscriptionId(12345678);

        // Act
        var result = subscriptionId.ToString();

        // Assert
        result.Should().Be("S12345678");
    }

    #endregion

    #region Parse

    [Fact]
    public void Parse_WithValidId_ShouldReturnSubscriptionId()
    {
        // Arrange
        const string id = "S00000001";

        // Act
        var subscriptionId = SubscriptionId.Parse(id);

        // Assert
        subscriptionId.Should().NotBeNull();
        subscriptionId.ToString().Should().Be(id);
    }

    [Fact]
    public void Parse_WithLargerNumber_ShouldReturnCorrectSubscriptionId()
    {
        // Arrange
        const string id = "S00012345";

        // Act
        var subscriptionId = SubscriptionId.Parse(id);

        // Assert
        subscriptionId.ToString().Should().Be(id);
    }

    [Fact]
    public void Parse_WithInvalidPrefix_ShouldThrow()
    {
        // Arrange
        const string id = "X00000001";

        // Act & Assert
        var act = () => SubscriptionId.Parse(id);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_WithInvalidFormat_ShouldThrow()
    {
        // Arrange
        const string id = "S123";

        // Act & Assert
        var act = () => SubscriptionId.Parse(id);
        act.Should().Throw<Exception>();
    }

    #endregion

    #region TryParse

    [Fact]
    public void TryParse_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        const string id = "S00000001";

        // Act
        var success = SubscriptionId.TryParse(id, out var subscriptionId);

        // Assert
        success.Should().BeTrue();
        subscriptionId.Should().NotBeNull();
        subscriptionId!.ToString().Should().Be(id);
    }

    [Fact]
    public void TryParse_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        const string id = "invalid";

        // Act
        var success = SubscriptionId.TryParse(id, out var subscriptionId);

        // Assert
        success.Should().BeFalse();
        subscriptionId.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithNull_ShouldReturnFalse()
    {
        // Act
        var success = SubscriptionId.TryParse(null, out var subscriptionId);

        // Assert
        success.Should().BeFalse();
        subscriptionId.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithWrongPrefix_ShouldReturnFalse()
    {
        // Arrange
        const string id = "U00000001"; // User prefix instead of Subscription

        // Act
        var success = SubscriptionId.TryParse(id, out var subscriptionId);

        // Assert
        success.Should().BeFalse();
        subscriptionId.Should().BeNull();
    }

    #endregion

    #region FromSequence

    [Fact]
    public void FromSequence_ShouldCreateSubscriptionId()
    {
        // Arrange
        const long sequenceValue = 42;

        // Act
        var subscriptionId = SubscriptionId.FromSequence(sequenceValue);

        // Assert
        subscriptionId.ToString().Should().Be("S00000042");
    }

    #endregion

    #region SequenceName

    [Fact]
    public void SequenceName_ShouldBeSubscriptions()
    {
        SubscriptionId.SequenceName.Should().Be("subscriptions");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var subscriptionId1 = new SubscriptionId(1);
        var subscriptionId2 = new SubscriptionId(1);

        // Assert
        subscriptionId1.Should().Be(subscriptionId2);
        (subscriptionId1 == subscriptionId2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var subscriptionId1 = new SubscriptionId(1);
        var subscriptionId2 = new SubscriptionId(2);

        // Assert
        subscriptionId1.Should().NotBe(subscriptionId2);
        (subscriptionId1 != subscriptionId2).Should().BeTrue();
    }

    #endregion
}
