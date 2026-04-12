using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies;

/// <summary>
/// Unit tests for the Study aggregate root.
/// </summary>
public class StudyTests
{
    private static StudyId CreateStudyId(long value = 1) => new(value);
    private static UserId CreateUserId(long value = 1) => new(value);
    private static StudyPaperId CreatePaperId(long value = 1) => new(value);
    private static StudyTitle CreateTitle(string value = "Test Study") => StudyTitle.Create(value).Value;
    private static StudyDescription CreateDescription(string? value = "Test description") =>
        StudyDescription.Create(value).Value;

    #region Create

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var studyId = CreateStudyId();
        var ownerId = CreateUserId();
        var title = CreateTitle("Genomic Analysis Study");
        var description = CreateDescription();

        // Act
        var result = Study.Create(studyId, ownerId, title, description, ResearchField.Genomics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(studyId);
        result.Value.OwnerId.Should().Be(ownerId);
        result.Value.Title.Should().Be(title);
        result.Value.Description.Should().Be(description);
        result.Value.ResearchField.Should().Be(ResearchField.Genomics);
    }

    [Fact]
    public void Create_ShouldInitializeDefaultValues()
    {
        // Arrange & Act
        var result = Study.Create(
            CreateStudyId(), CreateUserId(),
            CreateTitle(), CreateDescription(), ResearchField.Genomics);

        // Assert
        result.Value.Status.Should().Be(StudyStatus.Draft);
        result.Value.Settings.Should().Be(StudySettings.Default);
        result.Value.Metrics.Should().Be(StudyMetrics.Empty);
        result.Value.IsFeatured.Should().BeFalse();
        result.Value.Institution.Should().BeNull();
        result.Value.PrincipalInvestigator.Should().BeNull();
        result.Value.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldAddOwnerAsMember()
    {
        // Arrange
        var ownerId = CreateUserId(42);

        // Act
        var result = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics);

        // Assert
        result.Value.Members.Should().HaveCount(1);
        result.Value.Members.First().UserId.Should().Be(ownerId);
        result.Value.Members.First().Role.Should().Be(StudyRole.Owner);
    }

    [Fact]
    public void Create_ShouldRaiseStudyCreatedEvent()
    {
        // Act
        var result = Study.Create(
            CreateStudyId(), CreateUserId(),
            CreateTitle("My Study"), CreateDescription(), ResearchField.Genomics);

        // Assert
        result.Value.DomainEvents.Should().ContainSingle();
        result.Value.DomainEvents.First().Should().BeOfType<StudyCreatedEvent>();

        var evt = (StudyCreatedEvent)result.Value.DomainEvents.First();
        evt.Title.Should().Be("My Study");
        evt.ResearchField.Should().Be(ResearchField.Genomics);
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = Study.Create(
            CreateStudyId(), CreateUserId(),
            CreateTitle(), CreateDescription(), ResearchField.Genomics);

        // Assert
        result.Value.CreatedAt.Should().BeOnOrAfter(beforeCreate);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_ByOwner_ShouldUpdateFields()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle("Old Title"), CreateDescription("Old description"),
            ResearchField.Genomics).Value;
        study.ClearDomainEvents();

        var newTitle = CreateTitle("New Title");
        var newDescription = CreateDescription("New description");

        // Act
        var result = study.Update(newTitle, newDescription, ResearchField.Proteomics, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.Title.Should().Be(newTitle);
        study.Description.Should().Be(newDescription);
        study.ResearchField.Should().Be(ResearchField.Proteomics);
    }

    [Fact]
    public void Update_ShouldRaiseStudyUpdatedEvent()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.ClearDomainEvents();

        // Act
        study.Update(CreateTitle("New Title"), CreateDescription(), ResearchField.Genomics, ownerId);

        // Assert
        study.DomainEvents.Should().ContainSingle();
        study.DomainEvents.First().Should().BeOfType<StudyUpdatedEvent>();
    }

    [Fact]
    public void Update_ByViewer_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var viewerId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        // Act
        var result = study.Update(CreateTitle("New Title"), CreateDescription(), ResearchField.Genomics, viewerId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region ChangeStatus

    [Fact]
    public void ChangeStatus_ValidTransition_ShouldChangeStatus()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.ClearDomainEvents();

        // Act
        var result = study.ChangeStatus(StudyStatus.Active, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.Status.Should().Be(StudyStatus.Active);
    }

    [Fact]
    public void ChangeStatus_ShouldRaiseStudyStatusChangedEvent()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.ClearDomainEvents();

        // Act
        study.ChangeStatus(StudyStatus.Active, ownerId);

        // Assert
        study.DomainEvents.Should().ContainSingle();
        var evt = (StudyStatusChangedEvent)study.DomainEvents.First();
        evt.OldStatus.Should().Be(StudyStatus.Draft);
        evt.NewStatus.Should().Be(StudyStatus.Active);
    }

    [Fact]
    public void ChangeStatus_InvalidTransition_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Act - Draft -> Published is not valid
        var result = study.ChangeStatus(StudyStatus.Published, ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ChangeStatus_ByEditor_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var editorId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(editorId, StudyRole.Editor, ownerId);

        // Act
        var result = study.ChangeStatus(StudyStatus.Active, editorId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Member Management

    [Fact]
    public void AddMember_ByAdmin_ShouldAddMember()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var adminId = CreateUserId(2);
        var newUserId = CreateUserId(3);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(adminId, StudyRole.Admin, ownerId);
        study.ClearDomainEvents();

        // Act
        var result = study.AddMember(newUserId, StudyRole.Editor, adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.Members.Should().HaveCount(3);
        study.IsMember(newUserId).Should().BeTrue();
    }

    [Fact]
    public void AddMember_ShouldRaiseStudyMemberAddedEvent()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var newUserId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.ClearDomainEvents();

        // Act
        study.AddMember(newUserId, StudyRole.Editor, ownerId);

        // Assert
        study.DomainEvents.Should().ContainSingle();
        var evt = (StudyMemberAddedEvent)study.DomainEvents.First();
        evt.MemberUserId.Should().Be(newUserId);
        evt.Role.Should().Be(StudyRole.Editor);
    }

    [Fact]
    public void AddMember_AsOwner_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var newUserId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Act
        var result = study.AddMember(newUserId, StudyRole.Owner, ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddMember_AlreadyMember_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var userId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(userId, StudyRole.Editor, ownerId);

        // Act
        var result = study.AddMember(userId, StudyRole.Viewer, ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoveMember_ShouldRemoveMember()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var userId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(userId, StudyRole.Editor, ownerId);
        study.ClearDomainEvents();

        // Act
        var result = study.RemoveMember(userId, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(userId).Should().BeFalse();
    }

    [Fact]
    public void RemoveMember_Owner_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var adminId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        // Act
        var result = study.RemoveMember(ownerId, adminId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void LeaveStudy_ByNonOwner_ShouldSucceed()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var userId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(userId, StudyRole.Editor, ownerId);

        // Act
        var result = study.LeaveStudy(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(userId).Should().BeFalse();
    }

    [Fact]
    public void LeaveStudy_ByOwner_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Act
        var result = study.LeaveStudy(ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ChangeMemberRole_ShouldChangeRole()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var userId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(userId, StudyRole.Editor, ownerId);
        study.ClearDomainEvents();

        // Act
        var result = study.ChangeMemberRole(userId, StudyRole.Admin, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.GetMember(userId)!.Role.Should().Be(StudyRole.Admin);
    }

    [Fact]
    public void TransferOwnership_ShouldTransferToAdmin()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var adminId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(adminId, StudyRole.Admin, ownerId);
        study.ClearDomainEvents();

        // Act
        var result = study.TransferOwnership(adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.OwnerId.Should().Be(adminId);
        study.GetMember(adminId)!.Role.Should().Be(StudyRole.Owner);
        study.GetMember(ownerId)!.Role.Should().Be(StudyRole.Admin);
    }

    [Fact]
    public void TransferOwnership_ShouldRaiseStudyOwnershipTransferredEvent()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var adminId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(adminId, StudyRole.Admin, ownerId);
        study.ClearDomainEvents();

        // Act
        study.TransferOwnership(adminId);

        // Assert
        study.DomainEvents.Should().ContainSingle();
        var evt = (StudyOwnershipTransferredEvent)study.DomainEvents.First();
        evt.PreviousOwnerId.Should().Be(ownerId);
        evt.NewOwnerId.Should().Be(adminId);
    }

    [Fact]
    public void TransferOwnership_ToEditor_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var editorId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(editorId, StudyRole.Editor, ownerId);

        // Act
        var result = study.TransferOwnership(editorId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Tag Management

    [Fact]
    public void AddTag_ShouldAddTag()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Act
        var result = study.AddTag("genomics", ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.Tags.Should().Contain("genomics");
    }

    [Fact]
    public void AddTag_ShouldNormalizeToLowerCase()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Act
        study.AddTag("GENOMICS", ownerId);

        // Assert
        study.Tags.Should().Contain("genomics");
        study.Tags.Should().NotContain("GENOMICS");
    }

    [Fact]
    public void AddTag_Duplicate_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddTag("genomics", ownerId);

        // Act
        var result = study.AddTag("genomics", ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddTag_ExceedingMaxTags_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Add max tags
        for (int i = 0; i < Study.MaxTags; i++)
        {
            study.AddTag($"tag{i}", ownerId);
        }

        // Act
        var result = study.AddTag("onetoomany", ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoveTag_ShouldRemoveTag()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddTag("genomics", ownerId);

        // Act
        var result = study.RemoveTag("genomics", ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.Tags.Should().BeEmpty();
    }

    [Fact]
    public void SetTags_ShouldReplaceAllTags()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddTag("old1", ownerId);
        study.AddTag("old2", ownerId);

        // Act
        var result = study.SetTags(new[] { "new1", "new2", "new3" }, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.Tags.Should().HaveCount(3);
        study.Tags.Should().Contain(new[] { "new1", "new2", "new3" });
        study.Tags.Should().NotContain(new[] { "old1", "old2" });
    }

    #endregion

    #region Featured Management

    [Fact]
    public void FeatuREDACTED()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.ClearDomainEvents();

        // Act
        var result = study.Feature(ownerId, isAdmin: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void FeatuREDACTED()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.ClearDomainEvents();

        // Act
        study.Feature(ownerId, isAdmin: true);

        // Assert
        study.DomainEvents.Should().ContainSingle();
        study.DomainEvents.First().Should().BeOfType<StudyFeaturedEvent>();
    }

    [Fact]
    public void FeatuREDACTED()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Act
        var result = study.Feature(ownerId, isAdmin: false);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void FeatuREDACTED()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.Feature(ownerId, isAdmin: true);

        // Act
        var result = study.Feature(ownerId, isAdmin: true);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UnfeatuREDACTED()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.Feature(ownerId, isAdmin: true);
        study.ClearDomainEvents();

        // Act
        var result = study.Unfeature(ownerId, isAdmin: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsFeatured.Should().BeFalse();
    }

    #endregion

    #region Metrics Management

    [Fact]
    public void IncrementViews_ShouldIncreaseViewCount()
    {
        // Arrange
        var study = Study.Create(
            CreateStudyId(), CreateUserId(),
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Act
        study.IncrementViews();

        // Assert
        study.Metrics.ViewsCount.Should().Be(1);
    }

    [Fact]
    public void IncrementStars_ShouldIncreaseStarCount()
    {
        // Arrange
        var study = Study.Create(
            CreateStudyId(), CreateUserId(),
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Act
        study.IncrementStars();

        // Assert
        study.Metrics.StarsCount.Should().Be(1);
    }

    [Fact]
    public void DecrementStars_ShouldDecreaseStarCount()
    {
        // Arrange
        var study = Study.Create(
            CreateStudyId(), CreateUserId(),
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.IncrementStars();
        study.IncrementStars();

        // Act
        study.DecrementStars();

        // Assert
        study.Metrics.StarsCount.Should().Be(1);
    }

    #endregion

    #region Computed Properties

    [Fact]
    public void IsPublic_WhenPublished_ShouldReturnTrue()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.ChangeStatus(StudyStatus.Active, ownerId);
        study.ChangeStatus(StudyStatus.Completed, ownerId);
        study.ChangeStatus(StudyStatus.Published, ownerId);

        // Assert
        study.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void IsPublic_WhenDraft_ShouldReturnFalse()
    {
        // Arrange
        var study = Study.Create(
            CreateStudyId(), CreateUserId(),
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Assert
        study.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void MemberCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(CreateUserId(2), StudyRole.Editor, ownerId);
        study.AddMember(CreateUserId(3), StudyRole.Viewer, ownerId);

        // Assert
        study.MemberCount.Should().Be(3);
    }

    [Fact]
    public void TagCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var ownerId = CreateUserId();
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddTag("tag1", ownerId);
        study.AddTag("tag2", ownerId);

        // Assert
        study.TagCount.Should().Be(2);
    }

    [Fact]
    public void CanUserView_Member_ShouldReturnTrue()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var viewerId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        // Assert
        study.CanUserView(viewerId).Should().BeTrue();
    }

    [Fact]
    public void CanUserView_NonMemberOnDraftStudy_ShouldReturnFalse()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var nonMemberId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;

        // Assert
        study.CanUserView(nonMemberId).Should().BeFalse();
    }

    [Fact]
    public void CanUserEdit_Editor_ShouldReturnTrue()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var editorId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(editorId, StudyRole.Editor, ownerId);

        // Assert
        study.CanUserEdit(editorId).Should().BeTrue();
    }

    [Fact]
    public void CanUserEdit_Viewer_ShouldReturnFalse()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var viewerId = CreateUserId(2);
        var study = Study.Create(
            CreateStudyId(), ownerId,
            CreateTitle(), CreateDescription(), ResearchField.Genomics).Value;
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        // Assert
        study.CanUserEdit(viewerId).Should().BeFalse();
    }

    #endregion
}
