using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.GetUserExternalLogins;

/// <summary>
/// Query to get a user's linked external logins.
/// </summary>
/// <param name="UserId">The user's ID.</param>
public sealed record GetUserExternalLoginsQuery(string UserId)
    : IQuery<Result<IReadOnlyList<ExternalLoginDto>>>;
