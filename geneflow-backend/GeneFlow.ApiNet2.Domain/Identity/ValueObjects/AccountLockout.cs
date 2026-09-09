using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Encapsulates account lockout state and behavior.
/// </summary>
public sealed class AccountLockout : ValueObject
{
    /// <summary>Maximum failed attempts before lockout.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>Duration of account lockout.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>Gets the number of failed login attempts.</summary>
    public int FailedAttempts { get; private set; }

    /// <summary>Gets when the lockout ends, if locked.</summary>
    public DateTime? LockoutEnd { get; private set; }

    private AccountLockout() { }

    private AccountLockout(int failedAttempts, DateTime? lockoutEnd)
    {
        FailedAttempts = failedAttempts;
        LockoutEnd = lockoutEnd;
    }

    /// <summary>Gets an unlocked state with no failed attempts.</summary>
    public static AccountLockout None => new(0, null);

    /// <summary>Gets whether the account is currently locked.</summary>
    public bool IsLockedOut => LockoutEnd.HasValue && DateTime.UtcNow < LockoutEnd.Value;

    /// <summary>
    /// Records a failed login attempt.
    /// </summary>
    /// <returns>The new lockout state and whether lockout was triggered.</returns>
    public (AccountLockout Lockout, bool WasLockedOut) RecordFailedAttempt()
    {
        var newFailedAttempts = FailedAttempts + 1;

        if (newFailedAttempts >= MaxFailedAttempts)
        {
            var lockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
            return (new AccountLockout(newFailedAttempts, lockoutEnd), true);
        }

        return (new AccountLockout(newFailedAttempts, null), false);
    }

    /// <summary>
    /// Resets the lockout state.
    /// </summary>
    /// <returns>An unlocked state.</returns>
    public AccountLockout Reset() => None;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FailedAttempts;
        yield return LockoutEnd;
    }
}
