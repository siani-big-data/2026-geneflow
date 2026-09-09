using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.Register;

/// <summary>
/// Command to register a new user.
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string Username,
    string Password) : ICommand<Result<UserDto>>;
