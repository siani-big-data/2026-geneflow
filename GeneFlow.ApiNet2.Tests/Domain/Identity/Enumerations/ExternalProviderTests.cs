using GeneFlow.ApiNet2.Domain.Identity.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.Enumerations;

/// <summary>
/// Unit tests for the ExternalProvider enumeration.
/// </summary>
public class ExternalProviderTests
{
    #region Static Instances

    [Fact]
    public void Google_ShouldHaveCorrectIdAndName()
    {
        // Assert
        ExternalProvider.Google.Id.Should().Be(1);
        ExternalProvider.Google.Name.Should().Be("Google");
    }

    [Fact]
    public void GitHub_ShouldHaveCorrectIdAndName()
    {
        // Assert
        ExternalProvider.GitHub.Id.Should().Be(2);
        ExternalProvider.GitHub.Name.Should().Be("GitHub");
    }

    #endregion

    #region FromId

    [Fact]
    public void FromId_WithGoogleId_ShouldReturnGoogle()
    {
        // Act
        var result = ExternalProvider.FromId(1);

        // Assert
        result.Should().Be(ExternalProvider.Google);
    }

    [Fact]
    public void FromId_WithGitHubId_ShouldReturnGitHub()
    {
        // Act
        var result = ExternalProvider.FromId(2);

        // Assert
        result.Should().Be(ExternalProvider.GitHub);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = ExternalProvider.FromId(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromId_WithZero_ShouldReturnNull()
    {
        // Act
        var result = ExternalProvider.FromId(0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromId_WithNegativeId_ShouldReturnNull()
    {
        // Act
        var result = ExternalProvider.FromId(-1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region FromName

    [Fact]
    public void FromName_WithGoogle_ShouldReturnGoogle()
    {
        // Act
        var result = ExternalProvider.FromName("Google");

        // Assert
        result.Should().Be(ExternalProvider.Google);
    }

    [Fact]
    public void FromName_WithGitHub_ShouldReturnGitHub()
    {
        // Act
        var result = ExternalProvider.FromName("GitHub");

        // Assert
        result.Should().Be(ExternalProvider.GitHub);
    }

    [Fact]
    public void FromName_CaseInsensitive_ShouldReturnCorrectProvider()
    {
        // Act
        var googleLower = ExternalProvider.FromName("google");
        var googleUpper = ExternalProvider.FromName("GOOGLE");
        var gitHubMixed = ExternalProvider.FromName("GiThUb");

        // Assert
        googleLower.Should().Be(ExternalProvider.Google);
        googleUpper.Should().Be(ExternalProvider.Google);
        gitHubMixed.Should().Be(ExternalProvider.GitHub);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var result = ExternalProvider.FromName("Facebook");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromName_WithEmptyString_ShouldReturnNull()
    {
        // Act
        var result = ExternalProvider.FromName(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAll

    [Fact]
    public void GetAll_ShouldReturnAllProviders()
    {
        // Act
        var all = ExternalProvider.GetAll();

        // Assert
        all.Should().HaveCount(2);
        all.Should().Contain(ExternalProvider.Google);
        all.Should().Contain(ExternalProvider.GitHub);
    }

    #endregion

    #region TryFromId

    [Fact]
    public void TryFromId_WithValidId_ShouldReturnTrueAndProvider()
    {
        // Act
        var success = ExternalProvider.TryFromId(1, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().Be(ExternalProvider.Google);
    }

    [Fact]
    public void TryFromId_WithInvalidId_ShouldReturnFalseAndNull()
    {
        // Act
        var success = ExternalProvider.TryFromId(999, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    #endregion

    #region TryFromName

    [Fact]
    public void TryFromName_WithValidName_ShouldReturnTrueAndProvider()
    {
        // Act
        var success = ExternalProvider.TryFromName("GitHub", out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().Be(ExternalProvider.GitHub);
    }

    [Fact]
    public void TryFromName_WithInvalidName_ShouldReturnFalseAndNull()
    {
        // Act
        var success = ExternalProvider.TryFromName("Unknown", out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    #endregion

    #region IsDefined

    [Fact]
    public void IsDefined_WithValidId_ShouldReturnTrue()
    {
        // Assert
        ExternalProvider.IsDefined(1).Should().BeTrue();
        ExternalProvider.IsDefined(2).Should().BeTrue();
    }

    [Fact]
    public void IsDefined_WithInvalidId_ShouldReturnFalse()
    {
        // Assert
        ExternalProvider.IsDefined(0).Should().BeFalse();
        ExternalProvider.IsDefined(999).Should().BeFalse();
    }

    [Fact]
    public void IsDefined_WithValidName_ShouldReturnTrue()
    {
        // Assert
        ExternalProvider.IsDefined("Google").Should().BeTrue();
        ExternalProvider.IsDefined("GitHub").Should().BeTrue();
        ExternalProvider.IsDefined("google").Should().BeTrue(); // case-insensitive
    }

    [Fact]
    public void IsDefined_WithInvalidName_ShouldReturnFalse()
    {
        // Assert
        ExternalProvider.IsDefined("Unknown").Should().BeFalse();
        ExternalProvider.IsDefined("Facebook").Should().BeFalse();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameProvider_ShouldReturnTrue()
    {
        // Arrange
        var google1 = ExternalProvider.FromId(1);
        var google2 = ExternalProvider.FromName("Google");

        // Assert
        google1.Should().Be(google2);
        (google1 == google2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentProviders_ShouldReturnFalse()
    {
        // Assert
        ExternalProvider.Google.Should().NotBe(ExternalProvider.GitHub);
        (ExternalProvider.Google == ExternalProvider.GitHub).Should().BeFalse();
        (ExternalProvider.Google != ExternalProvider.GitHub).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Assert
        ExternalProvider.Google.Equals(null).Should().BeFalse();
        (ExternalProvider.Google == null).Should().BeFalse();
    }

    #endregion

    #region Implicit Conversions

    [Fact]
    public void ImplicitConversionToInt_ShouldReturnId()
    {
        // Arrange
        int googleId = ExternalProvider.Google;
        int gitHubId = ExternalProvider.GitHub;

        // Assert
        googleId.Should().Be(1);
        gitHubId.Should().Be(2);
    }

    [Fact]
    public void ImplicitConversionToString_ShouldReturnName()
    {
        // Arrange
        string googleName = ExternalProvider.Google;
        string gitHubName = ExternalProvider.GitHub;

        // Assert
        googleName.Should().Be("Google");
        gitHubName.Should().Be("GitHub");
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnName()
    {
        // Assert
        ExternalProvider.Google.ToString().Should().Be("Google");
        ExternalProvider.GitHub.ToString().Should().Be("GitHub");
    }

    #endregion

    #region CompareTo

    [Fact]
    public void CompareTo_ShouldCompareById()
    {
        // Assert
        ExternalProvider.Google.CompareTo(ExternalProvider.GitHub).Should().BeLessThan(0);
        ExternalProvider.GitHub.CompareTo(ExternalProvider.Google).Should().BeGreaterThan(0);
        ExternalProvider.Google.CompareTo(ExternalProvider.Google).Should().Be(0);
    }

    [Fact]
    public void CompareTo_WithNull_ShouldReturnPositive()
    {
        // Assert
        ExternalProvider.Google.CompareTo(null).Should().BeGreaterThan(0);
    }

    #endregion

    #region GetHashCode

    [Fact]
    public void GetHashCode_SameProvider_ShouldBeSame()
    {
        // Arrange
        var google1 = ExternalProvider.FromId(1);
        var google2 = ExternalProvider.FromName("Google");

        // Assert
        google1!.GetHashCode().Should().Be(google2!.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentProviders_ShouldBeDifferent()
    {
        // Assert
        ExternalProvider.Google.GetHashCode().Should().NotBe(ExternalProvider.GitHub.GetHashCode());
    }

    #endregion
}
