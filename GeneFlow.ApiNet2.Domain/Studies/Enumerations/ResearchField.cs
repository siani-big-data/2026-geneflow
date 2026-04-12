using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Studies.Enumerations;

/// <summary>
/// Research field/discipline for a study.
/// </summary>
public sealed class ResearchField : Enumeration<ResearchField>
{
    public static readonly ResearchField Genomics = new(1, nameof(Genomics), "Genomics");
    public static readonly ResearchField Proteomics = new(2, nameof(Proteomics), "Proteomics");
    public static readonly ResearchField Transcriptomics = new(3, nameof(Transcriptomics), "Transcriptomics");
    public static readonly ResearchField Metagenomics = new(4, nameof(Metagenomics), "Metagenomics");
    public static readonly ResearchField Phylogenetics = new(5, nameof(Phylogenetics), "Phylogenetics");
    public static readonly ResearchField MolecularBiology = new(6, nameof(MolecularBiology), "Molecular Biology");
    public static readonly ResearchField Genetics = new(7, nameof(Genetics), "Genetics");
    public static readonly ResearchField Bioinformatics = new(8, nameof(Bioinformatics), "Bioinformatics");
    public static readonly ResearchField Other = new(9, nameof(Other), "Other");

    public string DisplayName { get; }

    private ResearchField(int id, string name, string displayName) : base(id, name)
    {
        DisplayName = displayName;
    }
}
