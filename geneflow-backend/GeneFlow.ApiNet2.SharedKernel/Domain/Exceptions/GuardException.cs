namespace GeneFlow.ApiNet2.SharedKernel.Domain.Exceptions;

/// <summary>
/// Exception thrown when a guard clause fails.
/// Represents a violation of data integrity constraints.
/// </summary>
public sealed class GuardException : DomainException
{
    /// <summary>
    /// Gets the name of the parameter that violated the guard clause.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="GuardException"/>.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that violated the guard.</param>
    /// <param name="message">The error message.</param>
    public GuardException(string parameterName, string message)
        : base(message)
    {
        ParameterName = parameterName;
    }

    /// <summary>
    /// Creates a guard exception for a specific parameter violation.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="violation">Description of the violation.</param>
    /// <returns>A new guard exception instance.</returns>
    public static GuardException For(string parameterName, string violation)
    {
        return new GuardException(parameterName, $"'{parameterName}' {violation}.");
    }
}
