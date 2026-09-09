using GeneFlow.ApiNet2.Domain.Identity.Entities;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.Entities;

/// <summary>
/// Unit tests for the TwoFactorCode entity.
/// </summary>
public class TwoFactorCodeTests
{
    #region Create

    [Fact]
    public void Create_ShouldGenerateSixDigitCode()
    {
        // Act
        var code = TwoFactorCode.Create();

        // Assert
        code.Code.Should().HaveLength(6);
        code.Code.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void Create_ShouldSetCreatedAtToUtcNow()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var code = TwoFactorCode.Create();

        // Assert
        code.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        code.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldSetExpiresAtToTenMinutesFromNow()
    {
        // Arrange
        var expectedExpiry = DateTime.UtcNow.AddMinutes(10);

        // Act
        var code = TwoFactorCode.Create();

        // Assert
        code.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldNotBeUsed()
    {
        // Act
        var code = TwoFactorCode.Create();

        // Assert
        code.IsUsed.Should().BeFalse();
        code.UsedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var code = TwoFactorCode.Create();

        // Assert
        code.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_MultipleCodes_ShouldGenerateDifferentCodes()
    {
        // Act
        var codes = Enumerable.Range(0, 10)
            .Select(_ => TwoFactorCode.Create())
            .ToList();

        // Assert - While not guaranteed, 10 random 6-digit codes should likely be unique
        var uniqueCodes = codes.Select(c => c.Code).Distinct().Count();
        uniqueCodes.Should().BeGreaterThan(1);
    }

    #endregion

    #region IsValid

    [Fact]
    public void IsValid_WhenNotUsedAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var code = TwoFactorCode.Create();

        // Act & Assert
        code.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenUsed_ShouldReturnFalse()
    {
        // Arrange
        var code = TwoFactorCode.Create();
        code.MarkAsUsed();

        // Act & Assert
        code.IsValid.Should().BeFalse();
    }

    #endregion

    #region IsExpired

    [Fact]
    public void IsExpired_WhenCreatedJustNow_ShouldReturnFalse()
    {
        // Arrange
        var code = TwoFactorCode.Create();

        // Act & Assert
        code.IsExpired.Should().BeFalse();
    }

    #endregion

    #region MarkAsUsed

    [Fact]
    public void MarkAsUsed_ShouldSetIsUsedToTrue()
    {
        // Arrange
        var code = TwoFactorCode.Create();

        // Act
        code.MarkAsUsed();

        // Assert
        code.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void MarkAsUsed_ShouldSetUsedAtTimestamp()
    {
        // Arrange
        var code = TwoFactorCode.Create();
        var beforeUsed = DateTime.UtcNow;

        // Act
        code.MarkAsUsed();

        // Assert
        code.UsedAt.Should().NotBeNull();
        code.UsedAt.Should().BeOnOrAfter(beforeUsed);
        code.UsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Validate

    [Fact]
    public void Validate_WithMatchingCodeAndValid_ShouldReturnTrue()
    {
        // Arrange
        var code = TwoFactorCode.Create();
        var codeValue = code.Code;

        // Act
        var result = code.Validate(codeValue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNonMatchingCode_ShouldReturnFalse()
    {
        // Arrange
        var code = TwoFactorCode.Create();

        // Act
        var result = code.Validate("000000");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenAlreadyUsed_ShouldReturnFalse()
    {
        // Arrange
        var code = TwoFactorCode.Create();
        var codeValue = code.Code;
        code.MarkAsUsed();

        // Act
        var result = code.Validate(codeValue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithPartialMatch_ShouldReturnFalse()
    {
        // Arrange
        var code = TwoFactorCode.Create();
        var partialCode = code.Code.Substring(0, 3);

        // Act
        var result = code.Validate(partialCode);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
