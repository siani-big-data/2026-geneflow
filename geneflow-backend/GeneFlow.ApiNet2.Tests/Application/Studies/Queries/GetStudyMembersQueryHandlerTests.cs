using GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyMembers;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Queries;

/// <summary>
/// Unit tests for GetStudyMembersQueryHandler.
/// </summary>
public class GetStudyMembersQueryHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly GetStudyMembersQueryHandler _handler;

    public GetStudyMembersQueryHandlerTests()
    {
        _handler = new GetStudyMembersQueryHandler(
            _studyRepository,
            _userRepository,
            _profileRepository);

        // Setup default returns for user and profile repos
        _userRepository.GetByIdsAsync(Arg.Any<IEnumerable<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User>());
        _profileRepository.GetByUserIdsAsync(Arg.Any<IEnumerable<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Profile>());
    }

    #region Helper Methods

    private static Study CreateTestStudy(
        UserId? ownerId = null,
        StudyStatus? status = null)
    {
        var studyId = new StudyId(1);
        var userId = ownerId ?? new UserId(1);
        var title = StudyTitle.Create("Test Study").Value;
        var description = StudyDescription.Create("Test description").Value;

        var study = Study.Create(studyId, userId, title, description, ResearchField.Genomics).Value;

        if (status is not null && status != StudyStatus.Draft)
        {
            study.ChangeStatus(StudyStatus.Active, userId);
            if (status == StudyStatus.Completed || status == StudyStatus.Published)
            {
                study.ChangeStatus(StudyStatus.Completed, userId);
                if (status == StudyStatus.Published)
                {
                    study.ChangeStatus(StudyStatus.Published, userId);
                }
            }
        }

        return study;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidStudyId_ShouldReturnMembers()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddMember(new UserId(2), StudyRole.Editor, ownerId);
        study.AddMember(new UserId(3), StudyRole.Viewer, ownerId);

        var query = new GetStudyMembersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnMembers()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddMember(new UserId(2), StudyRole.Editor, ownerId);
        study.AddMember(new UserId(3), StudyRole.Viewer, ownerId);

        var query = new GetStudyMembersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnWithRoles()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddMember(new UserId(2), StudyRole.Admin, ownerId);
        study.AddMember(new UserId(3), StudyRole.Editor, ownerId);
        study.AddMember(new UserId(4), StudyRole.Viewer, ownerId);

        var query = new GetStudyMembersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);

        // Verify all roles are present
        result.Value.Should().Contain(m => m.Role == "Owner" && m.RoleId == StudyRole.Owner.Id);
        result.Value.Should().Contain(m => m.Role == "Admin" && m.RoleId == StudyRole.Admin.Id);
        result.Value.Should().Contain(m => m.Role == "Editor" && m.RoleId == StudyRole.Editor.Id);
        result.Value.Should().Contain(m => m.Role == "Viewer" && m.RoleId == StudyRole.Viewer.Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectRoles()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddMember(new UserId(2), StudyRole.Admin, ownerId);

        var query = new GetStudyMembersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(m => m.Role == "Owner");
        result.Value.Should().Contain(m => m.Role == "Admin");
    }

    [Fact]
    public async Task Handle_WhenNoMembers_ShouldReturnEmpty()
    {
        // Note: A study always has at least one member (the owner)
        // This test validates the scenario where we only have the owner
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var query = new GetStudyMembersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1); // Only owner
        result.Value.Single().Role.Should().Be("Owner");
    }

    [Fact]
    public async Task Handle_PublishedStudy_NonMemberCanViewMembers()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId, StudyStatus.Published);

        var query = new GetStudyMembersQuery("S00000001", "U00000099"); // Non-member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_AsMember_ShouldReturnMembers()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Viewer, ownerId);

        var query = new GetStudyMembersQuery("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetStudyMembersQuery("invalid-id", "U00000001");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetStudyMembersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Permission Failures

    [Fact]
    public async Task Handle_DraftStudy_NonMemberCannotView()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId); // Draft status

        var query = new GetStudyMembersQuery("S00000001", "U00000099"); // Non-member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentStudy_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetStudyMembersQuery("S00000999", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Pagination

    [Fact]
    public async Task Handle_ShouldPaginate()
    {
        // Note: The current GetStudyMembersQuery doesn't have pagination parameters
        // This test validates that the handler returns all members
        // If pagination is added in the future, this test should be updated
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        // Add multiple members
        for (int i = 2; i <= 10; i++)
        {
            study.AddMember(new UserId(i), StudyRole.Viewer, ownerId);
        }

        var query = new GetStudyMembersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(10); // Owner + 9 members
    }

    #endregion

    #region Member Details

    [Fact]
    public async Task Handle_ShouldIncludeJoinedAt()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddMember(new UserId(2), StudyRole.Editor, ownerId);

        var query = new GetStudyMembersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(m => m.JoinedAt.Should().NotBe(default));
    }

    [Fact]
    public async Task Handle_ShouldIncludeInvitedBy()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddMember(new UserId(2), StudyRole.Editor, ownerId);

        var query = new GetStudyMembersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var addedMember = result.Value.FirstOrDefault(m => m.UserId == "U00000002");
        addedMember.Should().NotBeNull();
        addedMember!.InvitedBy.Should().Be("U00000001");
    }

    #endregion
}
