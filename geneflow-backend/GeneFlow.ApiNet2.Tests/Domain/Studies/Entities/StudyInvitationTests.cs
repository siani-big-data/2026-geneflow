using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.Entities;

/// <summary>
/// Unit tests for the StudyInvitation entity.
/// </summary>
public class StudyInvitationTests
{
    private static StudyId CreateStudyId(long value = 1) => new(value);
    private static StudyInvitationId CreateInvitationId(long value = 1) => new(value);
    private static UserId CreateUserId(long value = 1) => new(value);

    #region Create

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var invitationId = CreateInvitationId();
        var studyId = CreateStudyId();
        var invitedBy = CreateUserId();
        const string email = "test@example.com";

        // Act
        var result = StudyInvitation.Create(invitationId, studyId, email, StudyRole.Editor, invitedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(invitationId);
        result.Value.StudyId.Should().Be(studyId);
        result.Value.Email.Should().Be(email.ToLowerInvariant());
        result.Value.Role.Should().Be(StudyRole.Editor);
        result.Value.InvitedBy.Should().Be(invitedBy);
        result.Value.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public void Create_ShouldNormalizeEmailToLowerCase()
    {
        // Arrange
        var invitationId = CreateInvitationId();
        var studyId = CreateStudyId();
        var invitedBy = CreateUserId();
        const string email = "Test@Example.COM";

        // Act
        var result = StudyInvitation.Create(invitationId, studyId, email, StudyRole.Editor, invitedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueToken()
    {
        // Arrange
        var studyId = CreateStudyId();
        var invitedBy = CreateUserId();

        // Act
        var result1 = StudyInvitation.Create(CreateInvitationId(1), studyId, "test1@example.com", StudyRole.Editor, invitedBy);
        var result2 = StudyInvitation.Create(CreateInvitationId(2), studyId, "test2@example.com", StudyRole.Editor, invitedBy);

        // Assert
        result1.Value.Token.Should().NotBe(result2.Value.Token);
        result1.Value.Token.Should().HaveLength(StudyInvitation.TokenLength);
    }

    [Fact]
    public void Create_ShouldSetExpirationDate()
    {
        // Arrange
        var invitationId = CreateInvitationId();
        var studyId = CreateStudyId();
        var invitedBy = CreateUserId();
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = StudyInvitation.Create(invitationId, studyId, "test@example.com", StudyRole.Editor, invitedBy);

        // Assert
        result.Value.ExpiresAt.Should().BeAfter(beforeCreate);
        result.Value.ExpiresAt.Should().BeCloseTo(
            DateTime.UtcNow.Add(StudyInvitation.DefaultExpiration),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithMessage_ShouldStoreMessage()
    {
        // Arrange
        var invitationId = CreateInvitationId();
        var studyId = CreateStudyId();
        var invitedBy = CreateUserId();
        const string message = "Please join our research team!";

        // Act
        var result = StudyInvitation.Create(invitationId, studyId, "test@example.com", StudyRole.Editor, invitedBy, message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Message.Should().Be(message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEmail_ShouldReturnFailure(string? email)
    {
        // Arrange
        var invitationId = CreateInvitationId();
        var studyId = CreateStudyId();
        var invitedBy = CreateUserId();

        // Act
        var result = StudyInvitation.Create(invitationId, studyId, email!, StudyRole.Editor, invitedBy);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithOwnerRole_ShouldReturnFailure()
    {
        // Arrange
        var invitationId = CreateInvitationId();
        var studyId = CreateStudyId();
        var invitedBy = CreateUserId();

        // Act
        var result = StudyInvitation.Create(invitationId, studyId, "test@example.com", StudyRole.Owner, invitedBy);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMessageTooLong_ShouldReturnFailure()
    {
        // Arrange
        var invitationId = CreateInvitationId();
        var studyId = CreateStudyId();
        var invitedBy = CreateUserId();
        var longMessage = new string('a', StudyInvitation.MaxMessageLength + 1);

        // Act
        var result = StudyInvitation.Create(invitationId, studyId, "test@example.com", StudyRole.Editor, invitedBy, longMessage);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Accept

    [Fact]
    public void Accept_WhenPending_ShouldReturnSuccessAndChangeStatus()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;

        // Act
        var result = invitation.Accept();

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void Accept_WhenAlreadyAccepted_ShouldReturnFailure()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;
        invitation.Accept();

        // Act
        var result = invitation.Accept();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Accept_WhenDeclined_ShouldReturnFailure()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;
        invitation.Decline();

        // Act
        var result = invitation.Accept();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Decline

    [Fact]
    public void Decline_WhenPending_ShouldReturnSuccessAndChangeStatus()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;

        // Act
        var result = invitation.Decline();

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Declined);
        invitation.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void Decline_WhenAlreadyAccepted_ShouldReturnFailure()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;
        invitation.Accept();

        // Act
        var result = invitation.Decline();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_WhenPending_ShouldReturnSuccessAndChangeStatus()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;

        // Act
        var result = invitation.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyAccepted_ShouldReturnFailure()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;
        invitation.Accept();

        // Act
        var result = invitation.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Resend

    [Fact]
    public void Resend_WhenPending_ShouldReturnSuccessAndResetToken()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;
        var originalToken = invitation.Token;

        // Act
        var result = invitation.Resend();

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Token.Should().NotBe(originalToken);
        invitation.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public void Resend_WhenAccepted_ShouldReturnFailure()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;
        invitation.Accept();

        // Act
        var result = invitation.Resend();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Computed Properties

    [Fact]
    public void IsPending_WhenPendingAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;

        // Assert
        invitation.IsPending.Should().BeTrue();
    }

    [Fact]
    public void IsPending_WhenAccepted_ShouldReturnFalse()
    {
        // Arrange
        var invitation = StudyInvitation.Create(
            CreateInvitationId(), CreateStudyId(), "test@example.com", StudyRole.Editor, CreateUserId()).Value;
        invitation.Accept();

        // Assert
        invitation.IsPending.Should().BeFalse();
    }

    #endregion
}
