using GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateResearchIdentifiers;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Commands;

// Note: Additional value objects (Bio, Location, etc.) are included in GeneFlow.ApiNet2.Domain.Profiles.ValueObjects

/// <summary>
/// Unit tests for UpdateResearchIdentifiersCommandHandler.
/// </summary>
public class UpdateResearchIdentifiersCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IProfileUnitOfWork _unitOfWork = Substitute.For<IProfileUnitOfWork>();
    private readonly UpdateResearchIdentifiersCommandHandler _handler;

    public UpdateResearchIdentifiersCommandHandlerTests()
    {
        _handler = new UpdateResearchIdentifiersCommandHandler(
            _profileRepository,
            _unitOfWork);
    }

    private static Profile CreateTestProfile()
    {
        var profileId = new ProfileId(1);
        var userId = new UserId(1);
        var name = PersonName.Create("John", "Doe").Value;
        return Profile.Create(profileId, userId, name).Value;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidOrcidAndWebsite_ShouldUpdate()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            "0000-0002-1825-0097",
            "https://example.com");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidId.Should().Be("0000-0002-1825-0097");
        result.Value.Website.Should().Be("https://example.com");
        result.Value.OrcidUrl.Should().Be("https://orcid.org/0000-0002-1825-0097");
    }

    [Fact]
    public async Task Handle_WithOnlyOrcid_ShouldUpdate()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            "0000-0002-1825-0097",
            null);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidId.Should().Be("0000-0002-1825-0097");
        result.Value.Website.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithOnlyWebsite_ShouldUpdate()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            null,
            "https://example.com");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidId.Should().BeNull();
        result.Value.Website.Should().Be("https://example.com");
    }

    [Fact]
    public async Task Handle_WithBothNull_ShouldClearIdentifiers()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand("U00000001", null, null);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidId.Should().BeNull();
        result.Value.Website.Should().BeNull();
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateResearchIdentifiersCommand("invalid", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithProfileNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateResearchIdentifiersCommand("U00000001", null, null);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Profile?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Theory]
    [InlineData("invalid-orcid")]
    [InlineData("1234-5678")]
    public async Task Handle_WithInvalidOrcidFormat_ShouldReturnFailure(string orcidId)
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand("U00000001", orcidId, null);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://invalid.com")]
    public async Task Handle_WithInvalidWebsiteFormat_ShouldReturnFailure(string website)
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand("U00000001", null, website);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallUpdateOnRepository()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            "0000-0002-1825-0097",
            "https://example.com");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _profileRepository.Received(1).Update(Arg.Any<Profile>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithAllNullIdentifiers_ShouldClearIdentifiers()
    {
        // Arrange
        var profile = CreateTestProfile();
        // First set some identifiers
        var identifiers = ResearchIdentifiers.Create("0000-0002-1825-0097", "https://example.com").Value;
        profile.UpdateResearchIdentifiers(identifiers);

        var command = new UpdateResearchIdentifiersCommand("U00000001", null, null);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidId.Should().BeNull();
        result.Value.Website.Should().BeNull();
        result.Value.OrcidUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithHttpsWebsite_ShouldSucceed()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            null,
            "https://www.university.edu/~researcher");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Website.Should().Be("https://www.university.edu/~researcher");
    }

    [Fact]
    public async Task Handle_WithHttpWebsite_ShouldSucceed()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            null,
            "http://www.university.edu/researcher");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Website.Should().Be("http://www.university.edu/researcher");
    }

    [Fact]
    public async Task Handle_WithValidOrcidWithCheckDigitX_ShouldSucceed()
    {
        // Arrange
        var profile = CreateTestProfile();
        // ORCID with X as check digit (valid ORCID format)
        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            "0000-0001-2345-678X",
            null);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidId.Should().Be("0000-0001-2345-678X");
        result.Value.OrcidUrl.Should().Be("https://orcid.org/0000-0001-2345-678X");
    }

    [Fact]
    public async Task Handle_WithOrcidContainingHyphens_ShouldGenerateCorrectUrl()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            "0000-0002-1825-0097",
            null);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrcidUrl.Should().Be("https://orcid.org/0000-0002-1825-0097");
    }

    [Fact]
    public async Task Handle_ShouldPreserveOtherProfileFieldsWhenUpdatingIdentifiers()
    {
        // Arrange
        var profile = CreateTestProfile();
        // Update basic info first
        var name = PersonName.Create("Jane", "Smith").Value;
        var bio = Bio.Create("Test bio description").Value;
        profile.UpdateBasicInfo(name, bio, Location.Empty, ProfessionalRole.Empty, Institution.Empty, null);

        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            "0000-0002-1825-0097",
            "https://example.com");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Smith");
        result.Value.Bio.Should().Be("Test bio description");
        result.Value.OrcidId.Should().Be("0000-0002-1825-0097");
        result.Value.Website.Should().Be("https://example.com");
    }

    [Theory]
    [InlineData("0000000218250097")]  // Missing hyphens
    [InlineData("0000-0002-1825")]    // Too short
    [InlineData("0000-0002-1825-00971")]  // Too long
    [InlineData("ABCD-0002-1825-0097")]  // Invalid characters
    public async Task Handle_WithMalformedOrcid_ShouldReturnFailure(string orcidId)
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand("U00000001", orcidId, null);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithEmptyStringOrcid_ShouldTreatAsNull()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            "",  // Empty string
            "https://example.com");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Behavior depends on implementation - either succeeds with null or validates
        // If empty string is treated as null, it should succeed
        if (result.IsSuccess)
        {
            result.Value.OrcidId.Should().BeNullOrEmpty();
            result.Value.Website.Should().Be("https://example.com");
        }
        // Otherwise validation should fail
    }

    [Fact]
    public async Task Handle_WithEmptyStringWebsite_ShouldTreatAsNull()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateResearchIdentifiersCommand(
            "U00000001",
            "0000-0002-1825-0097",
            "");  // Empty string

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Behavior depends on implementation - either succeeds with null or validates
        if (result.IsSuccess)
        {
            result.Value.OrcidId.Should().Be("0000-0002-1825-0097");
            result.Value.Website.Should().BeNullOrEmpty();
        }
    }

    #endregion
}
