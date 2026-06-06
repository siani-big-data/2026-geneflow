namespace GeneFlow.ApiNet2.Application.Identity.Queries.GetFollowers;

/// <summary>
/// Minimal projection of a user appearing in a followers / following list.
/// </summary>
public sealed record FollowUserDto(
    string UserId,
    string Username,
    string Email,
    DateTime CreatedAt);
