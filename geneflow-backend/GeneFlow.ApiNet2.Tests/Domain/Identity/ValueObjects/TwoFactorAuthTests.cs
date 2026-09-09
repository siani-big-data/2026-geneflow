using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.ValueObjects;

/// <summary>
/// Unit tests for the TwoFactorAuth value object.
/// </summary>
public class TwoFactorAuthTests
{
    #region Disabled State

    [Fact]
    public void Disabled_ShouldReturnDisabledState()
    {
        // Act
        var auth = TwoFactorAuth.Disabled;

        // Assert
        auth.IsEnabled.Should().BeFalse();
        auth.IsTotpConfigured.Should().BeFalse();
        auth.TotpSecret.Should().BeNull();
        auth.Codes.Should().BeEmpty();
    }

    #endregion

    #region Enable

    [Fact]
    public void Enable_WhenDisabled_ShouldReturnEnabledState()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;

        // Act
        var result = auth.Enable();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.IsTotpConfigured.Should().BeFalse();
    }

    [Fact]
    public void Enable_WhenAlreadyEnabled_ShouldReturnFailure()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled.Enable().Value;

        // Act
        var result = auth.Enable();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.TwoFactorAlreadyEnabled);
    }

    #endregion

    #region EnableWithTotp

    [Fact]
    public void EnableWithTotp_WhenDisabled_ShouldEnableWithTotpSecret()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;
        var totpSecret = TwoFactorSecret.Create("encrypted_totp_secret").Value;

        // Act
        var result = auth.EnableWithTotp(totpSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.IsTotpConfigured.Should().BeTrue();
        result.Value.TotpSecret.Should().NotBeNull();
        result.Value.TotpSecret!.EncryptedSecret.Should().Be("encrypted_totp_secret");
    }

    [Fact]
    public void EnableWithTotp_WhenAlreadyEnabledWithTotp_ShouldReturnFailure()
    {
        // Arrange
        var totpSecret = TwoFactorSecret.Create("first_secret").Value;
        var auth = TwoFactorAuth.Disabled.EnableWithTotp(totpSecret).Value;
        var newTotpSecret = TwoFactorSecret.Create("second_secret").Value;

        // Act
        var result = auth.EnableWithTotp(newTotpSecret);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.TwoFactorAlreadyEnabled);
    }

    #endregion

    #region Disable

    [Fact]
    public void Disable_WhenEnabled_ShouldReturnDisabledState()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled.Enable().Value;

        // Act
        var result = auth.Disable();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Disable_WhenEnabledWithTotp_ShouldClearTotpSecret()
    {
        // Arrange
        var totpSecret = TwoFactorSecret.Create("encrypted_totp_secret").Value;
        var auth = TwoFactorAuth.Disabled.EnableWithTotp(totpSecret).Value;

        // Act
        var result = auth.Disable();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsEnabled.Should().BeFalse();
        result.Value.IsTotpConfigured.Should().BeFalse();
        result.Value.TotpSecret.Should().BeNull();
    }

    [Fact]
    public void Disable_WhenNotEnabled_ShouldReturnFailure()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;

        // Act
        var result = auth.Disable();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.TwoFactorNotEnabled);
    }

    #endregion

    #region GenerateCode

    [Fact]
    public void GenerateCode_ShouldReturnNewCodeAndUpdatedAuth()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;

        // Act
        var (newAuth, code) = auth.GenerateCode();

        // Assert
        code.Should().NotBeNull();
        code.Code.Should().HaveLength(6);
        code.IsValid.Should().BeTrue();
        code.IsUsed.Should().BeFalse();
        newAuth.Codes.Should().ContainSingle();
    }

    [Fact]
    public void GenerateCode_ShouldInvalidatePreviousUnusedCodes()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;
        var (authWithFirstCode, firstCode) = auth.GenerateCode();

        // Act
        var (authWithSecondCode, secondCode) = authWithFirstCode.GenerateCode();

        // Assert
        firstCode.IsUsed.Should().BeTrue();
        secondCode.IsUsed.Should().BeFalse();
        secondCode.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GenerateCode_ShouldGenerateSixDigitCode()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;

        // Act
        var (_, code) = auth.GenerateCode();

        // Assert
        code.Code.Should().MatchRegex(@"^\d{6}$");
    }

    #endregion

    #region ValidateCode

    [Fact]
    public void ValidateCode_WithValidCode_ShouldReturnSuccess()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;
        var (authWithCode, generatedCode) = auth.GenerateCode();

        // Act
        var result = authWithCode.ValidateCode(generatedCode.Code);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_WithValidCode_ShouldMarkCodeAsUsed()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;
        var (authWithCode, generatedCode) = auth.GenerateCode();

        // Act
        var result = authWithCode.ValidateCode(generatedCode.Code);

        // Assert
        result.IsSuccess.Should().BeTrue();
        generatedCode.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_WithInvalidCode_ShouldReturnFailure()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;
        var (authWithCode, _) = auth.GenerateCode();

        // Act
        var result = authWithCode.ValidateCode("000000");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidTwoFactorCode);
    }

    [Fact]
    public void ValidateCode_WithNoCodes_ShouldReturnFailure()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;

        // Act
        var result = auth.ValidateCode("123456");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidTwoFactorCode);
    }

    [Fact]
    public void ValidateCode_WithAlreadyUsedCode_ShouldReturnFailure()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled;
        var (authWithCode, generatedCode) = auth.GenerateCode();

        // Use the code once
        var firstValidation = authWithCode.ValidateCode(generatedCode.Code);
        firstValidation.IsSuccess.Should().BeTrue();

        // Act - Try to use the same code again
        var result = firstValidation.Value.ValidateCode(generatedCode.Code);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidTwoFactorCode);
    }

    #endregion

    #region IsTotpConfigured

    [Fact]
    public void IsTotpConfigured_WhenNoTotpSecret_ShouldReturnFalse()
    {
        // Arrange
        var auth = TwoFactorAuth.Disabled.Enable().Value;

        // Act & Assert
        auth.IsTotpConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsTotpConfigured_WhenTotpSecretExists_ShouldReturnTrue()
    {
        // Arrange
        var totpSecret = TwoFactorSecret.Create("encrypted_secret").Value;
        var auth = TwoFactorAuth.Disabled.EnableWithTotp(totpSecret).Value;

        // Act & Assert
        auth.IsTotpConfigured.Should().BeTrue();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameState_ShouldReturnTrue()
    {
        // Arrange
        var auth1 = TwoFactorAuth.Disabled;
        var auth2 = TwoFactorAuth.Disabled;

        // Act & Assert
        auth1.Should().Be(auth2);
    }

    [Fact]
    public void Equals_WithDifferentEnabledState_ShouldReturnFalse()
    {
        // Arrange
        var auth1 = TwoFactorAuth.Disabled;
        var auth2 = TwoFactorAuth.Disabled.Enable().Value;

        // Act & Assert
        auth1.Should().NotBe(auth2);
    }

    #endregion
}
