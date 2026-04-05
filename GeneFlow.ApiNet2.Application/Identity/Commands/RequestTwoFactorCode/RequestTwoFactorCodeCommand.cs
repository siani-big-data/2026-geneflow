using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.RequestTwoFactorCode;

/// <summary>
/// Command to request a new two-factor authentication code.
/// </summary>
public sealed record RequestTwoFactorCodeCommand(string EmailOrUsername) : ICommand<Result>;
