"""Dataset for Sanger trace files (.ab1)."""

import struct
from dataclasses import dataclass
from pathlib import Path
from typing import Iterator

import numpy as np
import torch
from torch.utils.data import Dataset


@dataclass
class TraceSample:
    """A single trace sample with signals and quality_enhanced scores."""

    file_path: str
    sequence: str
    quality_scores: np.ndarray  # Phred scores per base
    signal_a: np.ndarray
    signal_t: np.ndarray
    signal_c: np.ndarray
    signal_g: np.ndarray
    peak_locations: np.ndarray

    @property
    def length(self) -> int:
        return len(self.sequence)

    @property
    def signals(self) -> np.ndarray:
        """Combined 4-channel signal array (4, seq_len)."""
        return np.stack([self.signal_a, self.signal_t, self.signal_c, self.signal_g])

    def to_tensor(self) -> dict[str, torch.Tensor]:
        """Convert to PyTorch tensors."""
        return {
            "signals": torch.tensor(self.signals, dtype=torch.float32),
            "quality_enhanced": torch.tensor(self.quality_scores, dtype=torch.float32),
            "peak_locs": torch.tensor(self.peak_locations, dtype=torch.long),
        }


class AB1Parser:
    """Parser for ABI/AB1 trace files."""

    def __init__(self, file_path: str | Path):
        self.file_path = Path(file_path)
        self._data = {}
        self._parse()

    def _parse(self) -> None:
        """Parse AB1 file structure."""
        with open(self.file_path, "rb") as f:
            # Check magic number
            magic = f.read(4)
            if magic != b"ABIF":
                raise ValueError(f"Not a valid AB1 file: {self.file_path}")

            # Read header
            f.seek(18)
            num_elements = struct.unpack(">I", f.read(4))[0]
            data_offset = struct.unpack(">I", f.read(4))[0]

            # Read directory entries
            f.seek(data_offset)
            for _ in range(num_elements):
                entry = self._read_directory_entry(f)
                if entry:
                    self._data[entry["tag"]] = entry

    def _read_directory_entry(self, f) -> dict | None:
        """Read a single directory entry."""
        tag = f.read(4).decode("ascii", errors="ignore")
        tag_num = struct.unpack(">I", f.read(4))[0]
        elem_type = struct.unpack(">H", f.read(2))[0]
        elem_size = struct.unpack(">H", f.read(2))[0]
        num_elements = struct.unpack(">I", f.read(4))[0]
        data_size = struct.unpack(">I", f.read(4))[0]
        data_offset = struct.unpack(">I", f.read(4))[0]
        f.read(4)  # handle

        if data_size == 0:
            return None

        return {
            "tag": f"{tag}{tag_num}",
            "type": elem_type,
            "size": elem_size,
            "count": num_elements,
            "data_size": data_size,
            "offset": data_offset if data_size > 4 else None,
            "inline_data": data_offset if data_size <= 4 else None,
        }

    def _read_data(self, entry: dict) -> bytes:
        """Read data for an entry."""
        with open(self.file_path, "rb") as f:
            if entry["offset"] is not None:
                f.seek(entry["offset"])
                return f.read(entry["data_size"])
            else:
                return struct.pack(">I", entry["inline_data"])[: entry["data_size"]]

    def get_sequence(self) -> str:
        """Get called sequence."""
        for tag in ["PBAS1", "PBAS2"]:
            if tag in self._data:
                data = self._read_data(self._data[tag])
                return data.decode("ascii", errors="ignore")
        return ""

    def get_quality_scores(self) -> np.ndarray:
        """Get Phred quality_enhanced scores."""
        for tag in ["PCON1", "PCON2"]:
            if tag in self._data:
                data = self._read_data(self._data[tag])
                return np.frombuffer(data, dtype=np.uint8)
        return np.array([], dtype=np.uint8)

    def get_trace_data(self, channel: int) -> np.ndarray:
        """Get trace data for a channel (1-4: G,A,T,C or 9-12)."""
        tag = f"DATA{channel}"
        if tag in self._data:
            data = self._read_data(self._data[tag])
            return np.frombuffer(data, dtype=">i2").astype(np.float32)
        return np.array([], dtype=np.float32)

    def get_peak_locations(self) -> np.ndarray:
        """Get peak locations in trace."""
        for tag in ["PLOC1", "PLOC2"]:
            if tag in self._data:
                data = self._read_data(self._data[tag])
                return np.frombuffer(data, dtype=">i2").astype(np.int32)
        return np.array([], dtype=np.int32)

    def to_sample(self) -> TraceSample:
        """Convert to TraceSample."""
        # Get signals - try different channel orderings
        signal_g = (
            self.get_trace_data(9) if len(self.get_trace_data(9)) > 0 else self.get_trace_data(1)
        )
        signal_a = (
            self.get_trace_data(10) if len(self.get_trace_data(10)) > 0 else self.get_trace_data(2)
        )
        signal_t = (
            self.get_trace_data(11) if len(self.get_trace_data(11)) > 0 else self.get_trace_data(3)
        )
        signal_c = (
            self.get_trace_data(12) if len(self.get_trace_data(12)) > 0 else self.get_trace_data(4)
        )

        return TraceSample(
            file_path=str(self.file_path),
            sequence=self.get_sequence(),
            quality_scores=self.get_quality_scores(),
            signal_a=signal_a,
            signal_t=signal_t,
            signal_c=signal_c,
            signal_g=signal_g,
            peak_locations=self.get_peak_locations(),
        )


class TraceDataset(Dataset):
    """PyTorch Dataset for trace files."""

    def __init__(
        self,
        trace_dir: str | Path,
        max_length: int = 1000,
        min_length: int = 100,
        normalize: bool = True,
    ):
        self.trace_dir = Path(trace_dir)
        self.max_length = max_length
        self.min_length = min_length
        self.normalize = normalize

        # Find all .ab1 files
        self.files = list(self.trace_dir.glob("**/*.ab1"))
        self._valid_indices: list[int] = []
        self._samples_cache: dict[int, TraceSample] = {}

    def __len__(self) -> int:
        return len(self.files)

    def _load_sample(self, idx: int) -> TraceSample | None:
        """Load and validate a trace sample."""
        if idx in self._samples_cache:
            return self._samples_cache[idx]

        try:
            parser = AB1Parser(self.files[idx])
            sample = parser.to_sample()

            # Validate
            if sample.length < self.min_length:
                return None
            if len(sample.quality_scores) != sample.length:
                return None
            if len(sample.peak_locations) != sample.length:
                return None

            self._samples_cache[idx] = sample
            return sample

        except Exception:
            return None

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        """Get a training sample."""
        sample = self._load_sample(idx)
        if sample is None:
            # Return dummy sample on error
            return {
                "signals": torch.zeros(4, self.max_length),
                "quality_enhanced": torch.zeros(self.max_length),
                "mask": torch.zeros(self.max_length, dtype=torch.bool),
            }

        # Extract signals at peak locations
        signals = []
        for sig, peaks in [
            (sample.signal_a, sample.peak_locations),
            (sample.signal_t, sample.peak_locations),
            (sample.signal_c, sample.peak_locations),
            (sample.signal_g, sample.peak_locations),
        ]:
            if len(sig) > 0 and len(peaks) > 0:
                valid_peaks = peaks[peaks < len(sig)]
                channel = sig[valid_peaks] if len(valid_peaks) > 0 else np.zeros(len(peaks))
            else:
                channel = np.zeros(sample.length)
            signals.append(channel)

        signals = np.stack(signals)  # (4, seq_len)
        quality = sample.quality_scores.astype(np.float32)
        seq_len = min(sample.length, self.max_length)

        # Normalize signals
        if self.normalize:
            max_val = signals.max()
            if max_val > 0:
                signals = signals / max_val

        # Pad or truncate
        if seq_len < self.max_length:
            pad_len = self.max_length - seq_len
            signals = np.pad(signals, ((0, 0), (0, pad_len)), mode="constant")
            quality = np.pad(quality, (0, pad_len), mode="constant")
            mask = np.concatenate([np.ones(seq_len), np.zeros(pad_len)])
        else:
            signals = signals[:, : self.max_length]
            quality = quality[: self.max_length]
            mask = np.ones(self.max_length)

        return {
            "signals": torch.tensor(signals, dtype=torch.float32),
            "quality_enhanced": torch.tensor(quality, dtype=torch.float32),
            "mask": torch.tensor(mask, dtype=torch.bool),
        }

    def iter_samples(self) -> Iterator[TraceSample]:
        """Iterate over valid samples."""
        for idx in range(len(self)):
            sample = self._load_sample(idx)
            if sample is not None:
                yield sample
