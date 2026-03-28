"""Biological constants and reference tables for GeneFlow Analysis Worker."""

BASES = {'A', 'C', 'G', 'T'}

# =============================================================================
# IUPAC Nucleotide Codes
# =============================================================================

# IUPAC ambiguity codes: code -> possible bases
IUPAC_CODES: dict[str, set[str]] = {
    "A": {"A"},
    "C": {"C"},
    "G": {"G"},
    "T": {"T"},
    "R": {"A", "G"},        # puRine
    "Y": {"C", "T"},        # pYrimidine
    "S": {"G", "C"},        # Strong (3 H-bonds)
    "W": {"A", "T"},        # Weak (2 H-bonds)
    "K": {"G", "T"},        # Keto
    "M": {"A", "C"},        # aMino
    "B": {"C", "G", "T"},   # not A
    "D": {"A", "G", "T"},   # not C
    "H": {"A", "C", "T"},   # not G
    "V": {"A", "C", "G"},   # not T
    "N": {"A", "C", "G", "T"},  # aNy
}

# Reverse lookup: frozenset of bases -> IUPAC code
BASES_TO_IUPAC: dict[frozenset[str], str] = {
    frozenset(bases): code for code, bases in IUPAC_CODES.items()
}


# =============================================================================
# Complement
# =============================================================================

# DNA complement map
COMPLEMENT: dict[str, str] = {
    "A": "T",
    "T": "A",
    "C": "G",
    "G": "C",

    # IUPAC ambiguity codes
    "R": "Y",  # A|G -> T|C
    "Y": "R",  # C|T -> G|A
    "S": "S",  # G|C -> C|G
    "W": "W",  # A|T -> T|A
    "K": "M",  # G|T -> C|A
    "M": "K",  # A|C -> T|G
    "B": "V",  # C|G|T -> G|C|A
    "D": "H",  # A|G|T -> T|C|A
    "H": "D",  # A|C|T -> T|G|A
    "V": "B",  # A|C|G -> T|G|C
    "N": "N",  # any -> any
}


# =============================================================================
# Codon Table (Standard Genetic Code)
# =============================================================================

# Standard codon table (NCBI transl_table=1)
CODON_TABLE: dict[str, str] = {
    # Phenylalanine (F)
    "TTT": "F", "TTC": "F",
    # Leucine (L)
    "TTA": "L", "TTG": "L", "CTT": "L", "CTC": "L", "CTA": "L", "CTG": "L",
    # Isoleucine (I)
    "ATT": "I", "ATC": "I", "ATA": "I",
    # Methionine (M) - START
    "ATG": "M",
    # Valine (V)
    "GTT": "V", "GTC": "V", "GTA": "V", "GTG": "V",
    # Serine (S)
    "TCT": "S", "TCC": "S", "TCA": "S", "TCG": "S", "AGT": "S", "AGC": "S",
    # Proline (P)
    "CCT": "P", "CCC": "P", "CCA": "P", "CCG": "P",
    # Threonine (T)
    "ACT": "T", "ACC": "T", "ACA": "T", "ACG": "T",
    # Alanine (A)
    "GCT": "A", "GCC": "A", "GCA": "A", "GCG": "A",
    # Tyrosine (Y)
    "TAT": "Y", "TAC": "Y",
    # STOP (*)
    "TAA": "*", "TAG": "*", "TGA": "*",
    # Histidine (H)
    "CAT": "H", "CAC": "H",
    # Glutamine (Q)
    "CAA": "Q", "CAG": "Q",
    # Asparagine (N)
    "AAT": "N", "AAC": "N",
    # Lysine (K)
    "AAA": "K", "AAG": "K",
    # Aspartic acid (D)
    "GAT": "D", "GAC": "D",
    # Glutamic acid (E)
    "GAA": "E", "GAG": "E",
    # Cysteine (C)
    "TGT": "C", "TGC": "C",
    # Tryptophan (W)
    "TGG": "W",
    # Arginine (R)
    "CGT": "R", "CGC": "R", "CGA": "R", "CGG": "R", "AGA": "R", "AGG": "R",
    # Glycine (G)
    "GGT": "G", "GGC": "G", "GGA": "G", "GGG": "G",
}

# Start codons
START_CODONS = {"ATG"}

# Stop codons
STOP_CODONS = {"TAA", "TAG", "TGA"}


# =============================================================================
# Amino Acids
# =============================================================================

# Amino acid info: 1-letter -> (3-letter, full name, properties)
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

# 3-letter to 1-letter lookup
THREE_TO_ONE: dict[str, str] = {v[0]: k for k, v in AMINO_ACIDS.items()}

# =============================================================================
# Restriction Enzymes
# =============================================================================

# Common restriction enzymes: name -> (recognition_sequence, cut_position, overhang_type)
# cut_position is relative to the start of the recognition sequence (0-indexed)
# overhang_type: "5'" = 5' overhang, "3'" = 3' overhang, "blunt" = blunt end
RESTRICTION_ENZYMES: dict[str, tuple[str, int, str]] = {
    # 6-cutters (common)
    "EcoRI": ("GAATTC", 1, "5'"),      # G|AATTC
    "BamHI": ("GGATCC", 1, "5'"),      # G|GATCC
    "HindIII": ("AAGCTT", 1, "5'"),    # A|AGCTT
    "XhoI": ("CTCGAG", 1, "5'"),       # C|TCGAG
    "SalI": ("GTCGAC", 1, "5'"),       # G|TCGAC
    "PstI": ("CTGCAG", 5, "3'"),       # CTGCA|G
    "SphI": ("GCATGC", 5, "3'"),       # GCATG|C
    "KpnI": ("GGTACC", 5, "3'"),       # GGTAC|C
    "SacI": ("GAGCTC", 5, "3'"),       # GAGCT|C
    "XbaI": ("TCTAGA", 1, "5'"),       # T|CTAGA
    "NcoI": ("CCATGG", 1, "5'"),       # C|CATGG
    "NdeI": ("CATATG", 2, "5'"),       # CA|TATG
    "NotI": ("GCGGCCGC", 2, "5'"),     # GC|GGCCGC (8-cutter, rare)
    "SmaI": ("CCCGGG", 3, "blunt"),    # CCC|GGG
    "EcoRV": ("GATATC", 3, "blunt"),   # GAT|ATC
    "HpaI": ("GTTAAC", 3, "blunt"),    # GTT|AAC
    # 4-cutters (frequent)
    "MspI": ("CCGG", 1, "5'"),         # C|CGG
    "HaeIII": ("GGCC", 2, "blunt"),    # GG|CC
    "AluI": ("AGCT", 2, "blunt"),      # AG|CT
    "RsaI": ("GTAC", 2, "blunt"),      # GT|AC
    "TaqI": ("TCGA", 1, "5'"),         # T|CGA
    "Sau3AI": ("GATC", 0, "5'"),       # |GATC (cuts before)
    "MboI": ("GATC", 0, "5'"),         # |GATC
    "DpnI": ("GATC", 2, "blunt"),      # GA|TC (methylation-sensitive)
}


# =============================================================================
# Utility Functions
# =============================================================================

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
        codon = sequence[i:i + 3]
        amino_acid = CODON_TABLE.get(codon, "X")  # X for unknown
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
