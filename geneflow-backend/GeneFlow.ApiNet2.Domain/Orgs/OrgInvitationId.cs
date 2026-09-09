using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Orgs;

/// <summary>
/// Strongly-typed identifier for an OrgInvitation aggregate.
/// Format: I######## (eight zero-padded digits).
/// </summary>
public sealed class OrgInvitationId : PrefixedId<OrgInvitationId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "org_invitations";

    /// <inheritdoc />
    protected override char Prefix => 'I';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    public OrgInvitationId(long value) : base(value) { }

    public static OrgInvitationId Parse(string id) => Parse(id, v => new OrgInvitationId(v));

    public static bool TryParse(string? id, out OrgInvitationId? result)
        => TryParse(id, v => new OrgInvitationId(v), out result);

    public static OrgInvitationId FromSequence(long sequenceValue) => new(sequenceValue);
}
