using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Common.Builders;

/// <summary>
/// Builder for creating User entities in tests.
/// </summary>
public class UserBuilder
{
    private UserId _id = new(1);
    private Email _email = Email.Create("test@example.com").Value;
    private Username _username = Username.Create("testuser").Value;
    private PasswordHash _passwordHash = PasswordHash.Create("hashedpassword123").Value;
    private bool _isEmailVerified = true;
    private bool _isActive = true;

    public UserBuilder WithId(long id)
    {
        _id = new UserId(id);
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = Email.Create(email).Value;
        return this;
    }

    public UserBuilder WithUsername(string username)
    {
        _username = Username.Create(username).Value;
        return this;
    }

    public UserBuilder WithPasswordHash(string hash)
    {
        _passwordHash = PasswordHash.Create(hash).Value;
        return this;
    }

    public UserBuilder WithEmailVerified(bool verified = true)
    {
        _isEmailVerified = verified;
        return this;
    }

    public UserBuilder WithActive(bool active = true)
    {
        _isActive = active;
        return this;
    }

    public User Build()
    {
        var result = User.Create(_id, _email, _username, _passwordHash);
        var user = result.Value;

        if (_isEmailVerified)
        {
            // User.Create() automatically creates a pending EmailVerification with token
            // Just use the existing token to verify the email
            var token = user.EmailVerificationToken!;
            user.VerifyEmail(token);
        }

        if (!_isActive)
        {
            user.Deactivate();
        }

        user.ClearDomainEvents();
        return user;
    }

    public static UserBuilder Default() => new();
}
