using GeneFlow.ApiNet2.API.Contracts.Identity.Responses;
using GeneFlow.ApiNet2.Application.Identity.DTOs;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Extension methods for mapping user DTOs to API responses.
/// </summary>
public static class UserMappingExtensions
{
    /// <summary>
    /// Converts a UserDto to a UserResponse.
    /// </summary>
    public static UserResponse ToResponse(this UserDto dto) => new(
        dto.Id,
        dto.Email,
        dto.Username,
        dto.IsActive,
        dto.EmailVerified,
        dto.TwoFactorEnabled,
        dto.HasPassword,
        dto.Roles,
        dto.CreatedAt,
        dto.ModifiedAt);

    /// <summary>
    /// Converts an AuthTokensDto to an AuthTokensResponse.
    /// </summary>
    public static AuthTokensResponse ToResponse(this AuthTokensDto dto) => new(
        dto.AccessToken,
        dto.RefreshToken,
        dto.ExpiresAt);

    /// <summary>
    /// Converts an AuthTokensDto to an AuthTokensResponse or null if tokens are null.
    /// </summary>
    public static AuthTokensResponse? ToResponseOrNull(this AuthTokensDto? dto) =>
        dto is null ? null : dto.ToResponse();
}
