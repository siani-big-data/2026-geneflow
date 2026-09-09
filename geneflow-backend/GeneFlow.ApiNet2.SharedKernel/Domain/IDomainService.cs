namespace GeneFlow.ApiNet2.SharedKernel.Domain;

/// <summary>
/// Marker interface for domain services.
/// Domain services encapsulate domain logic that doesn't naturally fit within an entity or value object.
/// Use when the operation involves multiple aggregates or requires external dependencies.
/// </summary>
public interface IDomainService;
