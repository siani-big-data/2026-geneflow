using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles.ValueObjects;

/// <summary>
/// Unit tests for the ResearchIdentifiers value object.
/// </summary>
public class ResearchIdentifiersTests
{
    #region Create - Valid ORCID Cases

    [Theory]
    [InlineData("0000-0002-1825-0097")]
    [InlineData("0000-0001-5109-3700")]
    [InlineData("0000-0002-9079-593X")]
    public void Create_WithValidOrcidId_ShouldReturnSuccess(string orcidId)
    {
        // Act
        var result = ResearchIdentifiers.Create(orcidId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidId.Should().Be(orcidId);
    }

    [Fact]
    public void Create_WithValidOrcid_ShouldGenerateOrcidUrl()
    {
        // Arrange
        const string orcidId = "0000-0002-1825-0097";

        // Act
        var result = ResearchIdentifiers.Create(orcidId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidUrl.Should().Be($"https://orcid.org/{orcidId}");
    }

    #endregion

    #region Create - Invalid ORCID Cases

    [Theory]
    [InlineData("1234-5678-9012-3456-7890")] // Too long
    [InlineData("1234-5678-9012")]           // Too short
    [InlineData("1234567890123456")]          // No dashes
    [InlineData("XXXX-XXXX-XXXX-XXXX")]       // Letters not allowed
    [InlineData("0000-0000-0000-000Y")]       // Invalid checksum character
    public void Create_WithInvalidOrcidFormat_ShouldReturnFailure(string orcidId)
    {
        // Act
        var result = ResearchIdentifiers.Create(orcidId, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("OrcidId");
    }

    #endregion

    #region Create - Valid Website Cases

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://www.university.edu/~researcher")]
    [InlineData("http://lab.example.org/members")]
    [InlineData("https://github.com/username")]
    public void Create_WithValidWebsite_ShouldReturnSuccess(string website)
    {
        // Act
        var result = ResearchIdentifiers.Create(null, website);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Website.Should().Be(website);
    }

    #endregion

    #region Create - Invalid Website Cases

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("www.example.com")]
    public void Create_WithInvalidWebsiteFormat_ShouldReturnFailure(string website)
    {
        // Act
        var result = ResearchIdentifiers.Create(null, website);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Website");
    }

    [Fact]
    public void Create_WithWebsiteTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longWebsite = "https://" + new string('a', ResearchIdentifiers.WebsiteMaxLength) + ".com";

        // Act
        var result = ResearchIdentifiers.Create(null, longWebsite);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Website");
    }

    #endregion

    #region Create - Both Values

    [Fact]
    public void Create_WithBothOrcidAndWebsite_ShouldReturnSuccess()
    {
        // Arrange
        const string orcidId = "0000-0002-1825-0097";
        const string website = "https://example.com/profile";

        // Act
        var result = ResearchIdentifiers.Create(orcidId, website);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidId.Should().Be(orcidId);
        result.Value.Website.Should().Be(website);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void Create_WithNullOrEmptyBoth_ShouldReturnSuccessWithNullValues(string? orcidId, string? website)
    {
        // Act
        var result = ResearchIdentifiers.Create(orcidId, website);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidId.Should().BeNull();
        result.Value.Website.Should().BeNull();
        result.Value.OrcidUrl.Should().BeNull();
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldReturnIdentifiersWithNullValues()
    {
        // Act
        var identifiers = ResearchIdentifiers.Empty;

        // Assert
        identifiers.OrcidId.Should().BeNull();
        identifiers.Website.Should().BeNull();
        identifiers.OrcidUrl.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var id1 = ResearchIdentifiers.Create("0000-0002-1825-0097", "https://example.com").Value;
        var id2 = ResearchIdentifiers.Create("0000-0002-1825-0097", "https://example.com").Value;

        // Assert
        id1.Should().Be(id2);
    }

    [Fact]
    public void Equals_WithDifferentOrcid_ShouldReturnFalse()
    {
        // Arrange
        var id1 = ResearchIdentifiers.Create("0000-0002-1825-0097", null).Value;
        var id2 = ResearchIdentifiers.Create("0000-0001-5109-3700", null).Value;

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void Equals_WithDifferentWebsite_ShouldReturnFalse()
    {
        // Arrange
        var id1 = ResearchIdentifiers.Create(null, "https://example1.com").Value;
        var id2 = ResearchIdentifiers.Create(null, "https://example2.com").Value;

        // Assert
        id1.Should().NotBe(id2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_WithBothValues_ShouldIncludeBoth()
    {
        // Arrange
        var identifiers = ResearchIdentifiers.Create("0000-0002-1825-0097", "https://example.com").Value;

        // Act
        var result = identifiers.ToString();

        // Assert
        result.Should().Contain("ORCID");
        result.Should().Contain("Website");
    }

    [Fact]
    public void ToString_WithOnlyOrcid_ShouldShowOnlyOrcid()
    {
        // Arrange
        var identifiers = ResearchIdentifiers.Create("0000-0002-1825-0097", null).Value;

        // Act
        var result = identifiers.ToString();

        // Assert
        result.Should().Contain("ORCID");
        result.Should().NotContain("Website");
    }

    [Fact]
    public void ToString_WithOnlyWebsite_ShouldShowOnlyWebsite()
    {
        // Arrange
        var identifiers = ResearchIdentifiers.Create(null, "https://example.com").Value;

        // Act
        var result = identifiers.ToString();

        // Assert
        result.Should().Contain("Website");
        result.Should().NotContain("ORCID");
    }

    [Fact]
    public void ToString_WithEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var identifiers = ResearchIdentifiers.Empty;

        // Act
        var result = identifiers.ToString();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
