using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Traces.Enumerations;

/// <summary>
/// Supported trace file formats.
/// </summary>
public sealed class TraceFormat : Enumeration<TraceFormat>
{
    public static readonly TraceFormat AB1 = new(1, nameof(AB1), "Applied Biosystems", ".ab1", true);
    public static readonly TraceFormat SCF = new(2, nameof(SCF), "Standard Chromatogram Format", ".scf", true);
    public static readonly TraceFormat FASTQ = new(3, nameof(FASTQ), "FASTQ Format", ".fastq", false);
    public static readonly TraceFormat FASTA = new(4, nameof(FASTA), "FASTA Format", ".fasta", false);
    public static readonly TraceFormat GenBank = new(5, nameof(GenBank), "GenBank Format", ".gb", false);

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// File extension including the dot.
    /// </summary>
    public string Extension { get; }

    /// <summary>
    /// Whether this format contains chromatogram data.
    /// </summary>
    public bool HasChromatogramData { get; }

    private TraceFormat(int id, string name, string displayName, string extension, bool hasChromatogramData)
        : base(id, name)
    {
        DisplayName = displayName;
        Extension = extension;
        HasChromatogramData = hasChromatogramData;
    }

    /// <summary>
    /// Gets the format from a file extension.
    /// </summary>
    public static TraceFormat? FromExtension(string extension)
    {
        var normalized = extension.StartsWith(".") ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";

        return normalized switch
        {
            ".ab1" => AB1,
            ".scf" => SCF,
            ".fastq" or ".fq" => FASTQ,
            ".fasta" or ".fa" => FASTA,
            ".gb" or ".gbk" or ".genbank" => GenBank,
            _ => null
        };
    }

    /// <summary>
    /// Gets the content type for this format.
    /// </summary>
    public string GetContentType() => this switch
    {
        _ when this == AB1 => "application/octet-stream",
        _ when this == SCF => "application/octet-stream",
        _ when this == FASTQ => "text/plain",
        _ when this == FASTA => "text/plain",
        _ when this == GenBank => "text/plain",
        _ => "application/octet-stream"
    };
}
