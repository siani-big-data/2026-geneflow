using GeneFlow.ApiNet2.Domain.Identity.Entities;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Encapsulates two-factor authentication state and behavior.
/// Supports both email-based codes and TOTP (authenticator app).
/// </summary>
public sealed class TwoFactorAuth : ValueObject
{
    /// <summary>Gets whether 2FA is enabled.</summary>
    public bool IsEnabled { get; }

    /// <summary>Gets the encrypted TOTP secret, if configured.</summary>
    public TwoFactorSecret? TotpSecret { get; }

    /// <summary>Gets whether TOTP (authenticator app) is configured.</summary>
    public bool IsTotpConfigured => TotpSecret is not null;

    private readonly List<TwoFactorCode> _codes;

    /// <summary>Gets the list of email-based 2FA codes.</summary>
    public IReadOnlyList<TwoFactorCode> Codes => _codes.AsReadOnly();

    private TwoFactorAuth()
    {
        _codes = [];
    }

    private TwoFactorAuth(bool isEnabled, List<TwoFactorCode> codes, TwoFactorSecret? totpSecret = null)
    {
        IsEnabled = isEnabled;
        _codes = codes;
        TotpSecret = totpSecret;
    }

    /// <summary>Gets a disabled 2FA state.</summary>
    public static TwoFactorAuth Disabled => new(false, []);

    internal static TwoFactorAuth Reconstitute(bool isEnabled, List<TwoFactorCode> codes, TwoFactorSecret? totpSecret = null)
    {
        return new TwoFactorAuth(isEnabled, codes, totpSecret);
    }

    /// <summary>
    /// Enables two-factor authentication (email-based).
    /// </summary>
    /// <returns>The enabled state or error if already enabled.</returns>
    public Result<TwoFactorAuth> Enable()
    {
        if (IsEnabled)
            return Result.Failure<TwoFactorAuth>(UserErrors.TwoFactorAlreadyEnabled);

        return new TwoFactorAuth(true, _codes, TotpSecret);
    }

    /// <summary>
    /// Enables TOTP-based two-factor authentication.
    /// </summary>
    /// <param name="totpSecret">The encrypted TOTP secret.</param>
    /// <returns>The enabled state with TOTP configured.</returns>
    public Result<TwoFactorAuth> EnableWithTotp(TwoFactorSecret totpSecret)
    {
        if (IsEnabled && IsTotpConfigured)
            return Result.Failure<TwoFactorAuth>(UserErrors.TwoFactorAlreadyEnabled);

        return new TwoFactorAuth(true, _codes, totpSecret);
    }

    /// <summary>
    /// Disables two-factor authentication.
    /// </summary>
    /// <returns>The disabled state or error if not enabled.</returns>
    public Result<TwoFactorAuth> Disable()
    {
        if (!IsEnabled)
            return Result.Failure<TwoFactorAuth>(UserErrors.TwoFactorNotEnabled);

        return new TwoFactorAuth(false, _codes, null);
    }

    /// <summary>
    /// Generates a new email-based 2FA code.
    /// </summary>
    /// <returns>The updated state and the new code.</returns>
    public (TwoFactorAuth Auth, TwoFactorCode Code) GenerateCode()
    {
        foreach (var existingCode in _codes)
        {
            if (!existingCode.IsUsed)
                existingCode.MarkAsUsed();
        }

        var newCode = TwoFactorCode.Create();
        _codes.Add(newCode);

        return (new TwoFactorAuth(IsEnabled, _codes, TotpSecret), newCode);
    }

    /// <summary>
    /// Validates a 2FA code.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <returns>The updated state or error.</returns>
    public Result<TwoFactorAuth> ValidateCode(string code)
    {
        var twoFactorCode = _codes
            .Where(c => !c.IsUsed)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        if (twoFactorCode is null)
            return Result.Failure<TwoFactorAuth>(UserErrors.InvalidTwoFactorCode);

        if (twoFactorCode.IsExpired)
            return Result.Failure<TwoFactorAuth>(UserErrors.TwoFactorCodeExpired);

        if (!twoFactorCode.Validate(code))
            return Result.Failure<TwoFactorAuth>(UserErrors.InvalidTwoFactorCode);

        twoFactorCode.MarkAsUsed();

        return new TwoFactorAuth(IsEnabled, _codes, TotpSecret);
    }

    internal List<TwoFactorCode> GetCodesForPersistence() => _codes;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return _codes.Count;
        yield return TotpSecret?.EncryptedSecret;
    }
}
