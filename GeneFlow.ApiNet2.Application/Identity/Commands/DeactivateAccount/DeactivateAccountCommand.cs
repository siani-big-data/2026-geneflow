using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.DeactivateAccount;

/// <summary>
/// Command to deactivate the current user's account.
/// </summary>
public sealed record DeactivateAccountCommand(UserId UserId) : ICommand<Result>;
