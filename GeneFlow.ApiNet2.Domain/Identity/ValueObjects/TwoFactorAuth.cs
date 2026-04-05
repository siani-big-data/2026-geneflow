using GeneFlow.ApiNet2.Domain.Identity.Entities;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Encapsulates two-factor authentication state and behavior.
/// </summary>
public sealed class TwoFactorAuth : ValueObject
{
    /// <summary>Gets whether 2FA is enabled.</summary>
    public bool IsEnabled { get; }

    private readonly List<TwoFactorCode> _codes;

    /// <summary>Gets the list of 2FA codes.</summary>
    public IReadOnlyList<TwoFactorCode> Codes => _codes.AsReadOnly();

    private TwoFactorAuth()
    {
        _codes = [];
    }

    private TwoFactorAuth(bool isEnabled, List<TwoFactorCode> codes)
    {
        IsEnabled = isEnabled;
        _codes = codes;
    }

    /// <summary>Gets a disabled 2FA state.</summary>
    public static TwoFactorAuth Disabled => new(false, []);

    internal static TwoFactorAuth Reconstitute(bool isEnabled, List<TwoFactorCode> codes)
    {
        return new TwoFactorAuth(isEnabled, codes);
    }

    /// <summary>
    /// Enables two-factor authentication.
    /// </summary>
    /// <returns>The enabled state or error if already enabled.</returns>
    public Result<TwoFactorAuth> Enable()
    {
        if (IsEnabled)
            return Result.Failure<TwoFactorAuth>(UserErrors.TwoFactorAlreadyEnabled);

        return new TwoFactorAuth(true, _codes);
    }

    /// <summary>
    /// Disables two-factor authentication.
    /// </summary>
    /// <returns>The disabled state or error if not enabled.</returns>
    public Result<TwoFactorAuth> Disable()
    {
        if (!IsEnabled)
            return Result.Failure<TwoFactorAuth>(UserErrors.TwoFactorNotEnabled);

        return new TwoFactorAuth(false, _codes);
    }

    /// <summary>
    /// Generates a new 2FA code.
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

        return (new TwoFactorAuth(IsEnabled, _codes), newCode);
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

        return new TwoFactorAuth(IsEnabled, _codes);
    }

    internal List<TwoFactorCode> GetCodesForPersistence() => _codes;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return _codes.Count;
    }
}
