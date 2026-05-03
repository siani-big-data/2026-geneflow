using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.PaymentMethods;

/// <summary>
/// Strongly-typed identifier for PaymentMethod aggregate.
/// Format: PM00000001 (PM + 8 digits)
/// </summary>
public sealed class PaymentMethodId : PrefixedId<PaymentMethodId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "payment_methods";

    /// <inheritdoc />
    protected override char Prefix => 'M';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    /// <summary>
    /// Initializes a new PaymentMethodId.
    /// </summary>
    public PaymentMethodId(long value) : base(value) { }

    /// <summary>
    /// Parses a string ID into a PaymentMethodId.
    /// </summary>
    public static PaymentMethodId Parse(string id) => Parse(id, v => new PaymentMethodId(v));

    /// <summary>
    /// Tries to parse a string ID into a PaymentMethodId.
    /// </summary>
    public static bool TryParse(string? id, out PaymentMethodId? result) => TryParse(id, v => new PaymentMethodId(v), out result);

    /// <summary>
    /// Creates a PaymentMethodId from a sequence value.
    /// </summary>
    public static PaymentMethodId FromSequence(long sequenceValue) => new(sequenceValue);
}
