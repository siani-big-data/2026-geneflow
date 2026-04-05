using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Services;

/// <summary>
/// Service for validating user authentication state.
/// </summary>
public sealed class UserAuthenticationValidator : IUserAuthenticationValidator
{
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the validator.
    /// </summary>
    public UserAuthenticationValidator(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    public Result ValidateCanAuthenticate(User user)
    {
        if (!user.IsActive)
            return Result.Failure(UserErrors.UserDeactivated);

        if (user.IsLockedOut)
            return Result.Failure(UserErrors.AccountLockedUntil(user.LockoutEnd!.Value));

        if (!user.EmailVerified)
            return Result.Failure(UserErrors.EmailNotVerified);

        return Result.Success();
    }

    /// <inheritdoc />
    public Result ValidatePassword(User user, string password)
    {
        if (!user.HasPassword)
            return Result.Failure(UserErrors.InvalidCredentials);

        if (!_passwordHasher.Verify(password, user.PasswordHash.Value))
            return Result.Failure(UserErrors.InvalidCredentials);

        return Result.Success();
    }
}
