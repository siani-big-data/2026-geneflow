using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfileByUserId;

/// <summary>
/// Query to get a profile by user ID.
/// </summary>
public sealed record GetProfileByUserIdQuery(string UserId) : IQuery<Result<ProfileDto>>;
