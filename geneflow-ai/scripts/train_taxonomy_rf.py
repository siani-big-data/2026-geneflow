#!/usr/bin/env python3
"""
Train a Random Forest classifier for taxonomy prediction using sequence features.

Uses compositional and structural features extracted from DNA sequences:
- GC/AT content, skew metrics
- Dinucleotide and trinucleotide frequencies (genomic signatures)
- Sequence complexity and entropy
- Homopolymer statistics

Supports GPU acceleration via cuML (RAPIDS) when available.

Usage:
    # GPU (cuML) - automatic if available
    uv run python scripts/train_taxonomy_rf.py --level kingdom

    # Force CPU (sklearn)
    uv run python scripts/train_taxonomy_rf.py --level kingdom --cpu

    # With options
    uv run python scripts/train_taxonomy_rf.py --level phylum --n-estimators 200
    uv run python scripts/train_taxonomy_rf.py --level genus --max-samples 10000
"""

import argparse
import gzip
import json
import sys
import time
from collections import Counter
from datetime import datetime
from pathlib import Path

import numpy as np

# Add project root to path
sys.path.insert(0, str(Path(__file__).parent.parent))

# =============================================================================
# GPU Detection
# =============================================================================

CUML_AVAILABLE = False
XGBOOST_GPU_AVAILABLE = False
GPU_NAME = None

# Try to get GPU name
try:
    import torch
    if torch.cuda.is_available():
        GPU_NAME = torch.cuda.get_device_name(0)
except ImportError:
    pass

# Try cuML first (Linux only)
try:
    from cuml.ensemble import RandomForestClassifier as cuMLRandomForestClassifier
    CUML_AVAILABLE = True
except ImportError:
    pass

# Try XGBoost with GPU (works on Windows)
try:
    import xgboost as xgb
    # Check if GPU is available for XGBoost
    if GPU_NAME:
        XGBOOST_GPU_AVAILABLE = True
except ImportError:
    pass

from src.ml.datasets.features.sequence_features import (  # noqa: E402
    SequenceFeatureExtractor,
    SequenceFeatures,
)

# =============================================================================
# Data Loading
# =============================================================================

def load_sequence(fasta_path: Path) -> str:
    """Load first sequence from FASTA file."""
    sequences = load_sequences(fasta_path)
    return sequences[0] if sequences else ""


def load_sequences(fasta_path: Path) -> list[str]:
    """Load all sequences from FASTA file."""
    try:
        if fasta_path.suffix == ".gz":
            with gzip.open(fasta_path, "rt") as f:
                lines = f.readlines()
        else:
            with open(fasta_path, "r") as f:
                lines = f.readlines()
    except Exception:
        return []

    sequences = []
    current_seq = []

    for line in lines:
        line = line.strip()
        if line.startswith(">"):
            if current_seq:
                sequences.append("".join(current_seq))
                current_seq = []
        else:
            current_seq.append(line)

    if current_seq:
        sequences.append("".join(current_seq))

    return sequences


# Kingdom normalization map
KINGDOM_NORMALIZE = {
    # Animals
    "metazoa": "animalia",
    "animalia": "animalia",
    # Plants
    "viridiplantae": "plantae",
    "plantae": "plantae",
    "streptophyta": "plantae",
    # Fungi
    "fungi": "fungi",
    # Protists
    "protista": "protista",
    "protozoa": "protista",
    "chromista": "protista",
    # Monera (Bacteria + Archaea in classic 5-kingdom)
    "monera": "monera",
    "bacteria": "monera",
    "eubacteria": "monera",
    "pseudomonadati": "monera",  # Bacterial phylum
    "bacillati": "monera",  # Bacterial phylum
    "archaea": "monera",  # In 5-kingdom, archaea is part of monera
    "methanobacteriati": "monera",  # Archaeal phylum
    "thermoproteati": "monera",
}


def load_samples(
    data_dir: Path,
    level: str,
    max_samples: int = 0,
    min_seq_length: int = 100,
    expand_sequences: bool = False,
) -> list[dict]:
    """Load samples from curated datalake.

    Args:
        data_dir: Root directory (e.g., datalake/curated)
        level: Taxonomy level (kingdom, phylum, class, order, family, genus)
        max_samples: Maximum samples to load (0 = unlimited)
        min_seq_length: Minimum sequence length
        expand_sequences: If True, create one sample per sequence in FASTA

    Returns:
        List of sample dictionaries
    """
    samples = []
    class_counts: dict[str, int] = {}

    json_files = list(data_dir.rglob("*.json"))
    print(f"Found {len(json_files)} metadata files")

    for i, json_path in enumerate(json_files):
        if (i + 1) % 10000 == 0:
            print(f"  Scanning {i + 1}/{len(json_files)} files...")

        try:
            with open(json_path, "r") as f:
                metadata = json.load(f)
        except Exception:
            continue

        # Get taxonomy
        taxonomy = metadata.get("taxonomy", {})
        label = taxonomy.get(level, "")

        if not label:
            if not label:
                continue

        label = label.lower()

        # Normalize kingdom labels
        if level == "kingdom":
            label = KINGDOM_NORMALIZE.get(label, None)
            if not label:
                continue  # Skip unknown kingdoms

        # Find FASTA file
        fasta_path = json_path.with_suffix(".fasta.gz")
        if not fasta_path.exists():
            fasta_path = json_path.with_suffix(".fasta")
            if not fasta_path.exists():
                continue

        if expand_sequences:
            # Load all sequences from FASTA
            sequences = load_sequences(fasta_path)
            for seq in sequences:
                if len(seq) >= min_seq_length:
                    samples.append({
                        "sequence": seq,
                        "label": label,
                        "taxonomy": taxonomy,
                    })
                    class_counts[label] = class_counts.get(label, 0) + 1

                    if max_samples > 0 and len(samples) >= max_samples:
                        break
        else:
            # Check sequence length
            total_length = metadata.get("total_length", 0)
            if total_length < min_seq_length:
                continue

            samples.append({
                "fasta_path": fasta_path,
                "label": label,
                "taxonomy": taxonomy,
            })
            class_counts[label] = class_counts.get(label, 0) + 1

        if max_samples > 0 and len(samples) >= max_samples:
            break

    print(f"Loaded {len(samples)} samples across {len(class_counts)} classes")
    for label, count in sorted(class_counts.items(), key=lambda x: -x[1])[:10]:
        print(f"  {label}: {count}")
    if len(class_counts) > 10:
        print(f"  ... and {len(class_counts) - 10} more classes")

    return samples


def get_cache_path(
    data_dir: Path,
    level: str,
    include_kmers: bool,
    include_tetramers: bool = False,
) -> Path:
    """Get cache file path for extracted features."""
    cache_dir = Path("checkpoints/taxonomy_rf/cache")
    cache_dir.mkdir(parents=True, exist_ok=True)
    kmers_str = "kmers" if include_kmers else "nokmers"
    tetra_str = "_tetra" if include_tetramers else ""
    return cache_dir / f"features_{level}_{kmers_str}{tetra_str}.npz"


def extract_features(
    samples: list[dict],
    extractor: SequenceFeatureExtractor,
    include_kmers: bool = True,
    include_tetramers: bool = False,
    cache_path: Path | None = None,
) -> tuple[np.ndarray, np.ndarray, list[str], dict[str, int]]:
    """Extract features from all samples.

    Returns:
        X: Feature matrix (n_samples, n_features)
        y: Labels (n_samples,)
        class_names: List of class names
        class_to_idx: Mapping from class name to index
    """
    # Try to load from cache
    if cache_path and cache_path.exists():
        print(f"\nLoading cached features from {cache_path}")
        data = np.load(cache_path, allow_pickle=True)
        X = data["X"]
        y = data["y"]
        class_names = data["class_names"].tolist()
        class_to_idx = {name: idx for idx, name in enumerate(class_names)}
        print(f"  Loaded {len(X)} samples, {len(class_names)} classes")
        return X, y, class_names, class_to_idx

    features_list = []
    labels_list = []

    # Build class mapping
    unique_labels = sorted(set(s["label"] for s in samples))
    class_to_idx = {label: idx for idx, label in enumerate(unique_labels)}

    print(f"\nExtracting features from {len(samples)} sequences...")
    start_time = time.time()

    for i, sample in enumerate(samples):
        if (i + 1) % 1000 == 0:
            elapsed = time.time() - start_time
            rate = (i + 1) / elapsed
            print(f"  Processed {i + 1}/{len(samples)} ({rate:.1f} seq/s)")

        # Get sequence (either pre-loaded or from file)
        if "sequence" in sample:
            sequence = sample["sequence"]
        else:
            sequence = load_sequence(sample["fasta_path"])

        if len(sequence) < 100:
            continue

        # Extract features
        feat = extractor.extract(sequence)
        feat_array = feat.to_array(
            include_kmers=include_kmers,
            include_codons=False,
            include_tetramers=include_tetramers,
        )

        features_list.append(feat_array)
        labels_list.append(class_to_idx[sample["label"]])

    elapsed = time.time() - start_time
    print(f"  Extracted {len(features_list)} feature vectors in {elapsed:.1f}s")

    X = np.stack(features_list)
    y = np.array(labels_list)

    # Save to cache
    if cache_path:
        print(f"  Saving cache to {cache_path}")
        np.savez_compressed(
            cache_path,
            X=X,
            y=y,
            class_names=np.array(unique_labels),
        )

    return X, y, unique_labels, class_to_idx


# =============================================================================
# Training
# =============================================================================

def train_random_forest(
    X_train: np.ndarray,
    y_train: np.ndarray,
    n_estimators: int = 100,
    max_depth: int | None = None,
    min_samples_leaf: int = 2,
    class_weight: str = "balanced",
    n_jobs: int = -1,
    random_state: int = 42,
    use_gpu: bool = True,
):
    """Train Random Forest classifier.

    Uses (in order of preference):
    1. cuML (GPU) - Linux only
    2. XGBoost (GPU) - Windows/Linux
    3. sklearn (CPU) - Fallback
    """
    print("\nTraining Random Forest...")
    print(f"  n_estimators: {n_estimators}")
    print(f"  max_depth: {max_depth}")
    print(f"  min_samples_leaf: {min_samples_leaf}")

    start_time = time.time()

    # Decide backend
    use_cuml = CUML_AVAILABLE and use_gpu
    use_xgb_gpu = XGBOOST_GPU_AVAILABLE and use_gpu and not use_cuml
    backend = "sklearn_cpu"

    if use_cuml:
        backend = "cuml_gpu"
        print(f"  Backend: cuML (GPU: {GPU_NAME})")

        # cuML requires float32 and int32
        X_gpu = X_train.astype(np.float32)
        y_gpu = y_train.astype(np.int32)

        clf = cuMLRandomForestClassifier(
            n_estimators=n_estimators,
            max_depth=max_depth if max_depth else 16,
            min_samples_leaf=min_samples_leaf,
            random_state=random_state,
            n_streams=4,
        )

        clf.fit(X_gpu, y_gpu)

    elif use_xgb_gpu:
        backend = "xgboost_gpu"
        print(f"  Backend: XGBoost (GPU: {GPU_NAME})")

        # Compute sample weights for class balancing
        class_counts = np.bincount(y_train)
        total = len(y_train)
        n_classes = len(class_counts)
        class_weights = total / (n_classes * class_counts)
        sample_weights = class_weights[y_train]
        print(f"  Using sample weights for class balancing")

        # XGBoost Random Forest mode with improved params
        clf = xgb.XGBRFClassifier(
            n_estimators=n_estimators,
            max_depth=max_depth if max_depth else 12,  # Deeper trees
            min_child_weight=min_samples_leaf,
            random_state=random_state,
            tree_method="hist",
            device="cuda",
            n_jobs=-1,
            verbosity=1,
            colsample_bynode=0.8,  # Feature sampling per split
            subsample=0.8,  # Row sampling per tree
            reg_alpha=0.1,  # L1 regularization
            reg_lambda=1.0,  # L2 regularization
        )

        clf.fit(X_train, y_train, sample_weight=sample_weights)

    else:
        backend = "sklearn_cpu"
        from sklearn.ensemble import RandomForestClassifier

        print("  Backend: sklearn (CPU)")
        print(f"  class_weight: {class_weight}")

        clf = RandomForestClassifier(
            n_estimators=n_estimators,
            max_depth=max_depth,
            min_samples_leaf=min_samples_leaf,
            class_weight=class_weight,
            n_jobs=n_jobs,
            random_state=random_state,
            verbose=1,
        )

        clf.fit(X_train, y_train)

    elapsed = time.time() - start_time
    print(f"  Training completed in {elapsed:.1f}s")

    return clf, backend


def evaluate(
    clf,
    X: np.ndarray,
    y: np.ndarray,
    class_names: list[str],
    split_name: str = "Test",
    backend: str = "sklearn_cpu",
) -> dict:
    """Evaluate classifier and return metrics."""
    from sklearn.metrics import (
        accuracy_score,
        classification_report,
        top_k_accuracy_score,
    )

    print(f"\n{'='*60}")
    print(f"{split_name} Evaluation")
    print(f"{'='*60}")

    if backend == "cuml_gpu":
        # cuML requires float32
        X_eval = X.astype(np.float32)
        y_pred = clf.predict(X_eval)
        y_proba = clf.predict_proba(X_eval)

        # Convert cupy arrays to numpy if needed
        if hasattr(y_pred, 'get'):
            y_pred = y_pred.get()
        if hasattr(y_proba, 'get'):
            y_proba = y_proba.get()
    else:
        y_pred = clf.predict(X)
        y_proba = clf.predict_proba(X)

    accuracy = accuracy_score(y, y_pred)
    print(f"Accuracy: {accuracy:.4f} ({accuracy*100:.2f}%)")

    # Top-k accuracy (if more than k classes)
    n_classes = len(class_names)
    for k in [3, 5]:
        if n_classes > k:
            top_k_acc = top_k_accuracy_score(y, y_proba, k=k)
            print(f"Top-{k} Accuracy: {top_k_acc:.4f} ({top_k_acc*100:.2f}%)")

    # Per-class report
    print("\nClassification Report:")
    print(classification_report(
        y, y_pred,
        target_names=class_names,
        zero_division=0,
    ))

    return {
        "accuracy": accuracy,
        "predictions": y_pred,
        "probabilities": y_proba,
    }


def print_featuREDACTED(
    clf,
    featuREDACTED: list[str],
    top_n: int = 20,
    backend: str = "sklearn_cpu",
):
    """Print top feature importances."""
    importances = clf.featuREDACTED

    # Convert cupy to numpy if needed
    if hasattr(importances, 'get'):
        importances = importances.get()

    indices = np.argsort(importances)[::-1]

    print(f"\n{'='*60}")
    print(f"Top {top_n} Feature Importances")
    print(f"{'='*60}")

    for i in range(min(top_n, len(indices))):
        idx = indices[i]
        print(f"  {i+1:2d}. {featuREDACTED[idx]:25s}: {importances[idx]:.4f}")


# =============================================================================
# Main
# =============================================================================

def parse_args():
    parser = argparse.ArgumentParser(
        description="Train Random Forest for taxonomy classification"
    )

    parser.add_argument(
        "--data-dir",
        type=str,
        default="datalake/curated/curated",
        help="Curated datalake directory",
    )
    parser.add_argument(
        "--level",
        type=str,
        default="kingdom",
        choices=["kingdom", "phylum", "class", "order", "family", "genus"],
        help="Taxonomy level to classify",
    )
    parser.add_argument(
        "--max-samples",
        type=int,
        default=0,
        help="Maximum samples to load (0 = unlimited)",
    )
    parser.add_argument(
        "--val-ratio",
        type=float,
        default=0.15,
        help="Validation set ratio",
    )
    parser.add_argument(
        "--n-estimators",
        type=int,
        default=300,
        help="Number of trees in the forest (default: 300)",
    )
    parser.add_argument(
        "--max-depth",
        type=int,
        default=None,
        help="Maximum tree depth (None = unlimited)",
    )
    parser.add_argument(
        "--min-samples-leaf",
        type=int,
        default=2,
        help="Minimum samples per leaf",
    )
    parser.add_argument(
        "--no-kmers",
        action="stoREDACTED",
        help="Exclude k-mer features (use only basic features)",
    )
    parser.add_argument(
        "--tetramers",
        action="stoREDACTED",
        help="Include 4-mer (tetranucleotide) features (+256 features)",
    )
    parser.add_argument(
        "--no-cache",
        action="stoREDACTED",
        help="Force re-extraction of features (ignore cache)",
    )
    parser.add_argument(
        "--checkpoint-dir",
        type=str,
        default="checkpoints/taxonomy_rf",
        help="Checkpoint directory",
    )
    parser.add_argument(
        "--seed",
        type=int,
        default=42,
        help="Random seed",
    )
    parser.add_argument(
        "--expand-sequences",
        action="stoREDACTED",
        help="Use all sequences from FASTAs (not just one per file)",
    )
    parser.add_argument(
        "--cpu",
        action="stoREDACTED",
        help="Force CPU training (sklearn) even if GPU is available",
    )

    return parser.parse_args()


def main():
    args = parse_args()

    print("="*60)
    print("Random Forest Taxonomy Classifier")
    print("="*60)

    # GPU info
    use_gpu = not args.cpu
    gpu_available = CUML_AVAILABLE or XGBOOST_GPU_AVAILABLE

    if GPU_NAME:
        print(f"GPU detected: {GPU_NAME}")
    else:
        print("GPU detected: No")

    if gpu_available:
        if CUML_AVAILABLE:
            print("GPU backend: cuML (RAPIDS)")
        elif XGBOOST_GPU_AVAILABLE:
            print("GPU backend: XGBoost")

        if args.cpu:
            print("GPU training: Disabled (--cpu flag)")
        else:
            print("GPU training: Enabled")
    else:
        print("GPU backend: None available")
        print("  Install XGBoost for GPU support: uv add xgboost")

    print()
    print(f"Data directory: {args.data_dir}")
    print(f"Classification level: {args.level}")
    print(f"Include k-mers: {not args.no_kmers}")
    print(f"Include 4-mers: {args.tetramers}")
    print(f"Validation ratio: {args.val_ratio}")

    # Set random seed
    np.random.seed(args.seed)

    # Load samples
    data_dir = Path(args.data_dir)
    if not data_dir.exists():
        print(f"ERROR: Data directory {data_dir} does not exist")
        return 1

    samples = load_samples(
        data_dir,
        level=args.level,
        max_samples=args.max_samples,
        expand_sequences=args.expand_sequences,
    )

    if len(samples) < 10:
        print("ERROR: Not enough samples loaded")
        return 1

    # Initialize feature extractor
    include_kmers = not args.no_kmers
    include_tetramers = args.tetramers

    extractor = SequenceFeatureExtractor(
        window_size=100,
        compute_codons=False,
        compute_tetramers=include_tetramers,
    )

    # Extract features (with caching)
    cache_path = None
    if not args.no_cache:
        cache_path = get_cache_path(
            data_dir, args.level, include_kmers, include_tetramers
        )

    X, y, class_names, class_to_idx = extract_features(
        samples,
        extractor,
        include_kmers=include_kmers,
        include_tetramers=include_tetramers,
        cache_path=cache_path,
    )

    print(f"\nDataset shape: {X.shape}")
    print(f"Number of classes: {len(class_names)}")

    # Filter classes with too few samples (need at least 2 for stratified split)
    min_samples = max(2, int(1 / args.val_ratio) + 1)  # Need enough for both splits
    class_counts = Counter(y)
    valid_classes = {c for c, count in class_counts.items() if count >= min_samples}

    if len(valid_classes) < len(class_names):
        # Filter samples
        mask = np.array([yi in valid_classes for yi in y])
        X = X[mask]
        y = y[mask]

        # Remap class indices
        old_to_new = {old: new for new, old in enumerate(sorted(valid_classes))}
        y = np.array([old_to_new[yi] for yi in y])
        class_names = [class_names[old] for old in sorted(valid_classes)]
        {name: idx for idx, name in enumerate(class_names)}

        print(f"Filtered to classes with >= {min_samples} samples:")
        print(f"  Remaining classes: {len(class_names)}")
        print(f"  Remaining samples: {len(X)}")

    # Get feature names
    featuREDACTED = SequenceFeatures.featuREDACTED(
        include_kmers=include_kmers,
        include_codons=False,
    )
    print(f"Number of features: {len(featuREDACTED)}")

    # Split data (stratified)
    from sklearn.model_selection import train_test_split

    X_train, X_val, y_train, y_val = train_test_split(
        X, y,
        test_size=args.val_ratio,
        random_state=args.seed,
        stratify=y,
    )

    print(f"\nTrain samples: {len(X_train)}")
    print(f"Val samples: {len(X_val)}")

    # Train model
    clf, backend = train_random_forest(
        X_train, y_train,
        n_estimators=args.n_estimators,
        max_depth=args.max_depth,
        min_samples_leaf=args.min_samples_leaf,
        random_state=args.seed,
        use_gpu=use_gpu,
    )

    # Evaluate
    train_metrics = evaluate(clf, X_train, y_train, class_names, "Train", backend=backend)
    val_metrics = evaluate(clf, X_val, y_val, class_names, "Validation", backend=backend)

    # Feature importance
    print_featuREDACTED(clf, featuREDACTED, top_n=25, backend=backend)

    # Get feature importance dict
    importances = clf.featuREDACTED
    if hasattr(importances, 'get'):
        importances = importances.get()
    featuREDACTED = dict(zip(featuREDACTED, importances.tolist()))

    # Compute class distribution
    class_distribution = {class_names[i]: int(c) for i, c in Counter(y).items()}

    # Compute confusion matrix and detailed metrics
    from sklearn.metrics import confusion_matrix as compute_cm
    train_metrics["predictions"]  # From train split for consistency check
    y_val_pred_actual = val_metrics["predictions"]
    compute_cm(y_val, y_val_pred_actual)

    # Import unified results system
    from src.ml.training import (
        DatasetInfo,
        HardwareInfo,
        PaperFigures,
        Predictions,
        ResultsWriter,
        TrainingResult,
        compute_classification_result,
    )

    # Compute detailed metrics
    train_detailed, train_per_class, _ = compute_classification_result(
        y_train, train_metrics["predictions"],
        train_metrics["probabilities"], class_names
    )
    val_detailed, val_per_class, val_cm = compute_classification_result(
        y_val, val_metrics["predictions"],
        val_metrics["probabilities"], class_names
    )

    # Build TrainingResult
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    run_id = f"{args.level}_{timestamp}"

    result = TrainingResult(
        run_id=run_id,
        model_name=f"TaxonomyRF_{args.level}",
        model_type="RandomForest",
        dataset=DatasetInfo(
            name=f"taxonomy_{args.level}",
            task="classification",
            train_samples=len(X_train),
            val_samples=len(X_val),
            n_features=len(featuREDACTED),
            n_classes=len(class_names),
            class_names=class_names,
            class_distribution=class_distribution,
            featuREDACTED=featuREDACTED,
        ),
        hardware=HardwareInfo(
            backend=backend,
            gpu_name=GPU_NAME if "gpu" in backend else None,
        ),
        hyperparameters={
            "n_estimators": args.n_estimators,
            "max_depth": args.max_depth,
            "min_samples_leaf": args.min_samples_leaf,
            "include_kmers": include_kmers,
            "seed": args.seed,
        },
        training_time_sec=0,  # TODO: track actual time
        train_metrics=train_detailed,
        val_metrics=val_detailed,
        per_class_metrics=val_per_class,
        featuREDACTED=featuREDACTED,
        confusion_matrix=val_cm,
        metadata={
            "level": args.level,
            "data_dir": str(args.data_dir),
        },
    )

    # Save using unified system
    writer = ResultsWriter(base_dir=args.checkpoint_dir)
    train_preds = Predictions(y_train, train_metrics["predictions"], train_metrics["probabilities"])
    val_preds = Predictions(y_val, val_metrics["predictions"], val_metrics["probabilities"])

    output_path = writer.save(
        result=result,
        model=clf,
        train_predictions=train_preds,
        val_predictions=val_preds,
    )

    # Generate publication-ready figures
    print("\nGenerating figures...")
    figures = PaperFigures(output_path / "figures", format="png")
    generated = figures.generate_all(result, val_preds)
    for fig_path in generated:
        print(f"  {fig_path.name}")

    # Print summary
    backend_display = {
        "cuml_gpu": "cuML (GPU)",
        "xgboost_gpu": "XGBoost (GPU)",
        "sklearn_cpu": "sklearn (CPU)",
    }
    print(f"\n{'='*60}")
    print("TRAINING COMPLETE")
    print(f"{'='*60}")
    print(f"  Run ID: {run_id}")
    print(f"  Backend: {backend_display.get(backend, backend)}")
    print(f"  Level: {args.level}")
    print(f"  Classes: {len(class_names)}")
    print(f"  Train Accuracy: {train_detailed['accuracy']*100:.2f}%")
    print(f"  Val Accuracy: {val_detailed['accuracy']*100:.2f}%")
    print(f"  Val F1 (macro): {val_detailed['f1_macro']*100:.2f}%")
    print(f"\nResults saved to: {output_path}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
