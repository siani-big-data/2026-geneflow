namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// Marker interface for commands/queries that require an authenticated user.
/// When a request implements this interface, the AuthenticationBehavior will
/// validate that the user is authenticated before the handler is invoked.
/// </summary>
public interface IRequireAuthentication
{
}
