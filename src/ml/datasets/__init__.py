"""Dataset classes for ML training."""

from .sequence_dataset import SequenceDataset
from .taxonomy_dataset import TaxonomyDataset
from .trace_dataset import TraceDataset, TraceSample
from .fastq_dataset import FastqDataset, FastqRecord, FastqDatasetBuilder
from .hierarchical_taxonomy_dataset import HierarchicalTaxonomyDataset

# Enhanced datasets with feature extraction
from .quality_featuREDACTED import (
    QualityDatasetConfig,
    QualityFeatureDataset,
    QualityDatasetBuilder,
    PrecomputedQualityDataset,
)
from .taxonomy_featuREDACTED import (
    TaxonomyDatasetConfig,
    TaxonomyFeatureDataset,
    TaxonomyDatasetBuilder,
    PrecomputedTaxonomyDataset,
)
from .hierarchical_taxonomy_featuREDACTED import (
    HierarchicalTaxonomyDatasetConfig,
    HierarchicalTaxonomyFeatureDataset,
    HierarchicalTaxonomyDatasetBuilder,
    PrecomputedHierarchicalTaxonomyDataset,
)

# Feature extractors
from .features import SequenceFeatureExtractor, SignalFeatureExtractor

__all__ = [
    # Base datasets
    "TraceDataset",
    "TraceSample",
    "SequenceDataset",
    "TaxonomyDataset",
    "HierarchicalTaxonomyDataset",
    "FastqDataset",
    "FastqRecord",
    "FastqDatasetBuilder",
    # Enhanced datasets
    "QualityDatasetConfig",
    "QualityFeatureDataset",
    "QualityDatasetBuilder",
    "PrecomputedQualityDataset",
    "TaxonomyDatasetConfig",
    "TaxonomyFeatureDataset",
    "TaxonomyDatasetBuilder",
    "PrecomputedTaxonomyDataset",
    # Hierarchical taxonomy datasets
    "HierarchicalTaxonomyDatasetConfig",
    "HierarchicalTaxonomyFeatureDataset",
    "HierarchicalTaxonomyDatasetBuilder",
    "PrecomputedHierarchicalTaxonomyDataset",
    # Feature extractors
    "SequenceFeatureExtractor",
    "SignalFeatureExtractor",
]
