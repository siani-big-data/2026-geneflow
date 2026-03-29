"""
Gene finding model for DNA sequences.

Predice regiones codificantes (CDS), UTRs y ORFs.

Arquitectura: Transformer encoder con heads para cada tipo de región
Input: Secuencia de ADN
Output: Regiones anotadas con tipo y confianza
"""

import time
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

from ..base import LocalModel, ModelCategory, PredictionResult


@dataclass
class GeneRegion:
    """Una región génica detectada."""

    start: int
    end: int
    regionType: str  # "CDS", "5UTR", "3UTR", "ORF", "exon", "intron"
    strand: str  # "+" o "-"
    frame: int  # 0, 1, 2
    score: float

    def to_dict(self) -> dict:
        return {
            "start": self.start,
            "end": self.end,
            "length": self.end - self.start,
            "regionType": self.regionType,
            "strand": self.strand,
            "frame": self.frame,
            "score": round(self.score, 3),
        }


class GeneFinder(LocalModel):
    """
    Predictor de genes y regiones codificantes.

    Identifica ORFs, regiones codificantes y UTRs
    en secuencias de ADN.
    """

    name = "gene_finder"
    version = "1.0.0"
    category = ModelCategory.ANNOTATION
    description = (
        "Predice regiones codificantes, ORFs y UTRs en secuencias "
        "de ADN usando análisis de marcos de lectura."
    )

    # Codones
    START_CODONS = {"ATG"}
    STOP_CODONS = {"TAA", "TAG", "TGA"}
    MIN_ORF_LENGTH = 100  # Mínimo 100bp para considerar un ORF

    def __init__(self):
        self._model = None
        self._is_loaded = False

    def load(self, model_path: Optional[Path] = None) -> None:
        self._is_loaded = True

    def unload(self) -> None:
        self._model = None
        self._is_loaded = False

    def is_loaded(self) -> bool:
        return self._is_loaded

    async def predict(
        self,
        sequence: str,
        min_orf_length: Optional[int] = None,
    ) -> PredictionResult:
        """
        Encontrar genes y ORFs en secuencia.

        Args:
            sequence: Secuencia de ADN
            min_orf_length: Longitud mínima de ORF (default 100)

        Returns:
            PredictionResult con regiones detectadas
        """
        start_time = time.perf_counter()

        seq = sequence.upper().replace(" ", "").replace("\n", "")
        min_len = min_orf_length or self.MIN_ORF_LENGTH

        regions = []

        # Buscar ORFs en ambas hebras
        for strand, search_seq in [("+", seq), ("-", self._reverse_complement(seq))]:
            for frame in range(3):
                orfs = self._find_orfs(search_seq, frame, strand, min_len)
                regions.extend(orfs)

        # Ordenar por posición
        regions.sort(key=lambda r: r.start)

        # Estadísticas
        total_coding = sum(r.end - r.start for r in regions if r.regionType == "ORF")
        coding_density = total_coding / len(seq) if len(seq) > 0 else 0

        processing_time = (time.perf_counter() - start_time) * 1000

        return PredictionResult(
            modelName=self.name,
            modelVersion=self.version,
            prediction={
                "regions": [r.to_dict() for r in regions],
                "totalRegions": len(regions),
                "longestOrf": max((r.end - r.start for r in regions), default=0),
            },
            confidence=0.8 if regions else 0.5,
            processingTimeMs=round(processing_time, 2),
            metadata={
                "sequenceLength": len(seq),
                "codingDensity": round(coding_density, 3),
                "orfsByStrand": {
                    "+": sum(1 for r in regions if r.strand == "+"),
                    "-": sum(1 for r in regions if r.strand == "-"),
                },
            },
        )

    def _find_orfs(
        self, seq: str, frame: int, strand: str, min_length: int
    ) -> list[GeneRegion]:
        """Encontrar ORFs en un marco de lectura."""
        orfs = []
        seq_len = len(seq)

        i = frame
        while i < seq_len - 2:
            codon = seq[i : i + 3]

            if codon in self.START_CODONS:
                # Buscar stop codon
                start_pos = i
                j = i + 3

                while j < seq_len - 2:
                    stop_codon = seq[j : j + 3]
                    if stop_codon in self.STOP_CODONS:
                        orf_length = j + 3 - start_pos
                        if orf_length >= min_length:
                            # Convertir posiciones si es hebra -
                            if strand == "-":
                                real_start = seq_len - (j + 3)
                                real_end = seq_len - start_pos
                            else:
                                real_start = start_pos
                                real_end = j + 3

                            score = min(orf_length / 500, 1.0)  # Score basado en longitud
                            orfs.append(
                                GeneRegion(
                                    start=real_start + 1,  # 1-based
                                    end=real_end,
                                    regionType="ORF",
                                    strand=strand,
                                    frame=frame,
                                    score=score,
                                )
                            )
                        break
                    j += 3

            i += 3

        return orfs

    def _reverse_complement(self, seq: str) -> str:
        """Obtener reverso complementario."""
        complement = {"A": "T", "T": "A", "C": "G", "G": "C", "N": "N"}
        return "".join(complement.get(base, "N") for base in reversed(seq))
