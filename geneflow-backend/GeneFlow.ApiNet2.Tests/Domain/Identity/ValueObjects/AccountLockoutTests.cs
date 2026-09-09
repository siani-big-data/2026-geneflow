using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.ValueObjects;

/// <summary>
/// Unit tests for the AccountLockout value object.
/// </summary>
public class AccountLockoutTests
{
    #region None

    [Fact]
    public void None_ShouldReturnUnlockedState()
    {
        // Act
        var lockout = AccountLockout.None;

        // Assert
        lockout.FailedAttempts.Should().Be(0);
        lockout.LockoutEnd.Should().BeNull();
        lockout.IsLockedOut.Should().BeFalse();
    }

    #endregion

    #region IsLockedOut

    [Fact]
    public void IsLockedOut_WhenNoLockoutEnd_ShouldReturnFalse()
    {
        // Arrange
        var lockout = AccountLockout.None;

        // Act & Assert
        lockout.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_WhenLockoutEndInFutuREDACTED()
    {
        // Arrange
        var lockout = AccountLockout.None;

        // Simulate max failed attempts to trigger lockout
        for (var i = 0; i < AccountLockout.MaxFailedAttempts; i++)
        {
            (lockout, _) = lockout.RecordFailedAttempt();
        }

        // Assert
        lockout.IsLockedOut.Should().BeTrue();
        lockout.LockoutEnd.Should().BeAfter(DateTime.UtcNow);
    }

    #endregion

    #region RecordFailedAttempt

    [Fact]
    public void RecordFailedAttempt_ShouldIncrementFailedAttempts()
    {
        // Arrange
        var lockout = AccountLockout.None;

        // Act
        var (newLockout, wasLockedOut) = lockout.RecordFailedAttempt();

        // Assert
        newLockout.FailedAttempts.Should().Be(1);
        wasLockedOut.Should().BeFalse();
    }

    [Fact]
    public void RecordFailedAttempt_WhenBelowMax_ShouldNotLockout()
    {
        // Arrange
        var lockout = AccountLockout.None;

        // Act - Record attempts below max
        for (var i = 0; i < AccountLockout.MaxFailedAttempts - 1; i++)
        {
            (lockout, var wasLockedOut) = lockout.RecordFailedAttempt();
            wasLockedOut.Should().BeFalse();
        }

        // Assert
        lockout.IsLockedOut.Should().BeFalse();
        lockout.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public void RecordFailedAttempt_WhenReachingMax_ShouldTriggerLockout()
    {
        // Arrange
        var lockout = AccountLockout.None;

        // Act - Record max failed attempts
        for (var i = 0; i < AccountLockout.MaxFailedAttempts - 1; i++)
        {
            (lockout, _) = lockout.RecordFailedAttempt();
        }

        var (finalLockout, wasLockedOut) = lockout.RecordFailedAttempt();

        // Assert
        wasLockedOut.Should().BeTrue();
        finalLockout.IsLockedOut.Should().BeTrue();
        finalLockout.FailedAttempts.Should().Be(AccountLockout.MaxFailedAttempts);
        finalLockout.LockoutEnd.Should().NotBeNull();
        finalLockout.LockoutEnd.Should().BeCloseTo(
            DateTime.UtcNow.Add(AccountLockout.LockoutDuration),
            TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Reset

    [Fact]
    public void Reset_ShouldReturnUnlockedState()
    {
        // Arrange
        var lockout = AccountLockout.None;
        for (var i = 0; i < AccountLockout.MaxFailedAttempts; i++)
        {
            (lockout, _) = lockout.RecordFailedAttempt();
        }

        // Act
        var resetLockout = lockout.Reset();

        // Assert
        resetLockout.FailedAttempts.Should().Be(0);
        resetLockout.LockoutEnd.Should().BeNull();
        resetLockout.IsLockedOut.Should().BeFalse();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameState_ShouldReturnTrue()
    {
        // Arrange
        var lockout1 = AccountLockout.None;
        var lockout2 = AccountLockout.None;

        // Assert
        lockout1.Should().Be(lockout2);
    }

    [Fact]
    public void Equals_WithDifferentFailedAttempts_ShouldReturnFalse()
    {
        // Arrange
        var lockout1 = AccountLockout.None;
        var (lockout2, _) = lockout1.RecordFailedAttempt();

        // Assert
        lockout1.Should().NotBe(lockout2);
    }

    #endregion

    #region Constants

    [Fact]
    public void MaxFailedAttempts_ShouldBeFive()
    {
        AccountLockout.MaxFailedAttempts.Should().Be(5);
    }

    [Fact]
    public void LockoutDuration_ShouldBeFifteenMinutes()
    {
        AccountLockout.LockoutDuration.Should().Be(TimeSpan.FromMinutes(15));
    }

    #endregion
}
