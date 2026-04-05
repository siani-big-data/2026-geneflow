using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.GetUserById;

/// <summary>
/// Query to get a user by their ID.
/// </summary>
public sealed record GetUserByIdQuery(string UserId) : IQuery<Result<UserDto>>;
