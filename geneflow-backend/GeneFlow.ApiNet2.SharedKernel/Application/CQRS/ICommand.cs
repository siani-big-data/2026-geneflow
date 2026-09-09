namespace GeneFlow.ApiNet2.SharedKernel.Application.CQRS;

/// <summary>
/// Marker interface for commands that don't return a value.
/// Commands represent intentions to change the system state.
/// </summary>
public interface ICommand : IRequest;

/// <summary>
/// Marker interface for commands that return a response.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;
