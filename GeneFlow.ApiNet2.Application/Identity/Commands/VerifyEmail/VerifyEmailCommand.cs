using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.VerifyEmail;

/// <summary>
/// Command to verify a user's email address.
/// </summary>
public sealed record VerifyEmailCommand(string Token) : ICommand<Result>;
