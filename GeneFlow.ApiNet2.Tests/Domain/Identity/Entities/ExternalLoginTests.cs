using System.Reflection;
using GeneFlow.ApiNet2.Domain.Identity.Entities;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.Entities;

/// <summary>
/// Unit tests for the ExternalLogin entity.
/// </summary>
public class ExternalLoginTests
{
    #region Create

    [Fact]
    public void Create_WithValidData_ShouldCreateExternalLogin()
    {
        // Arrange
        var provider = ExternalProvider.Google;
        const string providerKey = "google_user_12345";
        const string displayName = "John Doe";

        // Act
        var externalLogin = InvokeCreate(provider, providerKey, displayName);

        // Assert
        externalLogin.Provider.Should().Be(provider);
        externalLogin.ProviderKey.Should().Be(providerKey);
        externalLogin.ProviderDisplayName.Should().Be(displayName);
    }

    [Fact]
    public void Create_ShouldSetLinkedAtToUtcNow()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var externalLogin = InvokeCreate(ExternalProvider.GitHub, "github_123");

        // Assert
        externalLogin.LinkedAt.Should().BeOnOrAfter(beforeCreate);
        externalLogin.LinkedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var externalLogin = InvokeCreate(ExternalProvider.Google, "google_123");

        // Assert
        externalLogin.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithNullDisplayName_ShouldAllowNull()
    {
        // Act
        var externalLogin = InvokeCreate(ExternalProvider.GitHub, "github_user_456", null);

        // Assert
        externalLogin.ProviderDisplayName.Should().BeNull();
    }

    [Fact]
    public void Create_WithGitHubProvider_ShouldSetCorrectProvider()
    {
        // Act
        var externalLogin = InvokeCreate(ExternalProvider.GitHub, "github_12345");

        // Assert
        externalLogin.Provider.Should().Be(ExternalProvider.GitHub);
        externalLogin.Provider.Name.Should().Be("GitHub");
    }

    [Fact]
    public void Create_WithGoogleProvider_ShouldSetCorrectProvider()
    {
        // Act
        var externalLogin = InvokeCreate(ExternalProvider.Google, "google_12345");

        // Assert
        externalLogin.Provider.Should().Be(ExternalProvider.Google);
        externalLogin.Provider.Name.Should().Be("Google");
    }

    #endregion

    #region UpdateDisplayName

    [Fact]
    public void UpdateDisplayName_ShouldUpdateProviderDisplayName()
    {
        // Arrange
        var externalLogin = InvokeCreate(ExternalProvider.Google, "google_123", "Original Name");
        const string newDisplayName = "Updated Name";

        // Act
        InvokeUpdateDisplayName(externalLogin, newDisplayName);

        // Assert
        externalLogin.ProviderDisplayName.Should().Be(newDisplayName);
    }

    [Fact]
    public void UpdateDisplayName_WithNull_ShouldSetToNull()
    {
        // Arrange
        var externalLogin = InvokeCreate(ExternalProvider.Google, "google_123", "Original Name");

        // Act
        InvokeUpdateDisplayName(externalLogin, null);

        // Assert
        externalLogin.ProviderDisplayName.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Invokes the internal Create method using reflection.
    /// </summary>
    private static ExternalLogin InvokeCreate(
        ExternalProvider provider,
        string providerKey,
        string? providerDisplayName = null)
    {
        var createMethod = typeof(ExternalLogin).GetMethod(
            "Create",
            BindingFlags.Static | BindingFlags.NonPublic);

        if (createMethod is null)
            throw new InvalidOperationException("Create method not found on ExternalLogin.");

        return (ExternalLogin)createMethod.Invoke(
            null,
            [provider, providerKey, providerDisplayName])!;
    }

    /// <summary>
    /// Invokes the internal UpdateDisplayName method using reflection.
    /// </summary>
    private static void InvokeUpdateDisplayName(ExternalLogin externalLogin, string? displayName)
    {
        var updateMethod = typeof(ExternalLogin).GetMethod(
            "UpdateDisplayName",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (updateMethod is null)
            throw new InvalidOperationException("UpdateDisplayName method not found on ExternalLogin.");

        updateMethod.Invoke(externalLogin, [displayName]);
    }

    #endregion
}
