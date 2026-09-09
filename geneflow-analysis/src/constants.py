"""Biological constants and reference tables for GeneFlow Analysis Worker."""

BASES = {"A", "C", "G", "T"}

IUPAC_CODES: dict[str, set[str]] = {
    "A": {"A"},
    "C": {"C"},
    "G": {"G"},
    "T": {"T"},
    "R": {"A", "G"},
    "Y": {"C", "T"},
    "S": {"G", "C"},
    "W": {"A", "T"},
    "K": {"G", "T"},
    "M": {"A", "C"},
    "B": {"C", "G", "T"},
    "D": {"A", "G", "T"},
    "H": {"A", "C", "T"},
    "V": {"A", "C", "G"},
    "N": {"A", "C", "G", "T"},
}

BASES_TO_IUPAC: dict[frozenset[str], str] = {
    frozenset(bases): code for code, bases in IUPAC_CODES.items()
}


COMPLEMENT: dict[str, str] = {
    "A": "T",
    "T": "A",
    "C": "G",
    "G": "C",
    "R": "Y",
    "Y": "R",
    "S": "S",
    "W": "W",
    "K": "M",
    "M": "K",
    "B": "V",
    "D": "H",
    "H": "D",
    "V": "B",
    "N": "N",
}


CODON_TABLE: dict[str, str] = {
    "TTT": "F",
    "TTC": "F",
    "TTA": "L",
    "TTG": "L",
    "CTT": "L",
    "CTC": "L",
    "CTA": "L",
    "CTG": "L",
    "ATT": "I",
    "ATC": "I",
    "ATA": "I",
    "ATG": "M",
    "GTT": "V",
    "GTC": "V",
    "GTA": "V",
    "GTG": "V",
    "TCT": "S",
    "TCC": "S",
    "TCA": "S",
    "TCG": "S",
    "AGT": "S",
    "AGC": "S",
    "CCT": "P",
    "CCC": "P",
    "CCA": "P",
    "CCG": "P",
    "ACT": "T",
    "ACC": "T",
    "ACA": "T",
    "ACG": "T",
    "GCT": "A",
    "GCC": "A",
    "GCA": "A",
    "GCG": "A",
    "TAT": "Y",
    "TAC": "Y",
    "TAA": "*",
    "TAG": "*",
    "TGA": "*",
    "CAT": "H",
    "CAC": "H",
    "CAA": "Q",
    "CAG": "Q",
    "AAT": "N",
    "AAC": "N",
    "AAA": "K",
    "AAG": "K",
    "GAT": "D",
    "GAC": "D",
    "GAA": "E",
    "GAG": "E",
    "TGT": "C",
    "TGC": "C",
    "TGG": "W",
    "CGT": "R",
    "CGC": "R",
    "CGA": "R",
    "CGG": "R",
    "AGA": "R",
    "AGG": "R",
    "GGT": "G",
    "GGC": "G",
    "GGA": "G",
    "GGG": "G",
}

START_CODONS = {"ATG"}

STOP_CODONS = {"TAA", "TAG", "TGA"}


AMINO_ACIDS: dict[str, tuple[str, str, str]] = {
    "A": ("Ala", "Alanine", "nonpolar"),
    "C": ("Cys", "Cysteine", "polar"),
    "D": ("Asp", "Aspartic acid", "acidic"),
    "E": ("Glu", "Glutamic acid", "acidic"),
    "F": ("Phe", "Phenylalanine", "nonpolar"),
    "G": ("Gly", "Glycine", "nonpolar"),
    "H": ("His", "Histidine", "basic"),
    "I": ("Ile", "Isoleucine", "nonpolar"),
    "K": ("Lys", "Lysine", "basic"),
    "L": ("Leu", "Leucine", "nonpolar"),
    "M": ("Met", "Methionine", "nonpolar"),
    "N": ("Asn", "Asparagine", "polar"),
    "P": ("Pro", "Proline", "nonpolar"),
    "Q": ("Gln", "Glutamine", "polar"),
    "R": ("Arg", "Arginine", "basic"),
    "S": ("Ser", "Serine", "polar"),
    "T": ("Thr", "Threonine", "polar"),
    "V": ("Val", "Valine", "nonpolar"),
    "W": ("Trp", "Tryptophan", "nonpolar"),
    "Y": ("Tyr", "Tyrosine", "polar"),
    "*": ("***", "Stop", "stop"),
}

THREE_TO_ONE: dict[str, str] = {v[0]: k for k, v in AMINO_ACIDS.items()}


RESTRICTION_ENZYMES: dict[str, tuple[str, int, str]] = {
    "EcoRI": ("GAATTC", 1, "5'"),
    "BamHI": ("GGATCC", 1, "5'"),
    "HindIII": ("AAGCTT", 1, "5'"),
    "XhoI": ("CTCGAG", 1, "5'"),
    "SalI": ("GTCGAC", 1, "5'"),
    "PstI": ("CTGCAG", 5, "3'"),
    "SphI": ("GCATGC", 5, "3'"),
    "KpnI": ("GGTACC", 5, "3'"),
    "SacI": ("GAGCTC", 5, "3'"),
    "XbaI": ("TCTAGA", 1, "5'"),
    "NcoI": ("CCATGG", 1, "5'"),
    "NdeI": ("CATATG", 2, "5'"),
    "NotI": ("GCGGCCGC", 2, "5'"),
    "SmaI": ("CCCGGG", 3, "blunt"),
    "EcoRV": ("GATATC", 3, "blunt"),
    "HpaI": ("GTTAAC", 3, "blunt"),
    "MspI": ("CCGG", 1, "5'"),
    "HaeIII": ("GGCC", 2, "blunt"),
    "AluI": ("AGCT", 2, "blunt"),
    "RsaI": ("GTAC", 2, "blunt"),
    "TaqI": ("TCGA", 1, "5'"),
    "Sau3AI": ("GATC", 0, "5'"),
    "MboI": ("GATC", 0, "5'"),
    "DpnI": ("GATC", 2, "blunt"),
}


def reverse_complement(sequence: str) -> str:
    """Return the reverse complement of a DNA sequence."""
    return "".join(COMPLEMENT.get(base, "N") for base in reversed(sequence.upper()))


def complement(sequence: str) -> str:
    """Return the complement of a DNA sequence."""
    return "".join(COMPLEMENT.get(base, "N") for base in sequence.upper())


def translate(sequence: str, frame: int = 0) -> str:
    """
    Translate a DNA sequence to protein.

    Args:
        sequence: DNA sequence
        frame: Reading frame (0, 1, or 2)

    Returns:
        Protein sequence (1-letter codes)
    """
    sequence = sequence.upper()
    protein = []

    for i in range(frame, len(sequence) - 2, 3):
        codon = sequence[i : i + 3]
        amino_acid = CODON_TABLE.get(codon, "X")
        protein.append(amino_acid)

    return "".join(protein)


def gc_content(sequence: str) -> float:
    """Calculate GC content as a percentage."""
    sequence = sequence.upper()
    gc_count = sum(1 for base in sequence if base in ("G", "C"))
    return (gc_count / len(sequence)) * 100 if sequence else 0.0


def get_iupac_code(bases: set[str]) -> str:
    """Get IUPAC code for a set of bases."""
    return BASES_TO_IUPAC.get(frozenset(bases), "N")
