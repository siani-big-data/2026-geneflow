"""Persistent state for harvesting sequences."""

import json
import logging
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path

logger = logging.getLogger(__name__)

DEFAULT_STATE_FILE = Path("harvest_state.json")


@dataclass
class SpeciesProgress:
    """Progress for a single species."""
    taxon_id: int
    name: str
    target_count: int
    downloaded: int = 0
    accessions: list[str] = field(default_factory=list)
    completed: bool = False
    last_updated: str = ""


@dataclass
class KingdomProgress:
    """Progress for a kingdom."""
    name: str
    target_species: int
    species_completed: int = 0
    species_partial: int = 0
    total_sequences: int = 0
    species: dict[int, SpeciesProgress] = field(default_factory=dict)


@dataclass
class HarvestState:
    """Persistent harvest state."""
    version: str = "1.0"
    created_at: str = ""
    updated_at: str = ""
    target_sequences_per_species: int = 50
    kingdoms: dict[str, KingdomProgress] = field(default_factory=dict)
    global_stats: dict = field(default_factory=dict)

    def __post_init__(self):
        if not self.created_at:
            self.created_at = datetime.now(timezone.utc).isoformat()
        self.updated_at = datetime.now(timezone.utc).isoformat()

    @classmethod
    def load(cls, path: Path = DEFAULT_STATE_FILE) -> "HarvestState":
        """Load state from file."""
        if not path.exists():
            logger.info(f"No state file found at {path}, starting fresh")
            return cls()

        try:
            with open(path) as f:
                data = json.load(f)

            state = cls(
                version=data.get("version", "1.0"),
                created_at=data.get("created_at", ""),
                updated_at=data.get("updated_at", ""),
                target_sequences_per_species=data.get("target_sequences_per_species", 50),
                global_stats=data.get("global_stats", {}),
            )

            # Load kingdoms
            for k_name, k_data in data.get("kingdoms", {}).items():
                kingdom = KingdomProgress(
                    name=k_name,
                    target_species=k_data.get("target_species", 0),
                    species_completed=k_data.get("species_completed", 0),
                    species_partial=k_data.get("species_partial", 0),
                    total_sequences=k_data.get("total_sequences", 0),
                )

                # Load species
                for s_id, s_data in k_data.get("species", {}).items():
                    species = SpeciesProgress(
                        taxon_id=int(s_id),
                        name=s_data.get("name", ""),
                        target_count=s_data.get("target_count", 50),
                        downloaded=s_data.get("downloaded", 0),
                        accessions=s_data.get("accessions", []),
                        completed=s_data.get("completed", False),
                        last_updated=s_data.get("last_updated", ""),
                    )
                    kingdom.species[int(s_id)] = species

                state.kingdoms[k_name] = kingdom

            logger.info(f"Loaded state from {path}")
            return state

        except Exception as e:
            logger.error(f"Failed to load state: {e}")
            return cls()

    def save(self, path: Path = DEFAULT_STATE_FILE):
        """Save state to file."""
        self.updated_at = datetime.now(timezone.utc).isoformat()
        self._update_global_stats()

        data = {
            "version": self.version,
            "created_at": self.created_at,
            "updated_at": self.updated_at,
            "target_sequences_per_species": self.target_sequences_per_species,
            "global_stats": self.global_stats,
            "kingdoms": {},
        }

        for k_name, kingdom in self.kingdoms.items():
            k_data = {
                "name": kingdom.name,
                "target_species": kingdom.target_species,
                "species_completed": kingdom.species_completed,
                "species_partial": kingdom.species_partial,
                "total_sequences": kingdom.total_sequences,
                "species": {},
            }

            for s_id, species in kingdom.species.items():
                k_data["species"][str(s_id)] = {
                    "taxon_id": species.taxon_id,
                    "name": species.name,
                    "target_count": species.target_count,
                    "downloaded": species.downloaded,
                    "accessions": species.accessions,
                    "completed": species.completed,
                    "last_updated": species.last_updated,
                }

            data["kingdoms"][k_name] = k_data

        with open(path, "w") as f:
            json.dump(data, f, indent=2)

        logger.debug(f"State saved to {path}")

    def _update_global_stats(self):
        """Update global statistics."""
        total_species = 0
        completed_species = 0
        total_sequences = 0

        for kingdom in self.kingdoms.values():
            total_species += len(kingdom.species)
            completed_species += kingdom.species_completed
            total_sequences += kingdom.total_sequences

        self.global_stats = {
            "total_species": total_species,
            "completed_species": completed_species,
            "total_sequences": total_sequences,
        }

    def get_kingdom(self, name: str) -> KingdomProgress:
        """Get or create kingdom progress."""
        if name not in self.kingdoms:
            self.kingdoms[name] = KingdomProgress(name=name, target_species=0)
        return self.kingdoms[name]

    def record_species_download(
        self,
        kingdom: str,
        taxon_id: int,
        species_name: str,
        accessions: list[str],
        target_count: int,
    ):
        """Record downloaded sequences for a species."""
        k = self.get_kingdom(kingdom)

        if taxon_id not in k.species:
            k.species[taxon_id] = SpeciesProgress(
                taxon_id=taxon_id,
                name=species_name,
                target_count=target_count,
            )

        species = k.species[taxon_id]
        new_accessions = [a for a in accessions if a not in species.accessions]
        species.accessions.extend(new_accessions)
        species.downloaded = len(species.accessions)
        species.completed = species.downloaded >= species.target_count
        species.last_updated = datetime.now(timezone.utc).isoformat()

        # Update kingdom stats
        k.total_sequences = sum(s.downloaded for s in k.species.values())
        k.species_completed = sum(1 for s in k.species.values() if s.completed)
        k.species_partial = sum(
            1 for s in k.species.values() if not s.completed and s.downloaded > 0
        )

    def get_downloaded_accessions(self, kingdom: str, taxon_id: int) -> set[str]:
        """Get already downloaded accessions for a species."""
        k = self.kingdoms.get(kingdom)
        if not k:
            return set()
        species = k.species.get(taxon_id)
        if not species:
            return set()
        return set(species.accessions)

    def is_species_complete(self, kingdom: str, taxon_id: int) -> bool:
        """Check if a species has enough sequences."""
        k = self.kingdoms.get(kingdom)
        if not k:
            return False
        species = k.species.get(taxon_id)
        if not species:
            return False
        return species.completed

    def summary(self) -> str:
        """Get summary of harvest state."""
        self._update_global_stats()

        lines = [
            "=" * 60,
            "HARVEST STATE SUMMARY",
            "=" * 60,
            f"Created: {self.created_at}",
            f"Updated: {self.updated_at}",
            f"Target sequences/species: {self.target_sequences_per_species}",
            "",
            f"{'Kingdom':<15} {'Species':>10} {'Complete':>10} {'Sequences':>12}",
            "-" * 60,
        ]

        for k_name, kingdom in self.kingdoms.items():
            species_count = len(kingdom.species)
            completed = kingdom.species_completed
            total_seqs = kingdom.total_sequences
            lines.append(
                f"{k_name:<15} {species_count:>10,} {completed:>10,} {total_seqs:>12,}"
            )

        lines.extend([
            "-" * 60,
            f"{'TOTAL':<15} {self.global_stats.get('total_species', 0):>10,} "
            f"{self.global_stats.get('completed_species', 0):>10,} "
            f"{self.global_stats.get('total_sequences', 0):>12,}",
            "=" * 60,
        ])

        return "\n".join(lines)
