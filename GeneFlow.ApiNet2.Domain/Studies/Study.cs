using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Studies;

/// <summary>
/// Study aggregate root representing a research study/project.
/// </summary>
public sealed class Study : FullAuditableAggregateRoot<StudyId>
{
    #region Constants
    public const int MaxTags = 10;
    public const int MaxTagLength = 50;
    public const int MaxInstitutionLength = 200;
    public const int MaxPrincipalInvestigatorLength = 200;
    public const int MaxPapers = 50;
    public const int MaxReadmeLength = 100_000;
    #endregion

    #region Properties
    public UserId OwnerId { get; private set; } = null!;

    /// <summary>
    /// Whether this study's principal owner is a User or an Org. Defaults to
    /// <see cref="StudyOwnerType.User"/>. When set to <see cref="StudyOwnerType.Org"/>
    /// the <c>OwnerId</c> string-form identifies an Org rather than a User.
    /// </summary>
    public StudyOwnerType OwnerType { get; private set; } = StudyOwnerType.User;

    public StudyTitle Title { get; private set; } = null!;
    public StudyDescription Description { get; private set; } = null!;
    public ResearchField ResearchField { get; private set; } = null!;
    public StudyStatus Status { get; private set; } = null!;
    public StudySettings Settings { get; private set; } = null!;
    public StudyMetrics Metrics { get; private set; } = null!;

    public string? Institution { get; private set; }
    public string? PrincipalInvestigator { get; private set; }
    public string? ReadmeMarkdown { get; private set; }
    public bool IsFeatured { get; private set; }

    private readonly List<StudyMember> _members = new();
    public IReadOnlyList<StudyMember> Members => _members.AsReadOnly();

    private readonly List<StudyPaper> _papers = new();
    public IReadOnlyList<StudyPaper> Papers => _papers.AsReadOnly();

    private readonly List<string> _tags = new();
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();
    #endregion

    #region Computed Properties
    public bool IsPublic => Status.IsPubliclyVisible;
    public int MemberCount => _members.Count;
    public int PaperCount => _papers.Count(p => !p.IsDeleted);
    public int TagCount => _tags.Count;
    #endregion

    #region Constructors
    private Study() : base() { }

    private Study(
        StudyId id,
        UserId ownerId,
        StudyTitle title,
        StudyDescription description,
        ResearchField researchField) : base(id)
    {
        OwnerId = ownerId;
        OwnerType = StudyOwnerType.User;
        Title = title;
        Description = description;
        ResearchField = researchField;
        Status = StudyStatus.Draft;
        Settings = StudySettings.Default;
        Metrics = StudyMetrics.Empty;
        IsFeatured = false;

        // Add owner as first member
        _members.Add(StudyMember.Create(ownerId, StudyRole.Owner));

        InitializeCreatedAt(ownerId.Value.ToString());
    }
    #endregion

    #region Factory Methods
    public static Result<Study> Create(
        StudyId id,
        UserId ownerId,
        StudyTitle title,
        StudyDescription description,
        ResearchField researchField)
    {
        var study = new Study(id, ownerId, title, description, researchField);

        study.RaiseDomainEvent(new StudyCreatedEvent(
            study.Id,
            study.Title.Value,
            study.OwnerId,
            study.ResearchField));

        return study;
    }
    #endregion

    #region Basic Info Management
    public Result Update(
        StudyTitle title,
        StudyDescription description,
        ResearchField researchField,
        UserId updatedBy)
    {
        if (!CanUserEdit(updatedBy))
            return Result.Failure(StudyErrors.InsufficientPermissions);

        Title = title;
        Description = description;
        ResearchField = researchField;

        SetModified(updatedBy.Value.ToString());
        RaiseDomainEvent(new StudyUpdatedEvent(Id, updatedBy));

        return Result.Success();
    }

    public Result UpdateInstitution(string? institution, UserId updatedBy)
    {
        if (!CanUserEdit(updatedBy))
            return Result.Failure(StudyErrors.InsufficientPermissions);

        if (institution?.Length > MaxInstitutionLength)
            return Result.Failure(StudyErrors.InstitutionTooLong(MaxInstitutionLength));

        Institution = institution?.Trim();
        SetModified(updatedBy.Value.ToString());

        return Result.Success();
    }

    public Result UpdatePrincipalInvestigator(string? pi, UserId updatedBy)
    {
        if (!CanUserEdit(updatedBy))
            return Result.Failure(StudyErrors.InsufficientPermissions);

        if (pi?.Length > MaxPrincipalInvestigatorLength)
            return Result.Failure(StudyErrors.PrincipalInvestigatorTooLong(MaxPrincipalInvestigatorLength));

        PrincipalInvestigator = pi?.Trim();
        SetModified(updatedBy.Value.ToString());

        return Result.Success();
    }

    public Result UpdateReadme(string? markdown, UserId updatedBy)
    {
        if (!CanUserEdit(updatedBy))
            return Result.Failure(StudyErrors.InsufficientPermissions);

        if (markdown?.Length > MaxReadmeLength)
            return Result.Failure(StudyErrors.ReadmeTooLong(MaxReadmeLength));

        // Normalize empty to null so DB stores NULL rather than empty string.
        ReadmeMarkdown = string.IsNullOrWhiteSpace(markdown) ? null : markdown;
        SetModified(updatedBy.Value.ToString());

        RaiseDomainEvent(new StudyReadmeUpdatedEvent(Id, updatedBy));

        return Result.Success();
    }
    #endregion

    #region Status Management
    public Result ChangeStatus(StudyStatus newStatus, UserId changedBy)
    {
        var member = GetMember(changedBy);
        if (member is null || !member.Role.CanChangeStatus)
            return Result.Failure(StudyErrors.InsufficientPermissions);

        if (!Status.CanTransitionTo(newStatus))
            return Result.Failure(StudyErrors.InvalidStatusTransition(Status, newStatus));

        var oldStatus = Status;
        Status = newStatus;

        SetModified(changedBy.Value.ToString());
        RaiseDomainEvent(new StudyStatusChangedEvent(Id, oldStatus, newStatus, changedBy));

        return Result.Success();
    }

    public Result UpdateSettings(StudySettings settings, UserId updatedBy)
    {
        var member = GetMember(updatedBy);
        if (member is null || !member.Role.CanChangeStatus)
            return Result.Failure(StudyErrors.InsufficientPermissions);

        Settings = settings;
        SetModified(updatedBy.Value.ToString());

        return Result.Success();
    }
    #endregion

    #region Member Management
    public Result AddMember(UserId userId, StudyRole role, UserId addedBy)
    {
        var adder = GetMember(addedBy);
        if (adder is null || !adder.Role.CanManageMembers)
            return Result.Failure(StudyErrors.InsufficientPermissions);

        if (role == StudyRole.Owner)
            return Result.Failure(StudyErrors.CannotChangeOwnerRole);

        if (_members.Any(m => m.UserId.Equals(userId)))
            return Result.Failure(StudyErrors.UserAlreadyMember);

        var member = StudyMember.Create(userId, role, addedBy);
        _members.Add(member);

        RaiseDomainEvent(new StudyMemberAddedEvent(Id, userId, role, addedBy));

        return Result.Success();
    }

    public Result RemoveMember(UserId userId, UserId removedBy)
    {
        var remover = GetMember(removedBy);
        if (remover is null || !remover.Role.CanManageMembers)
            return Result.Failure(StudyErrors.InsufficientPermissions);

        var member = GetMember(userId);
        if (member is null)
            return Result.Failure(StudyErrors.UserNotMember);

        if (member.Role == StudyRole.Owner)
            return Result.Failure(StudyErrors.CannotRemoveOwner);

        _members.Remove(member);
        RaiseDomainEvent(new StudyMemberRemovedEvent(Id, userId, removedBy));

        return Result.Success();
    }

    public Result LeaveStudy(UserId userId)
    {
        var member = GetMember(userId);
        if (member is null)
            return Result.Failure(StudyErrors.UserNotMember);

        if (member.Role == StudyRole.Owner)
            return Result.Failure(StudyErrors.OwnerCannotLeave);

        _members.Remove(member);
        RaiseDomainEvent(new StudyMemberRemovedEvent(Id, userId, userId));

        return Result.Success();
    }

    public Result ChangeMemberRole(UserId userId, StudyRole newRole, UserId changedBy)
    {
        var changer = GetMember(changedBy);
        if (changer is null || !changer.Role.CanManageMembers)
            return Result.Failure(StudyErrors.InsufficientPermissions);

        var member = GetMember(userId);
        if (member is null)
            return Result.Failure(StudyErrors.UserNotMember);

        if (member.Role == StudyRole.Owner)
            return Result.Failure(StudyErrors.CannotChangeOwnerRole);

        if (newRole == StudyRole.Owner)
            return Result.Failure(StudyErrors.CannotChangeOwnerRole);

        var oldRole = member.Role;
        member.ChangeRole(newRole);

        RaiseDomainEvent(new StudyMemberRoleChangedEvent(Id, userId, oldRole, newRole, changedBy));

        return Result.Success();
    }

    public Result TransferOwnership(UserId newOwnerId)
    {
        var newOwner = GetMember(newOwnerId);
        if (newOwner is null)
            return Result.Failure(StudyErrors.UserNotMember);

        if (newOwner.Role != StudyRole.Admin)
            return Result.Failure(StudyErrors.NewOwnerMustBeAdmin);

        var currentOwner = _members.First(m => m.Role == StudyRole.Owner);
        var previousOwnerId = currentOwner.UserId;

        currentOwner.ChangeRole(StudyRole.Admin);
        newOwner.ChangeRole(StudyRole.Owner);
        OwnerId = newOwnerId;

        RaiseDomainEvent(new StudyOwnershipTransferredEvent(Id, previousOwnerId, newOwnerId));

        return Result.Success();
    }

    /// <summary>
    /// Cross-aggregate transfer: changes the study's principal owner from a
    /// User to an Org (or vice versa). The string form of the new owner id
    /// is stored in <see cref="OwnerId"/>; the application layer is
    /// responsible for verifying that the actor is allowed to do this and
    /// that the new owner exists.
    ///
    /// Raises <see cref="Orgs.Events.StudyOwnershipTransferredEvent"/> so
    /// projectors (search index, activity feed) can update.
    /// </summary>
    public Result TransferOwnership(StudyOwnerType newOwnerType, string newOwnerId)
    {
        if (string.IsNullOrWhiteSpace(newOwnerId))
            return Result.Failure(Error.Validation(
                "Study.OwnerIdRequired",
                "New owner ID is required."));

        var previousOwnerType = OwnerType;
        var previousOwnerId = OwnerId.ToString();

        OwnerType = newOwnerType;

        // OwnerId is typed as UserId. When transferring to an Org we still
        // need a valid UserId-shaped value here, so we re-parse using the
        // numeric portion. The OwnerType discriminator is the source of
        // truth for "what kind of id is this".
        if (newOwnerType == StudyOwnerType.User)
        {
            OwnerId = UserId.Parse(newOwnerId);
        }
        else
        {
            // For Org-owned studies, OwnerId carries the org's numeric id
            // but the prefix is still 'U' so EF round-trips cleanly. The
            // string form for downstream consumers (events, DTOs) uses
            // newOwnerId directly via the event.
            var numeric = ExtractNumericId(newOwnerId);
            OwnerId = new UserId(numeric);
        }

        RaiseDomainEvent(new Orgs.Events.StudyOwnershipTransferredEvent(
            Id,
            previousOwnerType,
            previousOwnerId,
            newOwnerType,
            newOwnerId));

        SetModified();
        return Result.Success();
    }

    private static long ExtractNumericId(string prefixedId)
    {
        // Accept either a prefixed id (e.g. "O00000007") or a bare number.
        if (string.IsNullOrEmpty(prefixedId))
            return 0;

        var span = char.IsLetter(prefixedId[0]) ? prefixedId.AsSpan(1) : prefixedId.AsSpan();
        return long.TryParse(span, out var v) ? v : 0;
    }

    public StudyMember? GetMember(UserId userId) =>
        _members.FirstOrDefault(m => m.UserId.Equals(userId));

    public bool IsMember(UserId userId) =>
        _members.Any(m => m.UserId.Equals(userId));

    public bool CanUserView(UserId userId) =>
        IsMember(userId) || IsPublic;

    public bool CanUserEdit(UserId userId)
    {
        var member = GetMember(userId);
        return member?.Role.CanEditStudy ?? false;
    }
    #endregion

    #region Tag Management
    public Result AddTag(string tag, UserId addedBy)
    {
        if (!CanUserEdit(addedBy))
            return Result.Failure(StudyErrors.InsufficientPermissions);

        if (string.IsNullOrWhiteSpace(tag))
            return Result.Failure(Error.Validation("Study.TagRequired", "Tag is required."));

        var normalizedTag = tag.Trim().ToLowerInvariant();

        if (normalizedTag.Length > MaxTagLength)
            return Result.Failure(StudyErrors.TagTooLong(MaxTagLength));

        if (_tags.Count >= MaxTags)
            return Result.Failure(StudyErrors.MaxTagsReached(MaxTags));

        if (_tags.Contains(normalizedTag))
            return Result.Failure(StudyErrors.TagAlreadyExists);

        _tags.Add(normalizedTag);
        SetModified();

        return Result.Success();
    }

    public Result RemoveTag(string tag, UserId removedBy)
    {
        if (!CanUserEdit(removedBy))
            return Result.Failure(StudyErrors.InsufficientPermissions);

        var normalizedTag = tag.Trim().ToLowerInvariant();

        if (!_tags.Remove(normalizedTag))
            return Result.Failure(StudyErrors.TagNotFound);

        SetModified();

        return Result.Success();
    }

    public Result SetTags(IEnumerable<string> tags, UserId setBy)
    {
        if (!CanUserEdit(setBy))
            return Result.Failure(StudyErrors.InsufficientPermissions);

        var normalizedTags = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (normalizedTags.Count > MaxTags)
            return Result.Failure(StudyErrors.MaxTagsReached(MaxTags));

        var tooLong = normalizedTags.FirstOrDefault(t => t.Length > MaxTagLength);
        if (tooLong is not null)
            return Result.Failure(StudyErrors.TagTooLong(MaxTagLength));

        _tags.Clear();
        _tags.AddRange(normalizedTags);
        SetModified();

        return Result.Success();
    }
    #endregion

    #region Paper Management
    public Result AddPaper(StudyPaper paper, UserId addedBy)
    {
        if (!CanUserEdit(addedBy))
            return Result.Failure(StudyErrors.InsufficientPermissions);

        if (_papers.Count(p => !p.IsDeleted) >= MaxPapers)
            return Result.Failure(StudyErrors.MaxPapersReached);

        _papers.Add(paper);

        RaiseDomainEvent(new StudyPaperAddedEvent(Id, paper.Id, paper.Title, paper.Doi, addedBy));

        return Result.Success();
    }

    public Result RemovePaper(StudyPaperId paperId, UserId removedBy)
    {
        if (!CanUserEdit(removedBy))
            return Result.Failure(StudyErrors.InsufficientPermissions);

        var paper = _papers.FirstOrDefault(p => p.Id.Equals(paperId) && !p.IsDeleted);
        if (paper is null)
            return Result.Failure(StudyErrors.PaperNotFound);

        paper.SoftDelete(DateTime.UtcNow, removedBy.Value.ToString());

        RaiseDomainEvent(new StudyPaperRemovedEvent(Id, paperId, removedBy));

        return Result.Success();
    }
    #endregion

    #region Featured Management
    public Result Feature(UserId featuredBy, bool isAdmin)
    {
        if (!isAdmin)
            return Result.Failure(StudyErrors.OnlyAdminCanFeature);

        if (IsFeatured)
            return Result.Failure(StudyErrors.AlreadyFeatured);

        IsFeatured = true;
        SetModified();

        RaiseDomainEvent(new StudyFeaturedEvent(Id, featuredBy));

        return Result.Success();
    }

    public Result Unfeature(UserId unfeaturedBy, bool isAdmin)
    {
        if (!isAdmin)
            return Result.Failure(StudyErrors.OnlyAdminCanFeature);

        if (!IsFeatured)
            return Result.Failure(StudyErrors.NotFeatured);

        IsFeatured = false;
        SetModified();

        RaiseDomainEvent(new StudyUnfeaturedEvent(Id, unfeaturedBy));

        return Result.Success();
    }
    #endregion

    #region Metrics Management
    public void IncrementViews()
    {
        Metrics = Metrics.IncrementViews();
    }

    public void IncrementStars()
    {
        Metrics = Metrics.IncrementStars();
    }

    public void DecrementStars()
    {
        Metrics = Metrics.DecrementStars();
    }
    #endregion
}
