namespace GeneFlow.ApiNet2.SharedKernel.Domain.Exceptions;

/// <summary>
/// Base exception for domain-level errors.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="DomainException"/> with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DomainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DomainException"/> with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
