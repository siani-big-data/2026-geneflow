namespace GeneFlow.ApiNet2.SharedKernel.Application.CQRS;

/// <summary>
/// Handler for commands that don't return a value.
/// </summary>
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand;

/// <summary>
/// Handler for commands that return a response.
/// </summary>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
