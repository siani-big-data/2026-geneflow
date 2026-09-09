namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Abstraction for GUID generation.
/// Allows for deterministic testing by replacing with a test implementation.
/// </summary>
public interface IGuidGenerator
{
    /// <summary>
    /// Generates a new unique identifier.
    /// </summary>
    Guid NewGuid();
}

/// <summary>
/// Default implementation using system GUID generation.
/// </summary>
public sealed class SystemGuidGenerator : IGuidGenerator
{
    /// <inheritdoc />
    public Guid NewGuid() => Guid.NewGuid();
}
