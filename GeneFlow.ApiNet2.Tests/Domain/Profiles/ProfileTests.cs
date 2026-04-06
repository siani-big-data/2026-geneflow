using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Enumerations;
using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles;

/// <summary>
/// Unit tests for the Profile aggregate root.
/// </summary>
public class ProfileTests
{
    private static ProfileId CreateProfileId(long value = 1) => new(value);
    private static UserId CreateUserId(long value = 1) => new(value);
    private static PersonName CreatePersonName(string firstName = "John", string? lastName = "Doe")
        => PersonName.Create(firstName, lastName).Value;

    #region Create

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var profileId = CreateProfileId();
        var userId = CreateUserId();
        var name = CreatePersonName();

        // Act
        var result = Profile.Create(profileId, userId, name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(profileId);
        result.Value.UserId.Should().Be(userId);
        result.Value.Name.Should().Be(name);
    }

    [Fact]
    public void Create_ShouldInitializeEmptyValueObjects()
    {
        // Arrange
        var profileId = CreateProfileId();
        var userId = CreateUserId();
        var name = CreatePersonName();

        // Act
        var result = Profile.Create(profileId, userId, name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Bio.Value.Should().BeNull();
        result.Value.Location.Value.Should().BeNull();
        result.Value.ProfessionalRole.Value.Should().BeNull();
        result.Value.Institution.Name.Should().BeNull();
        result.Value.ResearchField.Should().BeNull();
        result.Value.ResearchIdentifiers.OrcidId.Should().BeNull();
        result.Value.Photo.Url.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldRaiseProfileCreatedEvent()
    {
        // Arrange
        var profileId = CreateProfileId();
        var userId = CreateUserId();
        var name = CreatePersonName("Alice");

        // Act
        var result = Profile.Create(profileId, userId, name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvents = result.Value.DomainEvents;
        domainEvents.Should().ContainSingle();
        domainEvents.First().Should().BeOfType<ProfileCreatedEvent>();

        var evt = (ProfileCreatedEvent)domainEvents.First();
        evt.ProfileId.Should().Be(profileId);
        evt.UserId.Should().Be(userId);
        evt.FirstName.Should().Be("Alice");
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        // Arrange
        var profileId = CreateProfileId();
        var userId = CreateUserId();
        var name = CreatePersonName();
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = Profile.Create(profileId, userId, name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().BeOnOrAfter(beforeCreate);
    }

    #endregion

    #region Computed Properties

    [Fact]
    public void FullName_ShouldReturnNameFullName()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName("John", "Doe")).Value;

        // Assert
        profile.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void Initials_ShouldReturnNameInitials()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName("John", "Doe")).Value;

        // Assert
        profile.Initials.Should().Be("JD");
    }

    [Fact]
    public void IsComplete_WithFirstName_ShouldReturnTrue()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName("John")).Value;

        // Assert
        profile.IsComplete.Should().BeTrue();
    }

    #endregion

    #region UpdateBasicInfo

    [Fact]
    public void UpdateBasicInfo_ShouldUpdateAllFields()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName()).Value;
        var newName = PersonName.Create("Jane", "Smith").Value;
        var newBio = Bio.Create("New bio text").Value;
        var newLocation = Location.Create("New York").Value;
        var newRole = ProfessionalRole.Create("Senior Researcher").Value;
        var newInstitution = Institution.Create("MIT", "Biology").Value;
        var newField = ResearchField.Genomics;

        // Act
        var result = profile.UpdateBasicInfo(newName, newBio, newLocation, newRole, newInstitution, newField);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Name.Should().Be(newName);
        profile.Bio.Should().Be(newBio);
        profile.Location.Should().Be(newLocation);
        profile.ProfessionalRole.Should().Be(newRole);
        profile.Institution.Should().Be(newInstitution);
        profile.ResearchField.Should().Be(newField);
    }

    [Fact]
    public void UpdateBasicInfo_ShouldRaiseProfileUpdatedEvent()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName()).Value;
        profile.ClearDomainEvents();

        var newName = PersonName.Create("Jane", "Smith").Value;

        // Act
        profile.UpdateBasicInfo(
            newName,
            Bio.Empty,
            Location.Empty,
            ProfessionalRole.Empty,
            Institution.Empty,
            null);

        // Assert
        var domainEvents = profile.DomainEvents;
        domainEvents.Should().ContainSingle();
        domainEvents.First().Should().BeOfType<ProfileUpdatedEvent>();
    }

    [Fact]
    public void UpdateBasicInfo_ShouldSetModifiedAt()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName()).Value;
        var beforeUpdate = DateTime.UtcNow;

        // Act
        profile.UpdateBasicInfo(
            CreatePersonName("Jane"),
            Bio.Empty,
            Location.Empty,
            ProfessionalRole.Empty,
            Institution.Empty,
            null);

        // Assert
        profile.ModifiedAt.Should().NotBeNull();
        profile.ModifiedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    #endregion

    #region UpdateResearchIdentifiers

    [Fact]
    public void UpdateResearchIdentifiers_ShouldUpdateIdentifiers()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName()).Value;
        var identifiers = ResearchIdentifiers.Create("0000-0002-1825-0097", "https://example.com").Value;

        // Act
        var result = profile.UpdateResearchIdentifiers(identifiers);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.ResearchIdentifiers.OrcidId.Should().Be("0000-0002-1825-0097");
        profile.ResearchIdentifiers.Website.Should().Be("https://example.com");
    }

    [Fact]
    public void UpdateResearchIdentifiers_ShouldRaiseProfileUpdatedEvent()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName()).Value;
        profile.ClearDomainEvents();

        var identifiers = ResearchIdentifiers.Create("0000-0002-1825-0097", null).Value;

        // Act
        profile.UpdateResearchIdentifiers(identifiers);

        // Assert
        var domainEvents = profile.DomainEvents;
        domainEvents.Should().ContainSingle();
        domainEvents.First().Should().BeOfType<ProfileUpdatedEvent>();
    }

    #endregion

    #region UpdatePhoto

    [Fact]
    public void UpdatePhoto_ShouldUpdatePhoto()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName()).Value;
        var photo = ProfilePhoto.Create("https://example.com/photo.jpg", "https://example.com/thumb.jpg", 1024).Value;

        // Act
        var result = profile.UpdatePhoto(photo);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Photo.Url.Should().Be("https://example.com/photo.jpg");
        profile.Photo.ThumbnailUrl.Should().Be("https://example.com/thumb.jpg");
        profile.Photo.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public void UpdatePhoto_ShouldRaiseProfilePhotoUpdatedEvent()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName()).Value;
        profile.ClearDomainEvents();

        var photo = ProfilePhoto.Create("https://example.com/photo.jpg").Value;

        // Act
        profile.UpdatePhoto(photo);

        // Assert
        var domainEvents = profile.DomainEvents;
        domainEvents.Should().ContainSingle();
        domainEvents.First().Should().BeOfType<ProfilePhotoUpdatedEvent>();

        var evt = (ProfilePhotoUpdatedEvent)domainEvents.First();
        evt.PhotoUrl.Should().Be("https://example.com/photo.jpg");
    }

    #endregion

    #region RemovePhoto

    [Fact]
    public void RemovePhoto_ShouldClearPhoto()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName()).Value;
        var photo = ProfilePhoto.Create("https://example.com/photo.jpg").Value;
        profile.UpdatePhoto(photo);

        // Act
        var result = profile.RemovePhoto();

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Photo.Url.Should().BeNull();
        profile.Photo.ThumbnailUrl.Should().BeNull();
        profile.Photo.HasPhoto.Should().BeFalse();
    }

    [Fact]
    public void RemovePhoto_ShouldRaiseProfilePhotoUpdatedEventWithNullUrl()
    {
        // Arrange
        var profile = Profile.Create(CreateProfileId(), CreateUserId(), CreatePersonName()).Value;
        profile.ClearDomainEvents();

        // Act
        profile.RemovePhoto();

        // Assert
        var domainEvents = profile.DomainEvents;
        domainEvents.Should().ContainSingle();
        domainEvents.First().Should().BeOfType<ProfilePhotoUpdatedEvent>();

        var evt = (ProfilePhotoUpdatedEvent)domainEvents.First();
        evt.PhotoUrl.Should().BeNull();
    }

    #endregion
}
