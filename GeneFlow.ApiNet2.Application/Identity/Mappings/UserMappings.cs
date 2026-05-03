using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.Application.Identity.Mappings;

/// <summary>
/// Extension methods for mapping User domain entities to DTOs.
/// </summary>
public static class UserMappings
{
    /// <summary>
    /// Maps a User entity to a UserDto.
    /// </summary>
    /// <param name="user">The user entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id.ToString(),
            Email = user.Email.Value,
            Username = user.Username.Value,
            EmailVerified = user.EmailVerified,
            IsActive = user.IsActive,
            TwoFactorEnabled = user.TwoFactorEnabled,
            HasPassword = user.HasPassword,
            Roles = user.Roles.Select(r => r.Name).ToList(),
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt
        };
    }
}
