"""Heuristic strategies for sequence annotation."""

import re

from ..base import ModelStrategy, StrategyResult, StrategyType


class HeuristicGeneFinderStrategy(ModelStrategy):
    """Rule-based ORF finding."""

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_genefinder"
    model_version = "1.0.0"

    START_CODONS = {"ATG"}
    STOP_CODONS = {"TAA", "TAG", "TGA"}
    MIN_ORF_LENGTH = 100

    async def execute(
        self,
        sequence: str,
        min_orf_length: int | None = None,
        **kwargs,
    ) -> StrategyResult:
        """Find ORFs in sequence."""
        seq = sequence.upper().replace(" ", "").replace("\n", "")
        min_len = min_orf_length or self.MIN_ORF_LENGTH
        orfs = []

        # Search both strands
        for strand, search_seq in [("+", seq), ("-", self._reverse_complement(seq))]:
            for frame in range(3):
                orfs.extend(self._find_orfs_in_frame(search_seq, frame, strand, min_len, len(seq)))

        orfs.sort(key=lambda x: x["start"])

        return StrategyResult(
            data={
                "regions": orfs,
                "totalRegions": len(orfs),
                "longestOrf": max((o["length"] for o in orfs), default=0),
            },
            confidence=0.8 if orfs else 0.5,
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )

    def _find_orfs_in_frame(
        self, seq: str, frame: int, strand: str, min_len: int, orig_len: int
    ) -> list[dict]:
        orfs = []
        i = frame
        while i < len(seq) - 2:
            codon = seq[i : i + 3]
            if codon in self.START_CODONS:
                start_pos = i
                j = i + 3
                while j < len(seq) - 2:
                    stop_codon = seq[j : j + 3]
                    if stop_codon in self.STOP_CODONS:
                        orf_len = j + 3 - start_pos
                        if orf_len >= min_len:
                            if strand == "-":
                                real_start = orig_len - (j + 3)
                                real_end = orig_len - start_pos
                            else:
                                real_start = start_pos
                                real_end = j + 3
                            orfs.append(
                                {
                                    "start": real_start + 1,
                                    "end": real_end,
                                    "length": orf_len,
                                    "strand": strand,
                                    "frame": frame,
                                    "type": "ORF",
                                }
                            )
                        break
                    j += 3
            i += 3
        return orfs

    def _reverse_complement(self, seq: str) -> str:
        complement = {"A": "T", "T": "A", "C": "G", "G": "C", "N": "N"}
        return "".join(complement.get(b, "N") for b in reversed(seq))


class HeuristicMotifStrategy(ModelStrategy):
    """Rule-based motif scanning."""

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_motif"
    model_version = "1.0.0"

    MOTIFS = {
        "TATA_box": r"TATA[AT]A[AT]",
        "CAAT_box": r"GG[CT]CAATCT",
        "GC_box": r"GGGCGG",
        "Kozak": r"[AG]CCATGG",
        "polyA_signal": r"AATAAA",
        "splice_donor": r"[ACGT]AGGT[AG]AGT",
        "splice_acceptor": r"[CT]{10}[ACGT]AG[GT]",
    }

    async def execute(
        self,
        sequence: str,
        motifs: list[str] | None = None,
        **kwargs,
    ) -> StrategyResult:
        """Scan sequence for regulatory motifs."""
        seq = sequence.upper()
        search_motifs = motifs or list(self.MOTIFS.keys())
        found = []

        for motif_name in search_motifs:
            if motif_name not in self.MOTIFS:
                continue
            pattern = self.MOTIFS[motif_name]
            for match in re.finditer(pattern, seq):
                found.append(
                    {
                        "motif": motif_name,
                        "start": match.start() + 1,
                        "end": match.end(),
                        "sequence": match.group(),
                        "strand": "+",
                    }
                )

        return StrategyResult(
            data={
                "motifs": found,
                "totalMotifs": len(found),
                "motifTypes": list(set(m["motif"] for m in found)),
            },
            confidence=0.85,
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )
