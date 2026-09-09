using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.GetCurrentUser;

/// <summary>
/// Query to get the current authenticated user.
/// </summary>
public sealed record GetCurrentUserQuery : IQuery<Result<UserDto>>;
