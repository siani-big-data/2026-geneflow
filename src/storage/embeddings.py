"""Embedding service for sequence similarity search.

Uses k-mer based embeddings for DNA sequences.
Optionally integrates with Qdrant for vector storage.
"""

import hashlib
from dataclasses import dataclass, field
from typing import Optional

import numpy as np
import structlog

logger = structlog.get_logger()


@dataclass
class SequenceEmbedding:
    """Embedding for a DNA sequence."""

    id: str
    sequence_hash: str
    embedding: list[float]
    metadata: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "sequenceHash": self.sequence_hash,
            "embeddingDim": len(self.embedding),
            "metadata": self.metadata,
        }


class EmbeddingService:
    """Service for generating and storing sequence embeddings.

    Uses k-mer frequency vectors as embeddings.
    For production, consider using DNA-BERT or ESM-2.
    """

    def __init__(
        self,
        k: int = 6,
        qdrant_url: str | None = None,
        collection_name: str = "sequences",
    ):
        self.k = k
        self.qdrant_url = qdrant_url
        self.collection_name = collection_name
        self._qdrant_client = None
        self._embeddings_cache: dict[str, SequenceEmbedding] = {}

        # Pre-compute all possible k-mers for consistent vector size
        self._kmer_index = self._build_kmer_index(k)
        self._embedding_dim = len(self._kmer_index)

        if qdrant_url:
            self._init_qdrant()

    def _build_kmer_index(self, k: int) -> dict[str, int]:
        """Build index mapping k-mers to vector positions."""
        bases = "ACGT"
        kmers = {}
        idx = 0

        def generate(prefix: str, length: int):
            nonlocal idx
            if length == 0:
                kmers[prefix] = idx
                idx += 1
                return
            for base in bases:
                generate(prefix + base, length - 1)

        generate("", k)
        return kmers

    def _init_qdrant(self) -> None:
        """Initialize Qdrant client."""
        try:
            from qdrant_client import QdrantClient
            from qdrant_client.models import Distance, VectorParams

            self._qdrant_client = QdrantClient(url=self.qdrant_url)

            # Create collection if not exists
            collections = self._qdrant_client.get_collections().collections
            if self.collection_name not in [c.name for c in collections]:
                self._qdrant_client.create_collection(
                    collection_name=self.collection_name,
                    vectors_config=VectorParams(
                        size=self._embedding_dim,
                        distance=Distance.COSINE,
                    ),
                )
                logger.info(
                    "qdrant_collection_created",
                    collection=self.collection_name,
                    dim=self._embedding_dim,
                )

        except ImportError:
            logger.warning("qdrant_client not installed, using in-memory storage")
        except Exception as e:
            logger.error("qdrant_init_error", error=str(e))

    @property
    def is_qdrant_available(self) -> bool:
        """Check if Qdrant is available."""
        return self._qdrant_client is not None

    def generate_embedding(
        self,
        sequence: str,
        normalize: bool = True,
    ) -> list[float]:
        """Generate k-mer frequency embedding for a sequence.

        Args:
            sequence: DNA sequence
            normalize: Whether to L2-normalize the vector

        Returns:
            Embedding vector
        """
        seq = sequence.upper().replace(" ", "").replace("\n", "")

        # Count k-mers
        counts = np.zeros(self._embedding_dim, dtype=np.float32)

        for i in range(len(seq) - self.k + 1):
            kmer = seq[i : i + self.k]
            if kmer in self._kmer_index:
                counts[self._kmer_index[kmer]] += 1

        # Normalize
        if normalize:
            norm = np.linalg.norm(counts)
            if norm > 0:
                counts = counts / norm

        return counts.tolist()

    def _hash_sequence(self, sequence: str) -> str:
        """Generate hash for sequence."""
        clean = sequence.upper().replace(" ", "").replace("\n", "")
        return hashlib.sha256(clean.encode()).hexdigest()[:16]

    async def embed_and_store(
        self,
        id: str,
        sequence: str,
        metadata: dict | None = None,
    ) -> SequenceEmbedding:
        """Generate embedding and store it.

        Args:
            id: Unique identifier for the sequence
            sequence: DNA sequence
            metadata: Additional metadata

        Returns:
            SequenceEmbedding object
        """
        embedding = self.generate_embedding(sequence)
        seq_hash = self._hash_sequence(sequence)

        seq_embedding = SequenceEmbedding(
            id=id,
            sequence_hash=seq_hash,
            embedding=embedding,
            metadata=metadata or {},
        )

        # Store in cache
        self._embeddings_cache[id] = seq_embedding

        # Store in Qdrant if available
        if self._qdrant_client:
            try:
                from qdrant_client.models import PointStruct

                self._qdrant_client.upsert(
                    collection_name=self.collection_name,
                    points=[
                        PointStruct(
                            id=hash(id) % (2**63),  # Convert to int
                            vector=embedding,
                            payload={
                                "id": id,
                                "sequence_hash": seq_hash,
                                **(metadata or {}),
                            },
                        )
                    ],
                )
            except Exception as e:
                logger.error("qdrant_upsert_error", error=str(e))

        return seq_embedding

    async def search_similar(
        self,
        sequence: str,
        limit: int = 10,
        scoREDACTED: float = 0.0,
    ) -> list[dict]:
        """Find similar sequences.

        Args:
            sequence: Query sequence
            limit: Maximum results
            scoREDACTED: Minimum similarity score (0-1)

        Returns:
            List of similar sequences with scores
        """
        query_embedding = self.generate_embedding(sequence)

        if self._qdrant_client:
            try:
                results = self._qdrant_client.search(
                    collection_name=self.collection_name,
                    query_vector=query_embedding,
                    limit=limit,
                    scoREDACTED=scoREDACTED,
                )

                return [
                    {
                        "id": r.payload.get("id", ""),
                        "score": r.score,
                        "metadata": {k: v for k, v in r.payload.items() if k != "id"},
                    }
                    for r in results
                ]

            except Exception as e:
                logger.error("qdrant_search_error", error=str(e))

        # Fallback to in-memory search
        return self._search_in_memory(query_embedding, limit, scoREDACTED)

    def _search_in_memory(
        self,
        query_embedding: list[float],
        limit: int,
        scoREDACTED: float,
    ) -> list[dict]:
        """Search embeddings in memory."""
        query_vec = np.array(query_embedding)
        results = []

        for emb in self._embeddings_cache.values():
            emb_vec = np.array(emb.embedding)
            # Cosine similarity
            score = float(np.dot(query_vec, emb_vec))

            if score >= scoREDACTED:
                results.append(
                    {
                        "id": emb.id,
                        "score": score,
                        "metadata": emb.metadata,
                    }
                )

        # Sort by score descending
        results.sort(key=lambda x: x["score"], reverse=True)
        return results[:limit]

    async def get_embedding(self, id: str) -> Optional[SequenceEmbedding]:
        """Get embedding by ID."""
        return self._embeddings_cache.get(id)

    async def delete_embedding(self, id: str) -> bool:
        """Delete embedding by ID."""
        if id in self._embeddings_cache:
            del self._embeddings_cache[id]

            if self._qdrant_client:
                try:
                    self._qdrant_client.delete(
                        collection_name=self.collection_name,
                        points_selector=[hash(id) % (2**63)],
                    )
                except Exception as e:
                    logger.error("qdrant_delete_error", error=str(e))

            return True
        return False

    def get_metrics(self) -> dict:
        """Get service metrics."""
        return {
            "embeddingDim": self._embedding_dim,
            "kmerSize": self.k,
            "cachedEmbeddings": len(self._embeddings_cache),
            "qdrantAvailable": self.is_qdrant_available,
        }
