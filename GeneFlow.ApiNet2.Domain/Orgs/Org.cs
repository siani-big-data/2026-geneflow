using System.Text.RegularExpressions;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs.Entities;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.Domain.Orgs.Events;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Orgs;

/// <summary>
/// Organisation aggregate root. Represents a group of users that can
/// collectively own studies and collaborate. Members live inside the
/// aggregate and are mutated only through the root.
/// </summary>
public sealed class Org : FullAuditableAggregateRoot<OrgId>
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;
    public const int MaxLocationLength = 100;
    public const int MaxUrlLength = 500;

    /// <summary>
    /// Regex enforcing GitHub-style org handles: 2-39 chars, lowercase
    /// letters / digits / hyphens. Compiled for the lifetime of the
    /// process — a static field avoids re-parsing per call.
    /// </summary>
    public static readonly Regex HandleRegex =
        new("^[a-z0-9-]{2,39}$", RegexOptions.Compiled);

    public string Handle { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? Location { get; private set; }
    public OrgVisibility Visibility { get; private set; } = null!;

    private readonly List<OrgMember> _members = new();
    public IReadOnlyCollection<OrgMember> Members => _members.AsReadOnly();

    private Org() : base() { }

    private Org(OrgId id, string handle, string name, UserId creatorUserId) : base(id)
    {
        Handle = handle;
        Name = name;
        Visibility = OrgVisibility.Public;

        // Creator joins as the initial Owner so the invariant
        // "at least one Owner" holds from the very first second.
        _members.Add(OrgMember.Create(id, creatorUserId, OrgRole.Owner));

        InitializeCreatedAt(creatorUserId.ToString());
    }

    /// <summary>
    /// Creates a new organisation. Validates the handle and name, adds the
    /// creator as the initial <see cref="OrgRole.Owner"/>, and raises
    /// <see cref="OrgCreatedEvent"/>.
    /// </summary>
    public static Result<Org> Create(OrgId id, string handle, string name, UserId creatorUserId)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return Result.Failure<Org>(OrgErrors.HandleRequired);

        var normalizedHandle = handle.Trim().ToLowerInvariant();
        if (!HandleRegex.IsMatch(normalizedHandle))
            return Result.Failure<Org>(OrgErrors.HandleInvalid);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Org>(OrgErrors.NameRequired);

        var trimmedName = name.Trim();
        if (trimmedName.Length > MaxNameLength)
            return Result.Failure<Org>(OrgErrors.NameRequired);

        var org = new Org(id, normalizedHandle, trimmedName, creatorUserId);
        org.RaiseDomainEvent(new OrgCreatedEvent(id, normalizedHandle, trimmedName, creatorUserId));

        return org;
    }

    /// <summary>
    /// Updates the org's profile fields. Caller is responsible for verifying
    /// that the actor has permission (this is enforced in the application layer).
    /// </summary>
    public Result UpdateProfile(
        string name,
        string? description,
        string? avatarUrl,
        string? websiteUrl,
        string? location,
        UserId updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(OrgErrors.NameRequired);

        var trimmedName = name.Trim();
        if (trimmedName.Length > MaxNameLength)
            return Result.Failure(OrgErrors.NameRequired);

        Name = trimmedName;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        WebsiteUrl = string.IsNullOrWhiteSpace(websiteUrl) ? null : websiteUrl.Trim();
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();

        SetModified(updatedBy.ToString());
        RaiseDomainEvent(new OrgUpdatedEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Toggles the org between Public and Private visibility.
    /// </summary>
    public Result ChangeVisibility(OrgVisibility newVisibility, UserId changedBy)
    {
        Visibility = newVisibility;
        SetModified(changedBy.ToString());
        RaiseDomainEvent(new OrgUpdatedEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// Idempotent: silently returns success if the user is already a member.
    /// This makes invitation acceptance safe to retry.
    /// </summary>
    public Result AddMember(UserId userId, OrgRole role)
    {
        var existing = _members.FirstOrDefault(m => m.UserId.Equals(userId));
        if (existing is not null)
            return Result.Success();

        _members.Add(OrgMember.Create(Id, userId, role));
        return Result.Success();
    }

    /// <summary>
    /// Changes a member's role. Enforces the "at least one Owner" invariant:
    /// demoting the last remaining Owner fails.
    /// </summary>
    public Result ChangeMemberRole(UserId userId, OrgRole newRole, UserId actorUserId)
    {
        var member = _members.FirstOrDefault(m => m.UserId.Equals(userId));
        if (member is null)
            return Result.Failure(OrgErrors.MemberNotFound);

        // Demoting the last Owner would leave the org ownerless.
        if (member.Role == OrgRole.Owner && newRole != OrgRole.Owner)
        {
            var ownerCount = _members.Count(m => m.Role == OrgRole.Owner);
            if (ownerCount <= 1)
                return Result.Failure(OrgErrors.MustHaveOwner);
        }

        var oldRole = member.Role;
        member.ChangeRole(newRole);

        SetModified(actorUserId.ToString());
        RaiseDomainEvent(new OrgMemberRoleChangedEvent(Id, userId, oldRole, newRole, actorUserId));

        return Result.Success();
    }

    /// <summary>
    /// Removes a member. Enforces the "at least one Owner" invariant:
    /// removing the last remaining Owner fails.
    /// </summary>
    public Result RemoveMember(UserId userId, UserId actorUserId)
    {
        var member = _members.FirstOrDefault(m => m.UserId.Equals(userId));
        if (member is null)
            return Result.Failure(OrgErrors.MemberNotFound);

        if (member.Role == OrgRole.Owner)
        {
            var ownerCount = _members.Count(m => m.Role == OrgRole.Owner);
            if (ownerCount <= 1)
                return Result.Failure(OrgErrors.MustHaveOwner);
        }

        _members.Remove(member);

        SetModified(actorUserId.ToString());
        RaiseDomainEvent(new OrgMemberRemovedEvent(Id, userId, actorUserId));

        return Result.Success();
    }

    /// <summary>
    /// Convenience: returns the member entity for a user, or null.
    /// </summary>
    public OrgMember? GetMember(UserId userId) =>
        _members.FirstOrDefault(m => m.UserId.Equals(userId));

    /// <summary>
    /// True iff the user is a member of the org regardless of role.
    /// </summary>
    public bool IsMember(UserId userId) =>
        _members.Any(m => m.UserId.Equals(userId));
}
