using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetCurrentUserProfile;

/// <summary>
/// Query to get the current authenticated user's profile.
/// </summary>
public sealed record GetCurrentUserProfileQuery : IQuery<Result<ProfileDto>>;
