using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.DeleteAccount;

/// <summary>
/// Command to permanently delete the current user's account.
/// </summary>
public sealed record DeleteAccountCommand(
    UserId UserId,
    string ConfirmationText) : ICommand<Result>;
