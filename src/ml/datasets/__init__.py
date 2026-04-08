"""Dataset classes for ML training."""

from .fastq_dataset import FastqDataset, FastqDatasetBuilder, FastqRecord

# Feature extractors
from .features import SequenceFeatureExtractor, SignalFeatureExtractor
from .hierarchical_taxonomy_dataset import HierarchicalTaxonomyDataset
from .hierarchical_taxonomy_featuREDACTED import (
    HierarchicalTaxonomyDatasetBuilder,
    HierarchicalTaxonomyDatasetConfig,
    HierarchicalTaxonomyFeatureDataset,
    PrecomputedHierarchicalTaxonomyDataset,
)

# Enhanced datasets with feature extraction
from .quality_featuREDACTED import (
    PrecomputedQualityDataset,
    QualityDatasetBuilder,
    QualityDatasetConfig,
    QualityFeatureDataset,
)
from .sequence_dataset import SequenceDataset
from .taxonomy_dataset import TaxonomyDataset
from .taxonomy_featuREDACTED import (
    PrecomputedTaxonomyDataset,
    TaxonomyDatasetBuilder,
    TaxonomyDatasetConfig,
    TaxonomyFeatureDataset,
)
from .trace_dataset import TraceDataset, TraceSample

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
