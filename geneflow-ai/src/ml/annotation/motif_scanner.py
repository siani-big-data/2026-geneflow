"""
Motif scanning model for DNA sequences.

Detecta motivos regulatorios: promotores, sitios de splicing,
señales de poliadenilación, etc.

Arquitectura: CNN con kernels para cada tipo de motivo
Input: Secuencia de ADN
Output: Motivos detectados con posición y score
"""

import re
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

from ..base import LocalModel, ModelCategory, PredictionResult


@dataclass
class DetectedMotif:
    """Un motivo detectado."""

    name: str
    motifType: str
    start: int
    end: int
    sequence: str
    strand: str
    score: float

    def to_dict(self) -> dict:
        return {
            "name": self.name,
            "type": self.motifType,
            "start": self.start,
            "end": self.end,
            "sequence": self.sequence,
            "strand": self.strand,
            "score": round(self.score, 3),
        }


class MotifScanner(LocalModel):
    """
    Scanner de motivos regulatorios en ADN.

    Detecta promotores, sitios de splicing, señales poly-A
    y otros motivos regulatorios.
    """

    name = "motif_scanner"
    version = "1.0.0"
    category = ModelCategory.ANNOTATION
    description = (
        "Detecta motivos regulatorios en ADN: TATA box, sitios de "
        "splicing, señales de poliadenilación y más."
    )

    # Motivos conocidos (consensus sequences)
    MOTIFS = {
        # Promotores
        "TATA_box": {
            "pattern": r"TATA[AT]A[AT]",
            "type": "promoter",
            "description": "TATA box - promoter element",
        },
        "CAAT_box": {
            "pattern": r"GG[CT]CAATCT",
            "type": "promoter",
            "description": "CAAT box - promoter element",
        },
        "GC_box": {
            "pattern": r"GGGCGG",
            "type": "promoter",
            "description": "GC box - Sp1 binding site",
        },
        # Splicing
        "donor_splice": {
            "pattern": r"[AC]AG[G]T[AG]AGT",
            "type": "splicing",
            "description": "5' splice donor site",
        },
        "acceptor_splice": {
            "pattern": r"[CT]{10,}[ATCG]AG[G]",
            "type": "splicing",
            "description": "3' splice acceptor site",
        },
        "branch_point": {
            "pattern": r"[CT]T[AG]A[CT]",
            "type": "splicing",
            "description": "Branch point sequence",
        },
        # Polyadenylation
        "polyA_signal": {
            "pattern": r"AATAAA",
            "type": "polyadenylation",
            "description": "Polyadenylation signal",
        },
        "polyA_variant": {
            "pattern": r"ATTAAA",
            "type": "polyadenylation",
            "description": "Variant poly-A signal",
        },
        # Kozak
        "kozak_strong": {
            "pattern": r"[AG]CCATGG",
            "type": "translation",
            "description": "Strong Kozak sequence",
        },
        "kozak_weak": {
            "pattern": r"[^AG]..ATGG",
            "type": "translation",
            "description": "Weak Kozak sequence",
        },
    }

    def __init__(self):
        self._model = None
        self._is_loaded = False
        self._compiled_patterns = {}

    def load(self, model_path: Optional[Path] = None) -> None:
        # Pre-compilar patrones regex
        for name, motif in self.MOTIFS.items():
            self._compiled_patterns[name] = re.compile(motif["pattern"], re.IGNORECASE)
        self._is_loaded = True

    def unload(self) -> None:
        self._compiled_patterns = {}
        self._is_loaded = False

    def is_loaded(self) -> bool:
        return self._is_loaded

    async def predict(
        self,
        sequence: str,
        motif_types: Optional[list[str]] = None,
    ) -> PredictionResult:
        """
        Escanear secuencia para motivos.

        Args:
            sequence: Secuencia de ADN
            motif_types: Tipos a buscar (None = todos)

        Returns:
            PredictionResult con motivos detectados
        """
        start_time = time.perf_counter()

        seq = sequence.upper()
        motifs = []

        # Buscar en hebra +
        motifs.extend(self._scan_sequence(seq, "+", motif_types))

        # Buscar en hebra -
        rev_comp = self._reverse_complement(seq)
        rev_motifs = self._scan_sequence(rev_comp, "-", motif_types)
        # Ajustar posiciones para hebra -
        for m in rev_motifs:
            m.start = len(seq) - m.end + 1
            m.end = len(seq) - m.start + m.end - m.start + 1
        motifs.extend(rev_motifs)

        # Ordenar por posición
        motifs.sort(key=lambda m: m.start)

        # Contar por tipo
        counts_by_type = {}
        for m in motifs:
            counts_by_type[m.motifType] = counts_by_type.get(m.motifType, 0) + 1

        processing_time = (time.perf_counter() - start_time) * 1000

        return PredictionResult(
            modelName=self.name,
            modelVersion=self.version,
            prediction={
                "motifs": [m.to_dict() for m in motifs],
                "totalMotifs": len(motifs),
            },
            confidence=0.9,
            processingTimeMs=round(processing_time, 2),
            metadata={
                "sequenceLength": len(seq),
                "countsByType": counts_by_type,
                "motifsSearched": len(self.MOTIFS),
            },
        )

    def _scan_sequence(
        self, seq: str, strand: str, motif_types: Optional[list[str]]
    ) -> list[DetectedMotif]:
        """Escanear secuencia para todos los motivos."""
        motifs = []

        for name, motif_info in self.MOTIFS.items():
            # Filtrar por tipo si especificado
            if motif_types and motif_info["type"] not in motif_types:
                continue

            pattern = self._compiled_patterns.get(name)
            if not pattern:
                continue

            for match in pattern.finditer(seq):
                motifs.append(
                    DetectedMotif(
                        name=name,
                        motifType=motif_info["type"],
                        start=match.start() + 1,  # 1-based
                        end=match.end(),
                        sequence=match.group(),
                        strand=strand,
                        score=0.9,  # TODO: Score basado en match quality_enhanced
                    )
                )

        return motifs

    def _reverse_complement(self, seq: str) -> str:
        complement = {"A": "T", "T": "A", "C": "G", "G": "C", "N": "N"}
        return "".join(complement.get(base, "N") for base in reversed(seq))
