using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.Entities;

/// <summary>
/// Unit tests for the StudyStar entity.
/// </summary>
public class StudyStarTests
{
    private static StudyId CreateStudyId(long value = 1) => new(value);
    private static UserId CreateUserId(long value = 1) => new(value);

    #region Create

    [Fact]
    public void Create_WithValidData_ShouldReturnStar()
    {
        // Arrange
        var studyId = CreateStudyId(42);
        var userId = CreateUserId(99);

        // Act
        var star = StudyStar.Create(studyId, userId);

        // Assert
        star.Should().NotBeNull();
        star.StudyId.Should().Be(studyId);
        star.UserId.Should().Be(userId);
    }

    [Fact]
    public void Create_ShouldSetStarredAtToNow()
    {
        // Arrange
        var studyId = CreateStudyId();
        var userId = CreateUserId();
        var beforeCreate = DateTime.UtcNow;

        // Act
        var star = StudyStar.Create(studyId, userId);

        // Assert
        star.StarredAt.Should().BeOnOrAfter(beforeCreate);
        star.StarredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Arrange
        var studyId = CreateStudyId();
        var userId1 = CreateUserId(1);
        var userId2 = CreateUserId(2);

        // Act
        var star1 = StudyStar.Create(studyId, userId1);
        var star2 = StudyStar.Create(studyId, userId2);

        // Assert
        star1.Id.Should().NotBe(star2.Id);
        star1.Id.Should().NotBe(Guid.Empty);
        star2.Id.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region Multiple Stars

    [Fact]
    public void Create_DifferentUsers_ShouldCreateSeparateStars()
    {
        // Arrange
        var studyId = CreateStudyId();
        var user1 = CreateUserId(1);
        var user2 = CreateUserId(2);
        var user3 = CreateUserId(3);

        // Act
        var star1 = StudyStar.Create(studyId, user1);
        var star2 = StudyStar.Create(studyId, user2);
        var star3 = StudyStar.Create(studyId, user3);

        // Assert
        star1.UserId.Should().NotBe(star2.UserId);
        star2.UserId.Should().NotBe(star3.UserId);
        star1.StudyId.Should().Be(star2.StudyId);
        star2.StudyId.Should().Be(star3.StudyId);
    }

    [Fact]
    public void Create_DifferentStudies_ShouldCreateSeparateStars()
    {
        // Arrange
        var study1 = CreateStudyId(1);
        var study2 = CreateStudyId(2);
        var userId = CreateUserId();

        // Act
        var star1 = StudyStar.Create(study1, userId);
        var star2 = StudyStar.Create(study2, userId);

        // Assert
        star1.StudyId.Should().NotBe(star2.StudyId);
        star1.UserId.Should().Be(star2.UserId);
    }

    #endregion
}
