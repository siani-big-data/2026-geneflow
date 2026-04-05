"""Feature extraction for chromatogram signals.

Extracts signal quality_enhanced metrics useful for quality_enhanced prediction
from Sanger sequencing trace files.
"""

from dataclasses import dataclass

import numpy as np

# Optional scipy imports
try:
    from scipy.ndimage import gaussian_filter1d

    HAS_SCIPY = True
except ImportError:
    HAS_SCIPY = False

    def gaussian_filter1d(input_array, sigma, **kwargs):
        """Simple Gaussian blur approximation without scipy."""
        # Use a simple moving average as fallback
        kernel_size = max(3, int(sigma * 3) | 1)  # Ensure odd
        kernel = np.ones(kernel_size) / kernel_size
        return np.convolve(input_array, kernel, mode="same")


@dataclass
class SignalFeatures:
    """Container for extracted signal features."""

    # Per-position features (arrays of length seq_len)
    signal_a: np.ndarray
    signal_t: np.ndarray
    signal_c: np.ndarray
    signal_g: np.ndarray

    # Signal quality_enhanced metrics (per position)
    snr: np.ndarray  # Signal-to-noise ratio
    peak_clarity: np.ndarray  # Max signal / second max
    channel_entropy: np.ndarray  # Entropy across 4 channels
    base_confidence: np.ndarray  # Confidence of base call

    # Peak shape metrics (per position)
    peak_height: np.ndarray  # Height of dominant peak
    peak_separation: np.ndarray  # Distance to nearest peak
    peak_width: np.ndarray  # Width of dominant peak

    # Derivative features (per position)
    signal_gradient: np.ndarray  # First derivative magnitude
    signal_curvature: np.ndarray  # Second derivative magnitude

    # Neighborhood context (per position)
    local_noise: np.ndarray  # Local noise estimate
    signal_smoothness: np.ndarray  # Local signal smoothness

    def to_tensor_dict(self, max_length: int = 1000) -> dict[str, np.ndarray]:
        """Convert to dictionary of tensors for model input.

        Args:
            max_length: Maximum sequence length (pad/truncate)

        Returns:
            Dictionary with signal arrays ready for model
        """

        def pad_or_truncate(arr: np.ndarray) -> np.ndarray:
            if len(arr) >= max_length:
                return arr[:max_length]
            return np.pad(arr, (0, max_length - len(arr)), mode="constant")

        # Stack main signals (4 channels)
        signals = np.stack(
            [
                pad_or_truncate(self.signal_a),
                pad_or_truncate(self.signal_t),
                pad_or_truncate(self.signal_c),
                pad_or_truncate(self.signal_g),
            ]
        )

        # Stack auxiliary features (8 channels)
        aux_features = np.stack(
            [
                pad_or_truncate(self.snr),
                pad_or_truncate(self.peak_clarity),
                pad_or_truncate(self.channel_entropy),
                pad_or_truncate(self.base_confidence),
                pad_or_truncate(self.peak_height),
                pad_or_truncate(self.signal_gradient),
                pad_or_truncate(self.local_noise),
                pad_or_truncate(self.signal_smoothness),
            ]
        )

        # Create mask
        actual_len = min(len(self.signal_a), max_length)
        mask = np.zeros(max_length, dtype=bool)
        mask[:actual_len] = True

        return {
            "signals": signals.astype(np.float32),
            "aux_features": aux_features.astype(np.float32),
            "mask": mask,
        }

    def to_extended_signals(self, max_length: int = 1000) -> np.ndarray:
        """Get extended signal array with all features.

        Returns array of shape (12, max_length) combining
        raw signals and derived features.
        """
        tensor_dict = self.to_tensor_dict(max_length)
        return np.concatenate([tensor_dict["signals"], tensor_dict["aux_features"]], axis=0)


class SignalFeatureExtractor:
    """Extracts features from chromatogram signals."""

    def __init__(
        self,
        noise_window: int = 10,
        smoothing_sigma: float = 1.0,
        peak_min_distance: int = 5,
    ):
        """Initialize signal feature extractor.

        Args:
            noise_window: Window size for local noise estimation
            smoothing_sigma: Gaussian smoothing sigma
            peak_min_distance: Minimum distance between peaks for detection
        """
        self.noise_window = noise_window
        self.smoothing_sigma = smoothing_sigma
        self.peak_min_distance = peak_min_distance

    def extract(
        self,
        signal_a: np.ndarray,
        signal_t: np.ndarray,
        signal_c: np.ndarray,
        signal_g: np.ndarray,
        peak_locations: np.ndarray,
        normalize: bool = True,
    ) -> SignalFeatures:
        """Extract features from chromatogram signals.

        Args:
            signal_a: Raw signal for adenine channel
            signal_t: Raw signal for thymine channel
            signal_c: Raw signal for cytosine channel
            signal_g: Raw signal for guanine channel
            peak_locations: Array of peak positions (base call locations)
            normalize: Whether to normalize signals

        Returns:
            SignalFeatures object with all computed features
        """
        # Extract signals at peak locations
        signals_at_peaks = self._extract_at_peaks(
            [signal_a, signal_t, signal_c, signal_g], peak_locations
        )
        sig_a, sig_t, sig_c, sig_g = signals_at_peaks

        # Normalize if requested
        if normalize:
            max_val = max(sig_a.max(), sig_t.max(), sig_c.max(), sig_g.max(), 1e-6)
            sig_a = sig_a / max_val
            sig_t = sig_t / max_val
            sig_c = sig_c / max_val
            sig_g = sig_g / max_val

        seq_len = len(sig_a)

        # Stack signals for vectorized operations
        all_signals = np.stack([sig_a, sig_t, sig_c, sig_g], axis=0)  # (4, seq_len)

        # Compute signal quality_enhanced metrics
        snr = self._compute_snr(all_signals)
        peak_clarity = self._compute_peak_clarity(all_signals)
        channel_entropy = self._compute_channel_entropy(all_signals)
        base_confidence = self._compute_base_confidence(all_signals)

        # Compute peak shape metrics
        peak_height = np.max(all_signals, axis=0)
        peak_separation = self._compute_peak_separation(peak_locations, seq_len)
        peak_width = self._estimate_peak_width(all_signals, peak_locations, signal_a, signal_t, signal_c, signal_g)

        # Compute derivative features
        signal_gradient = self._compute_gradient(all_signals)
        signal_curvature = self._compute_curvature(all_signals)

        # Compute neighborhood context
        local_noise = self._compute_local_noise(all_signals)
        signal_smoothness = self._compute_smoothness(all_signals)

        return SignalFeatures(
            signal_a=sig_a,
            signal_t=sig_t,
            signal_c=sig_c,
            signal_g=sig_g,
            snr=snr,
            peak_clarity=peak_clarity,
            channel_entropy=channel_entropy,
            base_confidence=base_confidence,
            peak_height=peak_height,
            peak_separation=peak_separation,
            peak_width=peak_width,
            signal_gradient=signal_gradient,
            signal_curvature=signal_curvature,
            local_noise=local_noise,
            signal_smoothness=signal_smoothness,
        )

    def _extract_at_peaks(
        self, signals: list[np.ndarray], peak_locations: np.ndarray
    ) -> list[np.ndarray]:
        """Extract signal values at peak positions."""
        result = []
        for signal in signals:
            if len(signal) == 0 or len(peak_locations) == 0:
                result.append(np.zeros(len(peak_locations)))
                continue

            valid_peaks = peak_locations[peak_locations < len(signal)]
            values = np.zeros(len(peak_locations))
            values[: len(valid_peaks)] = signal[valid_peaks]
            result.append(values)

        return result

    def _compute_snr(self, signals: np.ndarray) -> np.ndarray:
        """Compute signal-to-noise ratio per position.

        SNR = max_signal / (mean of other signals + epsilon)
        """
        max_signal = np.max(signals, axis=0)
        sorted_signals = np.sort(signals, axis=0)
        noise = np.mean(sorted_signals[:-1], axis=0)  # Mean of non-max signals
        return max_signal / (noise + 1e-6)

    def _compute_peak_clarity(self, signals: np.ndarray) -> np.ndarray:
        """Compute peak clarity as ratio of max to second max signal."""
        sorted_signals = np.sort(signals, axis=0)[::-1]  # Descending
        max_sig = sorted_signals[0]
        second_max = sorted_signals[1]
        return max_sig / (second_max + 1e-6)

    def _compute_channel_entropy(self, signals: np.ndarray) -> np.ndarray:
        """Compute entropy across the 4 channels per position.

        Low entropy = clear single peak
        High entropy = ambiguous base call
        """
        # Normalize to probabilities
        sums = np.sum(signals, axis=0, keepdims=True)
        probs = signals / (sums + 1e-6)

        # Shannon entropy
        log_probs = np.log2(probs + 1e-10)
        entropy = -np.sum(probs * log_probs, axis=0)

        # Normalize to [0, 1] (max entropy is log2(4) = 2)
        return entropy / 2.0

    def _compute_base_confidence(self, signals: np.ndarray) -> np.ndarray:
        """Compute confidence score for base call.

        Based on difference between max and second max signals.
        """
        sorted_signals = np.sort(signals, axis=0)[::-1]
        max_sig = sorted_signals[0]
        second_max = sorted_signals[1]

        # Confidence increases with gap between top two signals
        gap = max_sig - second_max
        total = max_sig + second_max + 1e-6

        return gap / total

    def _compute_peak_separation(
        self, peak_locations: np.ndarray, seq_len: int
    ) -> np.ndarray:
        """Compute distance to neighboring peaks."""
        if len(peak_locations) <= 1:
            return np.ones(seq_len) * 10.0

        separation = np.zeros(seq_len)

        for i in range(seq_len):
            if i == 0:
                sep = peak_locations[1] - peak_locations[0] if len(peak_locations) > 1 else 10
            elif i >= len(peak_locations) - 1:
                sep = peak_locations[-1] - peak_locations[-2] if len(peak_locations) > 1 else 10
            else:
                sep = min(
                    peak_locations[i] - peak_locations[i - 1],
                    peak_locations[i + 1] - peak_locations[i],
                )
            separation[i] = sep

        return separation

    def _estimate_peak_width(
        self,
        signals: np.ndarray,
        peak_locations: np.ndarray,
        signal_a: np.ndarray,
        signal_t: np.ndarray,
        signal_c: np.ndarray,
        signal_g: np.ndarray,
    ) -> np.ndarray:
        """Estimate peak width by analyzing raw signal around each peak."""
        seq_len = signals.shape[1]
        widths = np.ones(seq_len) * 5.0  # Default width

        if len(peak_locations) == 0:
            return widths

        # Combine all raw signals
        all_raw = [signal_a, signal_t, signal_c, signal_g]
        max_len = max(len(s) for s in all_raw) if all_raw else 0

        if max_len == 0:
            return widths

        for i, peak in enumerate(peak_locations):
            if i >= seq_len:
                break

            # Find which channel has the max at this position
            channel_vals = signals[:, i]
            best_channel = np.argmax(channel_vals)

            # Estimate width from raw signal
            raw_signal = all_raw[best_channel]
            if peak < len(raw_signal):
                width = self._measuREDACTED(raw_signal, peak)
                widths[i] = width

        return widths

    def _measuREDACTED(self, signal: np.ndarray, peak_idx: int) -> float:
        """Measure width of a peak at half maximum."""
        if peak_idx >= len(signal):
            return 5.0

        peak_val = signal[peak_idx]
        half_max = peak_val / 2

        # Find left boundary
        left = peak_idx
        while left > 0 and signal[left] > half_max:
            left -= 1

        # Find right boundary
        right = peak_idx
        while right < len(signal) - 1 and signal[right] > half_max:
            right += 1

        return float(right - left)

    def _compute_gradient(self, signals: np.ndarray) -> np.ndarray:
        """Compute signal gradient magnitude."""
        max_signal = np.max(signals, axis=0)

        if len(max_signal) < 3:
            return np.zeros(len(max_signal))

        # Smooth first
        smoothed = gaussian_filter1d(max_signal, sigma=self.smoothing_sigma)

        # Compute gradient
        gradient = np.gradient(smoothed)
        return np.abs(gradient)

    def _compute_curvature(self, signals: np.ndarray) -> np.ndarray:
        """Compute signal curvature (second derivative)."""
        max_signal = np.max(signals, axis=0)

        if len(max_signal) < 3:
            return np.zeros(len(max_signal))

        # Smooth first
        smoothed = gaussian_filter1d(max_signal, sigma=self.smoothing_sigma)

        # Second derivative
        curvature = np.gradient(np.gradient(smoothed))
        return np.abs(curvature)

    def _compute_local_noise(self, signals: np.ndarray) -> np.ndarray:
        """Estimate local noise level using moving standard deviation."""
        max_signal = np.max(signals, axis=0)
        seq_len = len(max_signal)

        if seq_len < self.noise_window:
            return np.std(max_signal) * np.ones(seq_len)

        noise = np.zeros(seq_len)
        half_win = self.noise_window // 2

        for i in range(seq_len):
            start = max(0, i - half_win)
            end = min(seq_len, i + half_win + 1)
            window = max_signal[start:end]
            noise[i] = np.std(window)

        return noise

    def _compute_smoothness(self, signals: np.ndarray) -> np.ndarray:
        """Compute local smoothness as inverse of local variance."""
        noise = self._compute_local_noise(signals)
        return 1.0 / (noise + 0.01)

    def extract_global_stats(
        self,
        signal_a: np.ndarray,
        signal_t: np.ndarray,
        signal_c: np.ndarray,
        signal_g: np.ndarray,
    ) -> dict[str, float]:
        """Extract global statistics from raw signals.

        Returns:
            Dictionary with global signal statistics
        """
        signals = [signal_a, signal_t, signal_c, signal_g]
        combined = np.concatenate([s for s in signals if len(s) > 0])

        if len(combined) == 0:
            return {
                "mean_intensity": 0.0,
                "max_intensity": 0.0,
                "std_intensity": 0.0,
                "signal_range": 0.0,
                "baseline_noise": 0.0,
            }

        # Estimate baseline as lower percentile
        baseline = np.percentile(combined, 10)
        noise = np.std(combined[combined < np.percentile(combined, 25)])

        return {
            "mean_intensity": float(np.mean(combined)),
            "max_intensity": float(np.max(combined)),
            "std_intensity": float(np.std(combined)),
            "signal_range": float(np.max(combined) - np.min(combined)),
            "baseline_noise": float(noise) if not np.isnan(noise) else 0.0,
        }
