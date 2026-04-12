using GeneFlow.ApiNet2.Domain.Studies.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.Enumerations;

/// <summary>
/// Unit tests for StudyRole smart enumeration.
/// </summary>
public class StudyRoleTests
{
    #region Static Values

    [Fact]
    public void Owner_ShouldHaveCorrectValues()
    {
        // Assert
        StudyRole.Owner.Id.Should().Be(1);
        StudyRole.Owner.Name.Should().Be("Owner");
        StudyRole.Owner.DisplayName.Should().Be("Owner");
        StudyRole.Owner.PermissionLevel.Should().Be(100);
    }

    [Fact]
    public void Admin_ShouldHaveCorrectValues()
    {
        // Assert
        StudyRole.Admin.Id.Should().Be(2);
        StudyRole.Admin.Name.Should().Be("Admin");
        StudyRole.Admin.DisplayName.Should().Be("Administrator");
        StudyRole.Admin.PermissionLevel.Should().Be(80);
    }

    [Fact]
    public void Editor_ShouldHaveCorrectValues()
    {
        // Assert
        StudyRole.Editor.Id.Should().Be(3);
        StudyRole.Editor.Name.Should().Be("Editor");
        StudyRole.Editor.DisplayName.Should().Be("Editor");
        StudyRole.Editor.PermissionLevel.Should().Be(50);
    }

    [Fact]
    public void Viewer_ShouldHaveCorrectValues()
    {
        // Assert
        StudyRole.Viewer.Id.Should().Be(4);
        StudyRole.Viewer.Name.Should().Be("Viewer");
        StudyRole.Viewer.DisplayName.Should().Be("Viewer");
        StudyRole.Viewer.PermissionLevel.Should().Be(10);
    }

    #endregion

    #region GetAll

    [Fact]
    public void GetAll_ShouldReturnAllRoles()
    {
        // Act
        var allRoles = StudyRole.GetAll();

        // Assert
        allRoles.Should().HaveCount(4);
        allRoles.Should().Contain(StudyRole.Owner);
        allRoles.Should().Contain(StudyRole.Admin);
        allRoles.Should().Contain(StudyRole.Editor);
        allRoles.Should().Contain(StudyRole.Viewer);
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Owner")]
    [InlineData(2, "Admin")]
    [InlineData(3, "Editor")]
    [InlineData(4, "Viewer")]
    public void FromId_WithValidId_ShouldReturnCorrectRole(int id, string expectedName)
    {
        // Act
        var role = StudyRole.FromId(id);

        // Assert
        role.Should().NotBeNull();
        role!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var role = StudyRole.FromId(999);

        // Assert
        role.Should().BeNull();
    }

    #endregion

    #region CanManageMembers

    [Fact]
    public void CanManageMembers_Owner_ShouldReturnTrue()
    {
        // Assert
        StudyRole.Owner.CanManageMembers.Should().BeTrue();
    }

    [Fact]
    public void CanManageMembers_Admin_ShouldReturnTrue()
    {
        // Assert
        StudyRole.Admin.CanManageMembers.Should().BeTrue();
    }

    [Fact]
    public void CanManageMembers_Editor_ShouldReturnFalse()
    {
        // Assert
        StudyRole.Editor.CanManageMembers.Should().BeFalse();
    }

    [Fact]
    public void CanManageMembers_Viewer_ShouldReturnFalse()
    {
        // Assert
        StudyRole.Viewer.CanManageMembers.Should().BeFalse();
    }

    #endregion

    #region CanEditStudy

    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Admin", true)]
    [InlineData("Editor", true)]
    [InlineData("Viewer", false)]
    public void CanEditStudy_ShouldReturnCorrectValue(string roleName, bool expected)
    {
        // Arrange
        var role = StudyRole.FromName(roleName)!;

        // Assert
        role.CanEditStudy.Should().Be(expected);
    }

    #endregion

    #region CanChangeStatus

    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Admin", true)]
    [InlineData("Editor", false)]
    [InlineData("Viewer", false)]
    public void CanChangeStatus_ShouldReturnCorrectValue(string roleName, bool expected)
    {
        // Arrange
        var role = StudyRole.FromName(roleName)!;

        // Assert
        role.CanChangeStatus.Should().Be(expected);
    }

    #endregion

    #region CanDeleteStudy

    [Fact]
    public void CanDeleteStudy_OnlyOwner_ShouldReturnTrue()
    {
        // Assert
        StudyRole.Owner.CanDeleteStudy.Should().BeTrue();
        StudyRole.Admin.CanDeleteStudy.Should().BeFalse();
        StudyRole.Editor.CanDeleteStudy.Should().BeFalse();
        StudyRole.Viewer.CanDeleteStudy.Should().BeFalse();
    }

    #endregion

    #region CanTransferOwnership

    [Fact]
    public void CanTransferOwnership_OnlyOwner_ShouldReturnTrue()
    {
        // Assert
        StudyRole.Owner.CanTransferOwnership.Should().BeTrue();
        StudyRole.Admin.CanTransferOwnership.Should().BeFalse();
        StudyRole.Editor.CanTransferOwnership.Should().BeFalse();
        StudyRole.Viewer.CanTransferOwnership.Should().BeFalse();
    }

    #endregion

    #region PermissionLevel Ordering

    [Fact]
    public void PermissionLevel_ShouldBeOrderedCorrectly()
    {
        // Assert
        StudyRole.Owner.PermissionLevel.Should().BeGreaterThan(StudyRole.Admin.PermissionLevel);
        StudyRole.Admin.PermissionLevel.Should().BeGreaterThan(StudyRole.Editor.PermissionLevel);
        StudyRole.Editor.PermissionLevel.Should().BeGreaterThan(StudyRole.Viewer.PermissionLevel);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameRole_ShouldReturnTrue()
    {
        // Arrange
        var role1 = StudyRole.Admin;
        var role2 = StudyRole.FromId(2);

        // Assert
        (role1 == role2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentRole_ShouldReturnFalse()
    {
        // Arrange
        var role1 = StudyRole.Admin;
        var role2 = StudyRole.Editor;

        // Assert
        (role1 != role2).Should().BeTrue();
    }

    #endregion
}
