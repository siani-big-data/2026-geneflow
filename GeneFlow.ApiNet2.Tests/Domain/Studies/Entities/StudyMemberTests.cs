using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.Entities;

/// <summary>
/// Unit tests for StudyMember entity through Study aggregate.
/// Note: StudyMember is internal and created through Study.
/// </summary>
public class StudyMemberTests
{
    private static StudyId CreateStudyId(long value = 1) => new(value);
    private static UserId CreateUserId(long value = 1) => new(value);
    private static StudyTitle CreateTitle(string value = "Test Study") => StudyTitle.Create(value).Value;
    private static StudyDescription CreateDescription() => StudyDescription.Create("Test description").Value;

    private Study CreateStudy(UserId? ownerId = null)
    {
        return Study.Create(
            CreateStudyId(),
            ownerId ?? CreateUserId(),
            CreateTitle(),
            CreateDescription(),
            ResearchField.Genomics).Value;
    }

    #region Owner Member

    [Fact]
    public void NewStudy_ShouldHaveOwnerAsMember()
    {
        // Arrange
        var ownerId = CreateUserId(42);

        // Act
        var study = CreateStudy(ownerId);

        // Assert
        study.Members.Should().HaveCount(1);
        var ownerMember = study.Members.First();
        ownerMember.UserId.Should().Be(ownerId);
        ownerMember.Role.Should().Be(StudyRole.Owner);
    }

    [Fact]
    public void OwnerMember_ShouldHaveJoinedAtSet()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var study = CreateStudy();
        var ownerMember = study.Members.First();

        // Assert
        ownerMember.JoinedAt.Should().BeOnOrAfter(beforeCreate);
    }

    [Fact]
    public void OwnerMember_ShouldHaveNoInvitedBy()
    {
        // Act
        var study = CreateStudy();
        var ownerMember = study.Members.First();

        // Assert
        ownerMember.InvitedBy.Should().BeNull();
    }

    #endregion

    #region Adding Members

    [Fact]
    public void AddMember_ShouldCreateMemberWithCorrectRole()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var newUserId = CreateUserId(2);
        var study = CreateStudy(ownerId);

        // Act
        study.AddMember(newUserId, StudyRole.Editor, ownerId);

        // Assert
        var member = study.GetMember(newUserId);
        member.Should().NotBeNull();
        member!.UserId.Should().Be(newUserId);
        member.Role.Should().Be(StudyRole.Editor);
    }

    [Fact]
    public void AddMember_ShouldSetInvitedBy()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var newUserId = CreateUserId(2);
        var study = CreateStudy(ownerId);

        // Act
        study.AddMember(newUserId, StudyRole.Viewer, ownerId);

        // Assert
        var member = study.GetMember(newUserId);
        member!.InvitedBy.Should().Be(ownerId);
    }

    [Fact]
    public void AddMember_ShouldSetJoinedAt()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var newUserId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        var beforeAdd = DateTime.UtcNow;

        // Act
        study.AddMember(newUserId, StudyRole.Editor, ownerId);

        // Assert
        var member = study.GetMember(newUserId);
        member!.JoinedAt.Should().BeOnOrAfter(beforeAdd);
    }

    [Theory]
    [InlineData("Editor")]
    [InlineData("Admin")]
    [InlineData("Viewer")]
    public void AddMember_WithDifferentRoles_ShouldSetCorrectRole(string roleName)
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var newUserId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        var role = StudyRole.FromName(roleName)!;

        // Act
        study.AddMember(newUserId, role, ownerId);

        // Assert
        var member = study.GetMember(newUserId);
        member!.Role.Should().Be(role);
    }

    #endregion

    #region Role Changes

    [Fact]
    public void ChangeMemberRole_ShouldUpdateRole()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var userId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        study.AddMember(userId, StudyRole.Viewer, ownerId);

        // Act
        study.ChangeMemberRole(userId, StudyRole.Editor, ownerId);

        // Assert
        var member = study.GetMember(userId);
        member!.Role.Should().Be(StudyRole.Editor);
    }

    [Fact]
    public void ChangeMemberRole_FromViewerToAdmin_ShouldWork()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var userId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        study.AddMember(userId, StudyRole.Viewer, ownerId);

        // Act
        var result = study.ChangeMemberRole(userId, StudyRole.Admin, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.GetMember(userId)!.Role.Should().Be(StudyRole.Admin);
    }

    [Fact]
    public void ChangeMemberRole_ToOwner_ShouldFail()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var userId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        study.AddMember(userId, StudyRole.Admin, ownerId);

        // Act
        var result = study.ChangeMemberRole(userId, StudyRole.Owner, ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ChangeMemberRole_OfOwner_ShouldFail()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var adminId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        // Act
        var result = study.ChangeMemberRole(ownerId, StudyRole.Admin, adminId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Member Removal

    [Fact]
    public void RemoveMember_ShouldRemoveFromList()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var userId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        study.AddMember(userId, StudyRole.Editor, ownerId);

        // Act
        study.RemoveMember(userId, ownerId);

        // Assert
        study.IsMember(userId).Should().BeFalse();
        study.GetMember(userId).Should().BeNull();
    }

    [Fact]
    public void RemoveMember_Owner_ShouldFail()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var adminId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        // Act
        var result = study.RemoveMember(ownerId, adminId);

        // Assert
        result.IsFailure.Should().BeTrue();
        study.IsMember(ownerId).Should().BeTrue();
    }

    #endregion

    #region TransferOwnership Effect on Members

    [Fact]
    public void TransferOwnership_ShouldChangeRolesCorrectly()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var adminId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        // Act
        study.TransferOwnership(adminId);

        // Assert
        study.GetMember(ownerId)!.Role.Should().Be(StudyRole.Admin);
        study.GetMember(adminId)!.Role.Should().Be(StudyRole.Owner);
    }

    [Fact]
    public void TransferOwnership_ShouldMaintainMemberCount()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var adminId = CreateUserId(2);
        var editorId = CreateUserId(3);
        var study = CreateStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);
        study.AddMember(editorId, StudyRole.Editor, ownerId);

        // Act
        study.TransferOwnership(adminId);

        // Assert
        study.Members.Should().HaveCount(3);
        study.IsMember(ownerId).Should().BeTrue();
        study.IsMember(adminId).Should().BeTrue();
        study.IsMember(editorId).Should().BeTrue();
    }

    #endregion

    #region IsMember and GetMember

    [Fact]
    public void IsMember_ForMember_ShouldReturnTrue()
    {
        // Arrange
        var study = CreateStudy(CreateUserId(1));

        // Assert
        study.IsMember(CreateUserId(1)).Should().BeTrue();
    }

    [Fact]
    public void IsMember_ForNonMember_ShouldReturnFalse()
    {
        // Arrange
        var study = CreateStudy(CreateUserId(1));

        // Assert
        study.IsMember(CreateUserId(99)).Should().BeFalse();
    }

    [Fact]
    public void GetMember_ForMember_ShouldReturnMember()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var study = CreateStudy(ownerId);

        // Act
        var member = study.GetMember(ownerId);

        // Assert
        member.Should().NotBeNull();
        member!.UserId.Should().Be(ownerId);
    }

    [Fact]
    public void GetMember_ForNonMember_ShouldReturnNull()
    {
        // Arrange
        var study = CreateStudy(CreateUserId(1));

        // Act
        var member = study.GetMember(CreateUserId(99));

        // Assert
        member.Should().BeNull();
    }

    #endregion
}
