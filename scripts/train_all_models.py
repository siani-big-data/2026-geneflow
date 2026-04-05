#!/usr/bin/env python3
"""
Train all GeneFlow ML models.

Usage:
    uv run python scripts/train_all_models.py --all
    uv run python scripts/train_all_models.py --model trimming
    uv run python scripts/train_all_models.py --model heterozygote_training --epochs 100
"""

import argparse
import gzip
import json
import random
import sys
from dataclasses import dataclass
from pathlib import Path
from datetime import datetime

import numpy as np
import torch
import torch.nn as nn
from torch.optim import AdamW
from torch.optim.lr_scheduler import CosineAnnealingLR
from torch.utils.data import DataLoader, Dataset

sys.path.insert(0, str(Path(__file__).parent.parent))

random.seed(42)
np.random.seed(42)
torch.manual_seed(42)


# =============================================================================
# Dataset Generators
# =============================================================================

def read_fastq_sequences(fastq_dir: Path, max_reads: int = 50000) -> list[dict]:
    """Read sequences and qualities from FASTQ files."""
    reads = []

    for fq in fastq_dir.glob("*.fastq*"):
        opener = gzip.open if str(fq).endswith('.gz') else open
        mode = 'rt' if str(fq).endswith('.gz') else 'r'

        with opener(fq, mode) as f:
            while len(reads) < max_reads:
                header = f.readline()
                if not header:
                    break
                seq = f.readline().strip().upper()
                plus = f.readline()
                qual_str = f.readline().strip()

                if len(seq) >= 100 and all(c in 'ACGT' for c in seq):
                    qualities = np.array([ord(c) - 33 for c in qual_str], dtype=np.int32)
                    reads.append({'sequence': seq, 'qualities': qualities})

    return reads


def generate_trimming_dataset(reads: list[dict], num_samples: int) -> list[dict]:
    """Generate trimming dataset with synthetic trim labels."""
    samples = []

    for _ in range(num_samples):
        read = random.choice(reads)
        seq = read['sequence']
        qual = read['qualities']
        seq_len = len(seq)

        # Find optimal trim points based on quality
        # Sliding window to find quality drops
        window = 10
        threshold = 20

        # Find start: first position where avg quality > threshold
        start = 0
        for i in range(0, min(seq_len // 3, seq_len - window)):
            if np.mean(qual[i:i+window]) >= threshold:
                start = i
                break

        # Find end: last position where avg quality > threshold
        end = seq_len
        for i in range(seq_len - window, max(seq_len * 2 // 3, window), -1):
            if np.mean(qual[i:i+window]) >= threshold:
                end = i + window
                break

        # Add some noise to labels
        start = max(0, start + random.randint(-5, 5))
        end = min(seq_len, end + random.randint(-5, 5))

        samples.append({
            'qualities': qual[:500] if len(qual) > 500 else qual,
            'start_ratio': start / seq_len,
            'end_ratio': end / seq_len,
            'length': min(seq_len, 500),
        })

    return samples


def generate_heterozygote_dataset(reads: list[dict], num_samples: int) -> list[dict]:
    """Generate heterozygote_training dataset with synthetic labels."""
    samples = []

    for _ in range(num_samples):
        read = random.choice(reads)
        seq = list(read['sequence'][:500])
        qual = read['qualities'][:500].copy()
        seq_len = len(seq)

        # Create labels: 0=homo, 1=hetero
        labels = np.zeros(seq_len, dtype=np.int64)

        # Randomly add heterozygous positions (5-15% of positions)
        num_hetero = random.randint(int(seq_len * 0.05), int(seq_len * 0.15))
        hetero_positions = random.sample(range(seq_len), num_hetero)

        for pos in hetero_positions:
            labels[pos] = 1
            # Heterozygous positions often have slightly lower quality
            qual[pos] = max(5, qual[pos] - random.randint(5, 15))

        samples.append({
            'sequence': ''.join(seq),
            'qualities': qual,
            'labels': labels,
            'length': seq_len,
        })

    return samples


def generate_variant_dataset(reads: list[dict], num_samples: int) -> list[dict]:
    """Generate variant classification dataset."""
    samples = []
    variant_types = ['SNP', 'Insertion', 'Deletion', 'Complex']

    for _ in range(num_samples):
        read = random.choice(reads)
        seq = read['sequence']
        qual = read['qualities']

        # Random position for variant
        pos = random.randint(20, len(seq) - 20)

        # Generate variant
        variant_type = random.choice(variant_types)
        ref_seq = seq
        alt_seq = list(seq)

        if variant_type == 'SNP':
            # Single base change
            old_base = alt_seq[pos]
            new_base = random.choice([b for b in 'ACGT' if b != old_base])
            alt_seq[pos] = new_base
        elif variant_type == 'Insertion':
            # Insert 1-3 bases
            insert = ''.join(random.choices('ACGT', k=random.randint(1, 3)))
            alt_seq.insert(pos, insert)
        elif variant_type == 'Deletion':
            # Delete 1-3 bases
            del_len = random.randint(1, 3)
            del alt_seq[pos:pos + del_len]
        else:  # Complex
            # Both SNP and indel
            alt_seq[pos] = random.choice('ACGT')
            if random.random() > 0.5:
                alt_seq.insert(pos + 1, random.choice('ACGT'))
            else:
                if pos + 1 < len(alt_seq):
                    del alt_seq[pos + 1]

        alt_seq = ''.join(alt_seq)

        samples.append({
            'ref_seq': ref_seq,
            'alt_seq': alt_seq,
            'position': pos,
            'quality': qual[pos] if pos < len(qual) else 30,
            'variant_type': variant_types.index(variant_type),
        })

    return samples


def generate_orf_dataset(reads: list[dict], num_samples: int) -> list[dict]:
    """Generate ORF prediction dataset."""
    samples = []
    start_codon = 'ATG'
    stop_codons = ['TAA', 'TAG', 'TGA']

    for _ in range(num_samples):
        read = random.choice(reads)
        seq = list(read['sequence'][:500])
        seq_len = len(seq)

        # Labels: 0=non-coding, 1=start, 2=coding, 3=stop
        labels = np.zeros(seq_len, dtype=np.int64)

        # Insert synthetic ORFs
        num_orfs = random.randint(0, 3)
        used_regions = []

        for _ in range(num_orfs):
            # Random ORF length (divisible by 3)
            orf_len = random.randint(30, 150) // 3 * 3
            start_pos = random.randint(0, seq_len - orf_len - 6)

            # Check overlap
            overlap = False
            for r_start, r_end in used_regions:
                if not (start_pos + orf_len < r_start or start_pos > r_end):
                    overlap = True
                    break

            if overlap:
                continue

            # Insert start codon
            for i, base in enumerate(start_codon):
                if start_pos + i < seq_len:
                    seq[start_pos + i] = base
                    labels[start_pos + i] = 1

            # Mark coding region
            for i in range(3, orf_len - 3):
                if start_pos + i < seq_len:
                    labels[start_pos + i] = 2

            # Insert stop codon
            stop = random.choice(stop_codons)
            stop_pos = start_pos + orf_len - 3
            for i, base in enumerate(stop):
                if stop_pos + i < seq_len:
                    seq[stop_pos + i] = base
                    labels[stop_pos + i] = 3

            used_regions.append((start_pos, start_pos + orf_len))

        samples.append({
            'sequence': ''.join(seq),
            'labels': labels,
            'length': seq_len,
        })

    return samples


def generate_consensus_dataset(reads: list[dict], num_samples: int) -> list[dict]:
    """Generate consensus prediction dataset."""
    samples = []

    for _ in range(num_samples):
        # Pick a "reference" read
        ref_read = random.choice(reads)
        ref_seq = ref_read['sequence'][:300]
        ref_qual = ref_read['qualities'][:300]
        seq_len = len(ref_seq)

        # Generate 3-8 "aligned" reads with mutations
        num_reads = random.randint(3, 8)
        aligned_reads = []

        for _ in range(num_reads):
            read_seq = list(ref_seq)
            read_qual = ref_qual.copy()

            # Add random mutations (1-5%)
            num_mutations = random.randint(1, max(1, int(seq_len * 0.05)))
            for _ in range(num_mutations):
                pos = random.randint(0, seq_len - 1)
                read_seq[pos] = random.choice('ACGT')
                read_qual[pos] = max(5, read_qual[pos] - random.randint(0, 10))

            aligned_reads.append({
                'sequence': ''.join(read_seq),
                'qualities': read_qual,
            })

        # True consensus is majority vote (simplified: use reference)
        samples.append({
            'reads': aligned_reads,
            'consensus': ref_seq,
            'length': seq_len,
        })

    return samples


def generate_alignment_dataset(reads: list[dict], num_samples: int) -> list[dict]:
    """Generate alignment scoring dataset."""
    samples = []

    for _ in range(num_samples):
        read1 = random.choice(reads)
        seq1 = read1['sequence'][:300]

        # Decide similarity level
        if random.random() < 0.5:
            # High similarity: mutate slightly
            similarity = random.uniform(0.7, 0.99)
            seq2 = list(seq1)
            num_changes = int(len(seq1) * (1 - similarity))
            for _ in range(num_changes):
                pos = random.randint(0, len(seq2) - 1)
                seq2[pos] = random.choice('ACGT')
            seq2 = ''.join(seq2)
        else:
            # Low similarity: different sequence
            read2 = random.choice(reads)
            seq2 = read2['sequence'][:300]
            # Calculate actual similarity
            matches = sum(1 for a, b in zip(seq1, seq2) if a == b)
            similarity = matches / max(len(seq1), len(seq2))

        samples.append({
            'seq1': seq1,
            'seq2': seq2,
            'similarity': similarity,
        })

    return samples


# =============================================================================
# PyTorch Datasets
# =============================================================================

class TrimmingDataset(Dataset):
    def __init__(self, samples: list[dict], max_length: int = 500):
        self.samples = samples
        self.max_length = max_length

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        s = self.samples[idx]
        qual = s['qualities']

        # Pad/truncate
        padded = np.zeros(self.max_length, dtype=np.float32)
        length = min(len(qual), self.max_length)
        padded[:length] = qual[:length] / 60.0  # Normalize

        mask = np.zeros(self.max_length, dtype=bool)
        mask[:length] = True

        return {
            'quality': torch.tensor(padded),
            'mask': torch.tensor(mask),
            'target': torch.tensor([s['start_ratio'], s['end_ratio']], dtype=torch.float32),
        }


class HeterozygoteDataset(Dataset):
    def __init__(self, samples: list[dict], max_length: int = 500):
        self.samples = samples
        self.max_length = max_length
        self.mapping = {'A': 0, 'C': 1, 'G': 2, 'T': 3}

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        s = self.samples[idx]
        seq = s['sequence']
        qual = s['qualities']
        labels = s['labels']
        length = min(len(seq), self.max_length)

        # One-hot encode sequence
        one_hot = np.zeros((4, self.max_length), dtype=np.float32)
        for i, base in enumerate(seq[:length]):
            if base in self.mapping:
                one_hot[self.mapping[base], i] = 1.0

        # Pad quality
        padded_qual = np.zeros(self.max_length, dtype=np.float32)
        padded_qual[:length] = qual[:length] / 60.0

        # Pad labels
        padded_labels = np.zeros(self.max_length, dtype=np.int64)
        padded_labels[:length] = labels[:length]

        mask = np.zeros(self.max_length, dtype=bool)
        mask[:length] = True

        return {
            'sequence': torch.tensor(one_hot),
            'quality': torch.tensor(padded_qual),
            'labels': torch.tensor(padded_labels),
            'mask': torch.tensor(mask),
        }


class VariantDataset(Dataset):
    def __init__(self, samples: list[dict], context_size: int = 21):
        self.samples = samples
        self.context_size = context_size
        self.mapping = {'A': 0, 'C': 1, 'G': 2, 'T': 3}

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        s = self.samples[idx]
        pos = s['position']
        half = self.context_size // 2

        def encode_context(seq: str) -> np.ndarray:
            ctx = seq[max(0, pos - half):pos + half + 1]
            ctx = ctx.ljust(self.context_size, 'N')[:self.context_size]
            one_hot = np.zeros((4, self.context_size), dtype=np.float32)
            for i, base in enumerate(ctx):
                if base in self.mapping:
                    one_hot[self.mapping[base], i] = 1.0
            return one_hot

        return {
            'ref_context': torch.tensor(encode_context(s['ref_seq'])),
            'alt_context': torch.tensor(encode_context(s['alt_seq'])),
            'quality': torch.tensor([s['quality'] / 60.0], dtype=torch.float32),
            'label': torch.tensor(s['variant_type'], dtype=torch.long),
        }


class ORFDataset(Dataset):
    def __init__(self, samples: list[dict], max_length: int = 500):
        self.samples = samples
        self.max_length = max_length
        self.mapping = {'A': 0, 'C': 1, 'G': 2, 'T': 3}

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        s = self.samples[idx]
        seq = s['sequence']
        labels = s['labels']
        length = min(len(seq), self.max_length)

        one_hot = np.zeros((4, self.max_length), dtype=np.float32)
        for i, base in enumerate(seq[:length]):
            if base in self.mapping:
                one_hot[self.mapping[base], i] = 1.0

        padded_labels = np.zeros(self.max_length, dtype=np.int64)
        padded_labels[:length] = labels[:length]

        mask = np.zeros(self.max_length, dtype=bool)
        mask[:length] = True

        return {
            'sequence': torch.tensor(one_hot),
            'labels': torch.tensor(padded_labels),
            'mask': torch.tensor(mask),
        }


class AlignmentDataset(Dataset):
    def __init__(self, samples: list[dict], max_length: int = 300):
        self.samples = samples
        self.max_length = max_length
        self.mapping = {'A': 0, 'C': 1, 'G': 2, 'T': 3}

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        s = self.samples[idx]

        def encode_seq(seq: str) -> np.ndarray:
            one_hot = np.zeros((4, self.max_length), dtype=np.float32)
            for i, base in enumerate(seq[:self.max_length]):
                if base in self.mapping:
                    one_hot[self.mapping[base], i] = 1.0
            return one_hot

        return {
            'seq1': torch.tensor(encode_seq(s['seq1'])),
            'seq2': torch.tensor(encode_seq(s['seq2'])),
            'similarity': torch.tensor([s['similarity']], dtype=torch.float32),
        }


# =============================================================================
# Training Functions
# =============================================================================

def train_trimming(reads: list[dict], output_dir: Path, epochs: int = 100, patience: int = 20):
    """Train TrimmingPredictor."""
    from src.ml.models.trimming import TrimmingPredictor, TrimmingConfig

    print("\n" + "=" * 60)
    print("TRAINING: TrimmingPredictor")
    print("=" * 60)

    # Generate data
    samples = generate_trimming_dataset(reads, num_samples=20000)
    random.shuffle(samples)

    train_samples = samples[:16000]
    val_samples = samples[16000:]

    train_dataset = TrimmingDataset(train_samples)
    val_dataset = TrimmingDataset(val_samples)

    train_loader = DataLoader(train_dataset, batch_size=64, shuffle=True)
    val_loader = DataLoader(val_dataset, batch_size=64)

    # Model
    config = TrimmingConfig()
    model = TrimmingPredictor(config)
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    model = model.to(device)

    print(f"Parameters: {sum(p.numel() for p in model.parameters()):,}")

    criterion = nn.MSELoss()
    optimizer = AdamW(model.parameters(), lr=1e-3)
    scheduler = CosineAnnealingLR(optimizer, T_max=epochs)

    best_val_loss = float('inf')
    no_improve = 0

    for epoch in range(epochs):
        # Train
        model.train()
        train_loss = 0
        for batch in train_loader:
            optimizer.zero_grad()
            quality = batch['quality'].to(device)
            mask = batch['mask'].to(device)
            target = batch['target'].to(device)

            pred = model(quality, mask)
            loss = criterion(pred, target)
            loss.backward()
            optimizer.step()
            train_loss += loss.item()

        train_loss /= len(train_loader)

        # Validate
        model.eval()
        val_loss = 0
        with torch.no_grad():
            for batch in val_loader:
                quality = batch['quality'].to(device)
                mask = batch['mask'].to(device)
                target = batch['target'].to(device)
                pred = model(quality, mask)
                val_loss += criterion(pred, target).item()
        val_loss /= len(val_loader)

        scheduler.step()

        if val_loss < best_val_loss:
            best_val_loss = val_loss
            no_improve = 0
            model.save(output_dir / 'trimming_best.pt')
        else:
            no_improve += 1

        if epoch % 10 == 0 or no_improve == 0:
            print(f"Epoch {epoch+1:3d} | Train: {train_loss:.4f} | Val: {val_loss:.4f}")

        if no_improve >= patience:
            print(f"Early stopping at epoch {epoch+1}")
            break

    print(f"Best val loss: {best_val_loss:.4f}")
    return best_val_loss


def train_heterozygote(reads: list[dict], output_dir: Path, epochs: int = 100, patience: int = 20):
    """Train HeterozygoteClassifier."""
    from src.ml.models.heterozygote import HeterozygoteClassifier, HeterozygoteConfig

    print("\n" + "=" * 60)
    print("TRAINING: HeterozygoteClassifier")
    print("=" * 60)

    samples = generate_heterozygote_dataset(reads, num_samples=20000)
    random.shuffle(samples)

    train_samples = samples[:16000]
    val_samples = samples[16000:]

    train_dataset = HeterozygoteDataset(train_samples)
    val_dataset = HeterozygoteDataset(val_samples)

    train_loader = DataLoader(train_dataset, batch_size=64, shuffle=True)
    val_loader = DataLoader(val_dataset, batch_size=64)

    config = HeterozygoteConfig()
    model = HeterozygoteClassifier(config)
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    model = model.to(device)

    print(f"Parameters: {sum(p.numel() for p in model.parameters()):,}")

    criterion = nn.CrossEntropyLoss()
    optimizer = AdamW(model.parameters(), lr=1e-3)
    scheduler = CosineAnnealingLR(optimizer, T_max=epochs)

    best_val_acc = 0
    no_improve = 0

    for epoch in range(epochs):
        model.train()
        train_loss = 0
        for batch in train_loader:
            optimizer.zero_grad()
            seq = batch['sequence'].to(device)
            qual = batch['quality'].to(device)
            labels = batch['labels'].to(device)
            mask = batch['mask'].to(device)

            logits = model(seq, qual, mask)
            loss = criterion(logits[mask].view(-1, 2), labels[mask].view(-1))
            loss.backward()
            optimizer.step()
            train_loss += loss.item()

        train_loss /= len(train_loader)

        model.eval()
        correct = 0
        total = 0
        with torch.no_grad():
            for batch in val_loader:
                seq = batch['sequence'].to(device)
                qual = batch['quality'].to(device)
                labels = batch['labels'].to(device)
                mask = batch['mask'].to(device)

                logits = model(seq, qual, mask)
                preds = logits.argmax(dim=-1)
                correct += (preds[mask] == labels[mask]).sum().item()
                total += mask.sum().item()

        val_acc = correct / total
        scheduler.step()

        if val_acc > best_val_acc:
            best_val_acc = val_acc
            no_improve = 0
            model.save(output_dir / 'heterozygote_best.pt')
        else:
            no_improve += 1

        if epoch % 10 == 0 or no_improve == 0:
            print(f"Epoch {epoch+1:3d} | Train Loss: {train_loss:.4f} | Val Acc: {val_acc:.2%}")

        if no_improve >= patience:
            print(f"Early stopping at epoch {epoch+1}")
            break

    print(f"Best val accuracy: {best_val_acc:.2%}")
    return best_val_acc


def train_variant(reads: list[dict], output_dir: Path, epochs: int = 100, patience: int = 20):
    """Train VariantClassifier."""
    from src.ml.models.variants import VariantClassifier, VariantConfig

    print("\n" + "=" * 60)
    print("TRAINING: VariantClassifier")
    print("=" * 60)

    samples = generate_variant_dataset(reads, num_samples=20000)
    random.shuffle(samples)

    train_samples = samples[:16000]
    val_samples = samples[16000:]

    train_dataset = VariantDataset(train_samples)
    val_dataset = VariantDataset(val_samples)

    train_loader = DataLoader(train_dataset, batch_size=64, shuffle=True)
    val_loader = DataLoader(val_dataset, batch_size=64)

    config = VariantConfig()
    model = VariantClassifier(config)
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    model = model.to(device)

    print(f"Parameters: {sum(p.numel() for p in model.parameters()):,}")

    criterion = nn.CrossEntropyLoss()
    optimizer = AdamW(model.parameters(), lr=1e-3)
    scheduler = CosineAnnealingLR(optimizer, T_max=epochs)

    best_val_acc = 0
    no_improve = 0

    for epoch in range(epochs):
        model.train()
        train_loss = 0
        for batch in train_loader:
            optimizer.zero_grad()
            ref = batch['ref_context'].to(device)
            alt = batch['alt_context'].to(device)
            qual = batch['quality'].to(device)
            labels = batch['label'].to(device)

            logits = model(ref, alt, qual)
            loss = criterion(logits, labels)
            loss.backward()
            optimizer.step()
            train_loss += loss.item()

        train_loss /= len(train_loader)

        model.eval()
        correct = 0
        total = 0
        with torch.no_grad():
            for batch in val_loader:
                ref = batch['ref_context'].to(device)
                alt = batch['alt_context'].to(device)
                qual = batch['quality'].to(device)
                labels = batch['label'].to(device)

                logits = model(ref, alt, qual)
                preds = logits.argmax(dim=-1)
                correct += (preds == labels).sum().item()
                total += labels.size(0)

        val_acc = correct / total
        scheduler.step()

        if val_acc > best_val_acc:
            best_val_acc = val_acc
            no_improve = 0
            model.save(output_dir / 'variant_best.pt')
        else:
            no_improve += 1

        if epoch % 10 == 0 or no_improve == 0:
            print(f"Epoch {epoch+1:3d} | Train Loss: {train_loss:.4f} | Val Acc: {val_acc:.2%}")

        if no_improve >= patience:
            print(f"Early stopping at epoch {epoch+1}")
            break

    print(f"Best val accuracy: {best_val_acc:.2%}")
    return best_val_acc


def train_orf(reads: list[dict], output_dir: Path, epochs: int = 100, patience: int = 20):
    """Train ORFPredictor."""
    from src.ml.models.orf import ORFPredictor, ORFConfig

    print("\n" + "=" * 60)
    print("TRAINING: ORFPredictor")
    print("=" * 60)

    samples = generate_orf_dataset(reads, num_samples=20000)
    random.shuffle(samples)

    train_samples = samples[:16000]
    val_samples = samples[16000:]

    train_dataset = ORFDataset(train_samples)
    val_dataset = ORFDataset(val_samples)

    train_loader = DataLoader(train_dataset, batch_size=64, shuffle=True)
    val_loader = DataLoader(val_dataset, batch_size=64)

    config = ORFConfig()
    model = ORFPredictor(config)
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    model = model.to(device)

    print(f"Parameters: {sum(p.numel() for p in model.parameters()):,}")

    criterion = nn.CrossEntropyLoss()
    optimizer = AdamW(model.parameters(), lr=1e-3)
    scheduler = CosineAnnealingLR(optimizer, T_max=epochs)

    best_val_acc = 0
    no_improve = 0

    for epoch in range(epochs):
        model.train()
        train_loss = 0
        for batch in train_loader:
            optimizer.zero_grad()
            seq = batch['sequence'].to(device)
            labels = batch['labels'].to(device)
            mask = batch['mask'].to(device)

            logits = model(seq)
            loss = criterion(logits[mask].view(-1, 4), labels[mask].view(-1))
            loss.backward()
            optimizer.step()
            train_loss += loss.item()

        train_loss /= len(train_loader)

        model.eval()
        correct = 0
        total = 0
        with torch.no_grad():
            for batch in val_loader:
                seq = batch['sequence'].to(device)
                labels = batch['labels'].to(device)
                mask = batch['mask'].to(device)

                logits = model(seq)
                preds = logits.argmax(dim=-1)
                correct += (preds[mask] == labels[mask]).sum().item()
                total += mask.sum().item()

        val_acc = correct / total
        scheduler.step()

        if val_acc > best_val_acc:
            best_val_acc = val_acc
            no_improve = 0
            model.save(output_dir / 'orf_best.pt')
        else:
            no_improve += 1

        if epoch % 10 == 0 or no_improve == 0:
            print(f"Epoch {epoch+1:3d} | Train Loss: {train_loss:.4f} | Val Acc: {val_acc:.2%}")

        if no_improve >= patience:
            print(f"Early stopping at epoch {epoch+1}")
            break

    print(f"Best val accuracy: {best_val_acc:.2%}")
    return best_val_acc


def train_alignment(reads: list[dict], output_dir: Path, epochs: int = 100, patience: int = 20):
    """Train AlignmentScorer."""
    from src.ml.models.alignment import AlignmentScorer, AlignmentConfig

    print("\n" + "=" * 60)
    print("TRAINING: AlignmentScorer")
    print("=" * 60)

    samples = generate_alignment_dataset(reads, num_samples=20000)
    random.shuffle(samples)

    train_samples = samples[:16000]
    val_samples = samples[16000:]

    train_dataset = AlignmentDataset(train_samples)
    val_dataset = AlignmentDataset(val_samples)

    train_loader = DataLoader(train_dataset, batch_size=64, shuffle=True)
    val_loader = DataLoader(val_dataset, batch_size=64)

    config = AlignmentConfig()
    model = AlignmentScorer(config)
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    model = model.to(device)

    print(f"Parameters: {sum(p.numel() for p in model.parameters()):,}")

    criterion = nn.MSELoss()
    optimizer = AdamW(model.parameters(), lr=1e-3)
    scheduler = CosineAnnealingLR(optimizer, T_max=epochs)

    best_val_loss = float('inf')
    no_improve = 0

    for epoch in range(epochs):
        model.train()
        train_loss = 0
        for batch in train_loader:
            optimizer.zero_grad()
            seq1 = batch['seq1'].to(device)
            seq2 = batch['seq2'].to(device)
            target = batch['similarity'].to(device)

            pred = model(seq1, seq2)
            loss = criterion(pred, target)
            loss.backward()
            optimizer.step()
            train_loss += loss.item()

        train_loss /= len(train_loader)

        model.eval()
        val_loss = 0
        with torch.no_grad():
            for batch in val_loader:
                seq1 = batch['seq1'].to(device)
                seq2 = batch['seq2'].to(device)
                target = batch['similarity'].to(device)
                pred = model(seq1, seq2)
                val_loss += criterion(pred, target).item()
        val_loss /= len(val_loader)

        scheduler.step()

        if val_loss < best_val_loss:
            best_val_loss = val_loss
            no_improve = 0
            model.save(output_dir / 'alignment_best.pt')
        else:
            no_improve += 1

        if epoch % 10 == 0 or no_improve == 0:
            print(f"Epoch {epoch+1:3d} | Train: {train_loss:.4f} | Val: {val_loss:.4f}")

        if no_improve >= patience:
            print(f"Early stopping at epoch {epoch+1}")
            break

    print(f"Best val loss: {best_val_loss:.4f}")
    return best_val_loss


# =============================================================================
# Main
# =============================================================================

def main():
    parser = argparse.ArgumentParser(description="Train all GeneFlow models")
    parser.add_argument("--all", action="stoREDACTED", help="Train all models")
    parser.add_argument("--model", type=str, choices=[
        'trimming', 'heterozygote_training', 'variant', 'orf', 'consensus', 'alignment'
    ], help="Train specific model")
    parser.add_argument("--fastq-dir", type=str, default="datalake/fastq_real")
    parser.add_argument("--output-dir", type=str, default="checkpoints/models")
    parser.add_argument("--epochs", type=int, default=100)
    parser.add_argument("--patience", type=int, default=20)
    args = parser.parse_args()

    fastq_dir = Path(args.fastq_dir)
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    print("=" * 60)
    print("GENEFLOW MODEL TRAINER")
    print("=" * 60)
    print(f"Started: {datetime.now()}")
    print(f"FASTQ dir: {fastq_dir}")
    print(f"Output dir: {output_dir}")

    # Load FASTQ data
    print("\nLoading FASTQ sequences...")
    reads = read_fastq_sequences(fastq_dir)
    print(f"Loaded {len(reads):,} reads")

    if len(reads) == 0:
        print("ERROR: No reads found!")
        return 1

    # Train models
    results = {}

    train_funcs = {
        'trimming': train_trimming,
        'heterozygote_training': train_heterozygote,
        'variant': train_variant,
        'orf': train_orf,
        'alignment': train_alignment,
    }

    if args.all:
        models_to_train = list(train_funcs.keys())
    elif args.model:
        models_to_train = [args.model]
    else:
        parser.print_help()
        return 1

    for model_name in models_to_train:
        if model_name in train_funcs:
            result = train_funcs[model_name](
                reads, output_dir, epochs=args.epochs, patience=args.patience
            )
            results[model_name] = result

    # Summary
    print("\n" + "=" * 60)
    print("TRAINING COMPLETE")
    print("=" * 60)
    print(f"Finished: {datetime.now()}")
    print("\nResults:")
    for name, metric in results.items():
        print(f"  {name}: {metric:.4f}")

    # Save summary
    with open(output_dir / "training_summary.json", "w") as f:
        json.dump({
            'timestamp': str(datetime.now()),
            'results': results,
        }, f, indent=2)

    return 0


if __name__ == "__main__":
    sys.exit(main())
