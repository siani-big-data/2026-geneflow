using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles.Enumerations;
using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles;

/// <summary>
/// Profile aggregate root.
/// </summary>
public sealed class Profile : FullAuditableAggregateRoot<ProfileId>
{
    /// <summary>Gets the user ID this profile belongs to.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Gets the person's name.</summary>
    public PersonName Name { get; private set; } = null!;

    /// <summary>Gets the profile bio.</summary>
    public Bio Bio { get; private set; } = null!;

    /// <summary>Gets the location.</summary>
    public Location Location { get; private set; } = null!;

    /// <summary>Gets the professional role.</summary>
    public ProfessionalRole ProfessionalRole { get; private set; } = null!;

    /// <summary>Gets the institution information.</summary>
    public Institution Institution { get; private set; } = null!;

    /// <summary>Gets the research field.</summary>
    public ResearchField? ResearchField { get; private set; }

    /// <summary>Gets the research identifiers (ORCID, Website).</summary>
    public ResearchIdentifiers ResearchIdentifiers { get; private set; } = null!;

    /// <summary>Gets the profile photo.</summary>
    public ProfilePhoto Photo { get; private set; } = null!;

    /// <summary>Gets whether the profile is complete (has required fields filled).</summary>
    public bool IsComplete => !string.IsNullOrWhiteSpace(Name.FirstName);

    /// <summary>Gets the full name.</summary>
    public string FullName => Name.FullName;

    /// <summary>Gets the initials.</summary>
    public string Initials => Name.Initials;

    private Profile() : base() { }

    private Profile(
        ProfileId id,
        UserId userId,
        PersonName name) : base(id)
    {
        UserId = userId;
        Name = name;
        Bio = Bio.Empty;
        Location = Location.Empty;
        ProfessionalRole = ProfessionalRole.Empty;
        Institution = Institution.Empty;
        ResearchField = null;
        ResearchIdentifiers = ResearchIdentifiers.Empty;
        Photo = ProfilePhoto.Empty;

        InitializeCreatedAt();
    }

    /// <summary>
    /// Creates a new profile.
    /// </summary>
    public static Result<Profile> Create(
        ProfileId id,
        UserId userId,
        PersonName name)
    {
        var profile = new Profile(id, userId, name);

        profile.RaiseDomainEvent(new ProfileCreatedEvent(
            profile.Id,
            profile.UserId,
            profile.Name.FirstName));

        return profile;
    }

    /// <summary>
    /// Updates the basic profile information.
    /// </summary>
    public Result UpdateBasicInfo(
        PersonName name,
        Bio bio,
        Location location,
        ProfessionalRole professionalRole,
        Institution institution,
        ResearchField? researchField)
    {
        Name = name;
        Bio = bio;
        Location = location;
        ProfessionalRole = professionalRole;
        Institution = institution;
        ResearchField = researchField;

        SetModified();
        RaiseDomainEvent(new ProfileUpdatedEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Updates the research identifiers (ORCID and Website).
    /// </summary>
    public Result UpdateResearchIdentifiers(ResearchIdentifiers identifiers)
    {
        ResearchIdentifiers = identifiers;

        SetModified();
        RaiseDomainEvent(new ProfileUpdatedEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Updates the profile photo.
    /// </summary>
    public Result UpdatePhoto(ProfilePhoto photo)
    {
        Photo = photo;

        SetModified();
        RaiseDomainEvent(new ProfilePhotoUpdatedEvent(Id, photo.Url));

        return Result.Success();
    }

    /// <summary>
    /// Removes the profile photo.
    /// </summary>
    public Result RemovePhoto()
    {
        Photo = ProfilePhoto.Empty;

        SetModified();
        RaiseDomainEvent(new ProfilePhotoUpdatedEvent(Id, null));

        return Result.Success();
    }
}
