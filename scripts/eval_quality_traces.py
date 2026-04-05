#!/usr/bin/env python3
"""
Evaluate quality model on real AB1 traces.

Usage:
    uv run python scripts/eval_quality_traces.py --traces datalake/traces --model checkpoints/quality_enhanced/best.pt
"""

import argparse
import sys
from pathlib import Path

import numpy as np
import torch
from Bio import SeqIO

sys.path.insert(0, str(Path(__file__).parent.parent))


def extract_features_from_trace(record) -> tuple[np.ndarray, np.ndarray] | None:
    """Extract features from a Biopython SeqRecord (AB1 file)."""
    seq = str(record.seq)
    quality = np.array(record.letter_annotations.get('phred_quality', []))

    if len(seq) == 0 or len(quality) == 0:
        return None

    # Basic sequence features
    seq_len = len(seq)
    gc_content = (seq.count('G') + seq.count('C')) / seq_len if seq_len > 0 else 0

    # Quality statistics
    q_mean = np.mean(quality)
    q_std = np.std(quality)
    q_min = np.min(quality)
    q_max = np.max(quality)
    q_median = np.median(quality)

    # Quality distribution
    q_low = np.sum(quality < 20) / len(quality)  # % low quality
    q_high = np.sum(quality >= 30) / len(quality)  # % high quality

    # Positional quality
    start_q = np.mean(quality[:min(50, len(quality))])
    end_q = np.mean(quality[-min(50, len(quality)):])
    mid_start = len(quality) // 4
    mid_end = 3 * len(quality) // 4
    mid_q = np.mean(quality[mid_start:mid_end]) if mid_end > mid_start else q_mean

    # K-mer features (simplified)
    kmers = {}
    for k in [2, 3]:
        for i in range(len(seq) - k + 1):
            kmer = seq[i:i+k]
            kmers[kmer] = kmers.get(kmer, 0) + 1

    # Dinucleotide frequencies
    dinucs = ['AA', 'AT', 'AC', 'AG', 'TA', 'TT', 'TC', 'TG',
              'CA', 'CT', 'CC', 'CG', 'GA', 'GT', 'GC', 'GG']
    dinuc_freqs = [kmers.get(d, 0) / max(seq_len - 1, 1) for d in dinucs]

    # Combine all features (no signal features - just sequence-based)
    features = [
        seq_len / 1000,  # Normalized length
        gc_content,
        q_mean / 60,  # Normalized (but we won't use this for prediction!)
        q_std / 20,
        q_min / 60,
        q_max / 60,
        q_median / 60,
        q_low,
        q_high,
        start_q / 60,
        end_q / 60,
        mid_q / 60,
    ] + dinuc_freqs

    # Pad to expected size (101 features)
    while len(features) < 101:
        features.append(0)

    return np.array(features[:101], dtype=np.float32), quality


def main():
    parser = argparse.ArgumentParser(description="Evaluate quality model on AB1 traces")
    parser.add_argument("--traces", type=str, default="datalake/traces", help="Directory with AB1 files")
    parser.add_argument("--model", type=str, default="checkpoints/quality_enhanced/best.pt", help="Model checkpoint")
    args = parser.parse_args()

    traces_dir = Path(args.traces)
    model_path = Path(args.model)

    print("=" * 70)
    print("QUALITY MODEL EVALUATION ON REAL AB1 TRACES")
    print("=" * 70)

    # Load model
    if not model_path.exists():
        print(f"ERROR: Model not found at {model_path}")
        return 1

    print(f"\nModel: {model_path}")
    checkpoint = torch.load(model_path, map_location="cpu", weights_only=True)

    # Recreate model
    from scripts.train_quality_precomputed import QualityMLP

    input_dim = checkpoint.get("input_dim", 101)
    hidden_dims = checkpoint.get("hidden_dims", [256, 128, 64])

    model = QualityMLP(input_dim=input_dim, hidden_dims=hidden_dims, dropout=0.0)
    model.load_state_dict(checkpoint["model_state_dict"])
    model.eval()

    print(f"Loaded model: input_dim={input_dim}, hidden_dims={hidden_dims}")

    # Find AB1 files
    ab1_files = list(traces_dir.glob("*.ab1"))
    print(f"\nFound {len(ab1_files)} AB1 files")

    if not ab1_files:
        print("ERROR: No AB1 files found")
        return 1

    # Process each trace
    print("\n" + "-" * 70)
    print(f"{'File':<30} {'Length':>8} {'Real Q':>8} {'Pred Q':>8} {'Error':>8}")
    print("-" * 70)

    results = []

    for ab1_file in sorted(ab1_files):
        try:
            record = SeqIO.read(ab1_file, 'abi')
            seq_len = len(record.seq)

            if seq_len < 50:
                print(f"{ab1_file.name:<30} SKIP: too short ({seq_len} bp)")
                continue

            # Extract features
            result = extract_features_from_trace(record)
            if result is None:
                print(f"{ab1_file.name:<30} SKIP: feature extraction failed")
                continue

            features, quality = result

            if len(quality) == 0:
                print(f"{ab1_file.name:<30} SKIP: no quality data")
                continue

            # Real mean quality
            real_mean_q = np.mean(quality)

            # For fair plots, zero out the quality-derived features
            # (indices 2-11 contain quality info that would be cheating)
            features_no_leak = features.copy()
            features_no_leak[2:12] = 0  # Zero out q_mean, q_std, q_min, etc.

            with torch.no_grad():
                features_tensor = torch.tensor(features_no_leak).unsqueeze(0)
                pred_mean_q = model(features_tensor).item()

            error = pred_mean_q - real_mean_q

            print(f"{ab1_file.name:<30} {seq_len:>8} {real_mean_q:>8.1f} {pred_mean_q:>8.1f} {error:>+8.1f}")

            results.append({
                "file": ab1_file.name,
                "length": seq_len,
                "real_q": real_mean_q,
                "pred_q": pred_mean_q,
                "error": error,
            })

        except Exception as e:
            print(f"{ab1_file.name:<30} ERROR: {e}")

    if not results:
        print("\nNo valid traces processed")
        return 1

    # Summary statistics
    print("\n" + "=" * 70)
    print("SUMMARY")
    print("=" * 70)

    errors = [r["error"] for r in results]
    abs_errors = [abs(e) for e in errors]

    print(f"\nTraces evaluated: {len(results)}")
    print(f"Mean Absolute Error (MAE): {np.mean(abs_errors):.2f}")
    print(f"Root Mean Square Error (RMSE): {np.sqrt(np.mean([e**2 for e in errors])):.2f}")
    print(f"Mean Error (bias): {np.mean(errors):+.2f}")
    print(f"Max Error: {max(abs_errors):.2f}")

    # Accuracy within tolerance
    for tol in [1, 2, 5]:
        acc = sum(1 for e in abs_errors if e <= tol) / len(abs_errors)
        print(f"Accuracy (±{tol}): {acc:.1%}")

    # Quality distribution comparison
    print(f"\nReal quality range: {min(r['real_q'] for r in results):.1f} - {max(r['real_q'] for r in results):.1f}")
    print(f"Pred quality range: {min(r['pred_q'] for r in results):.1f} - {max(r['pred_q'] for r in results):.1f}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
