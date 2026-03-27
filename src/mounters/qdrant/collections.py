"""Qdrant collection definitions for GeneFlow embeddings."""

from dataclasses import dataclass

from qdrant_client.models import Distance


@dataclass
class CollectionConfig:
    """Configuration for a Qdrant collection."""

    name: str
    vector_size: int
    distance: Distance = Distance.COSINE


SEQUENCES_COLLECTION = CollectionConfig(
    name="geneflow_sequences",
    vector_size=768,
    distance=Distance.COSINE,
)

ANNOTATIONS_COLLECTION = CollectionConfig(
    name="geneflow_annotations",
    vector_size=1536,
    distance=Distance.COSINE,
)

TRACES_COLLECTION = CollectionConfig(
    name="geneflow_traces",
    vector_size=256,
    distance=Distance.COSINE,
)

COLLECTIONS = [
    SEQUENCES_COLLECTION,
    ANNOTATIONS_COLLECTION,
    TRACES_COLLECTION,
]
