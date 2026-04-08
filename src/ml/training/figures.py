"""Publication-ready figure generation for ML training results.

Generates high-quality figures suitable for academic papers, including:
- Confusion matrices
- ROC and PR curves
- Feature importance plots
- Training curves
- Model comparison tables
"""

from pathlib import Path
from typing import TYPE_CHECKING, Literal

import numpy as np

if TYPE_CHECKING:
    from src.ml.training.results import Predictions, TrainingResult

# Type alias for figure format
FigFormat = Literal["pdf", "png", "svg", "eps"]


class PaperFigures:
    """Generate publication-ready figures from training results.

    Follows academic paper conventions:
    - Vector formats (PDF, SVG) for line plots
    - High DPI (300+) for raster when needed
    - Consistent font sizes and styles
    - Colorblind-friendly palettes
    """

    # Limits for readable figures
    MAX_CLASSES_CONFUSION_MATRIX = 50  # Beyond this, confusion matrix is unreadable
    MAX_CLASSES_ROC_PR = 20  # Beyond this, ROC/PR curves are cluttered
    MAX_CLASSES_CELL_ANNOTATIONS = 15  # Beyond this, skip cell text
    MAX_CLASSES_DISTRIBUTION = 30  # Max classes to show in distribution plot

    # Colorblind-friendly palette (based on Wong 2011)
    COLORS = [
        "#0072B2",  # Blue
        "#E69F00",  # Orange
        "#009E73",  # Green
        "#CC79A7",  # Pink
        "#F0E442",  # Yellow
        "#56B4E9",  # Sky blue
        "#D55E00",  # Vermillion
        "#000000",  # Black
    ]

    # Paper-ready style settings
    STYLE = {
        "font.family": "serif",
        "font.size": 10,
        "axes.titlesize": 11,
        "axes.labelsize": 10,
        "xtick.labelsize": 9,
        "ytick.labelsize": 9,
        "legend.fontsize": 9,
        "figure.titlesize": 12,
        "axes.linewidth": 0.8,
        "lines.linewidth": 1.5,
        "axes.grid": True,
        "grid.alpha": 0.3,
        "grid.linewidth": 0.5,
    }

    def __init__(
        self,
        output_dir: str | Path,
        format: FigFormat = "pdf",
        dpi: int = 300,
        style: str = "seaborn-v0_8-whitegrid",
    ):
        """Initialize figure generator.

        Args:
            output_dir: Directory to save figures
            format: Output format (pdf recommended for papers)
            dpi: Resolution for raster formats
            style: Matplotlib style to use
        """
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.format = format
        self.dpi = dpi
        self.style = style

        # Lazy import matplotlib
        self._plt = None
        self._sns = None

    @property
    def plt(self):
        if self._plt is None:
            import matplotlib
            matplotlib.use("Agg")
            import matplotlib.pyplot as plt
            plt.rcParams.update(self.STYLE)
            if self.style in plt.style.available:
                plt.style.use(self.style)
            self._plt = plt
        return self._plt

    @property
    def sns(self):
        if self._sns is None:
            try:
                import seaborn as sns
                sns.set_palette(self.COLORS)
                self._sns = sns
            except ImportError:
                self._sns = None
        return self._sns

    def _save_fig(self, fig, name: str) -> Path:
        """Save figure with consistent settings."""
        path = self.output_dir / f"{name}.{self.format}"
        fig.savefig(
            path,
            format=self.format,
            dpi=self.dpi,
            bbox_inches="tight",
            facecolor="white",
            edgecolor="none",
        )
        self.plt.close(fig)
        return path

    def confusion_matrix(
        self,
        cm: np.ndarray,
        class_names: list[str],
        normalize: bool = True,
        title: str = "Confusion Matrix",
        figsize: tuple | None = None,
        cmap: str = "Blues",
        show_values: bool | None = None,
    ) -> Path | None:
        """Generate confusion matrix figure.

        Args:
            cm: Confusion matrix array (n_classes x n_classes)
            class_names: List of class names
            normalize: Whether to normalize by row (true labels)
            title: Figure title
            figsize: Figure size in inches (auto-calculated if None)
            cmap: Colormap name
            show_values: Whether to show values in cells (auto if None)

        Returns:
            Path to saved figure, or None if too many classes
        """
        n_classes = len(class_names)

        # Skip if too many classes
        if n_classes > self.MAX_CLASSES_CONFUSION_MATRIX:
            print(
                f"  (Confusion matrix skipped: {n_classes} classes "
                f"> {self.MAX_CLASSES_CONFUSION_MATRIX} limit)"
            )
            return None

        plt = self.plt

        # Auto-calculate figsize based on number of classes
        if figsize is None:
            size = max(8, min(20, n_classes * 0.4))
            figsize = (size, size * 0.8)

        fig, ax = plt.subplots(figsize=figsize)

        if normalize:
            cm_display = cm.astype(float) / cm.sum(axis=1, keepdims=True)
            cm_display = np.nan_to_num(cm_display)
            fmt = ".2f"
            vmin, vmax = 0, 1
        else:
            cm_display = cm
            fmt = "d"
            vmin, vmax = 0, cm.max()

        im = ax.imshow(
            cm_display, interpolation="nearest", cmap=cmap, vmin=vmin, vmax=vmax
        )

        # Add colorbar
        cbar = fig.colorbar(im, ax=ax, fraction=0.046, pad=0.04)
        ylabel = "Proportion" if normalize else "Count"
        cbar.ax.set_ylabel(ylabel, rotation=-90, va="bottom")

        # Labels
        ax.set_xticks(np.arange(n_classes))
        ax.set_yticks(np.arange(n_classes))
        fontsize = max(4, 10 - n_classes // 10)
        ax.set_xticklabels(class_names, rotation=45, ha="right", fontsize=fontsize)
        ax.set_yticklabels(class_names, fontsize=fontsize)

        ax.set_xlabel("Predicted Label")
        ax.set_ylabel("True Label")
        ax.set_title(title, fontweight="bold")

        # Auto-determine show_values based on class count
        if show_values is None:
            show_values = n_classes <= self.MAX_CLASSES_CELL_ANNOTATIONS

        # Add text annotations (only for small matrices)
        if show_values:
            thresh = (vmax + vmin) / 2
            for i in range(n_classes):
                for j in range(n_classes):
                    val = cm_display[i, j]
                    text = f"{val:{fmt}}" if normalize else f"{val}"
                    ax.text(j, i, text, ha="center", va="center",
                           color="white" if val > thresh else "black",
                           fontsize=max(4, 8 - n_classes // 5))

        fig.tight_layout()
        return self._save_fig(fig, "confusion_matrix")

    def roc_curves(
        self,
        y_true: np.ndarray,
        y_proba: np.ndarray,
        class_names: list[str],
        title: str = "ROC Curves",
        figsize: tuple = (8, 6),
    ) -> Path | None:
        """Generate ROC curves for multi-class classification.

        Args:
            y_true: True labels (n_samples,)
            y_proba: Predicted probabilities (n_samples, n_classes)
            class_names: List of class names
            title: Figure title
            figsize: Figure size

        Returns:
            Path to saved figure, or None if too many classes
        """
        from collections import Counter

        from sklearn.metrics import auc, roc_curve
        from sklearn.preprocessing import label_binarize

        n_classes = len(class_names)

        # Skip if too many classes
        if n_classes > self.MAX_CLASSES_ROC_PR:
            # Select top N classes by frequency
            class_counts = Counter(y_true)
            top_classes = [cls for cls, _ in class_counts.most_common(self.MAX_CLASSES_ROC_PR)]
            print(
                f"  (ROC curves: showing top {self.MAX_CLASSES_ROC_PR} "
                f"of {n_classes} classes by frequency)"
            )
        else:
            top_classes = list(range(n_classes))

        plt = self.plt
        fig, ax = plt.subplots(figsize=figsize)

        # Binarize labels for multi-class
        if n_classes > 2:
            y_true_bin = label_binarize(y_true, classes=range(n_classes))
        else:
            y_true_bin = y_true.reshape(-1, 1)
            if y_proba.ndim > 1:
                y_proba = y_proba[:, 1].reshape(-1, 1)
            else:
                y_proba = y_proba.reshape(-1, 1)

        # Compute ROC for selected classes
        fpr, tpr, roc_auc = {}, {}, {}
        for i in top_classes:
            if i >= y_proba.shape[1]:
                continue
            fpr[i], tpr[i], _ = roc_curve(
                y_true_bin[:, i] if y_true_bin.ndim > 1 else y_true_bin,
                y_proba[:, i] if y_proba.ndim > 1 else y_proba,
            )
            roc_auc[i] = auc(fpr[i], tpr[i])

        # Plot each class
        for idx, i in enumerate(top_classes):
            if i not in fpr:
                continue
            name = class_names[i] if i < len(class_names) else f"Class {i}"
            color = self.COLORS[idx % len(self.COLORS)]
            ax.plot(fpr[i], tpr[i], color=color, lw=1.5,
                   label=f"{name} (AUC = {roc_auc[i]:.3f})")

        # Diagonal
        ax.plot([0, 1], [0, 1], "k--", lw=1, alpha=0.5, label="Random")

        ax.set_xlim([0, 1])
        ax.set_ylim([0, 1.02])
        ax.set_xlabel("False Positive Rate")
        ax.set_ylabel("True Positive Rate")
        ax.set_title(title, fontweight="bold")

        # Legend outside if many classes
        if len(top_classes) > 4:
            ax.legend(loc="center left", bbox_to_anchor=(1, 0.5), fontsize=8)
        else:
            ax.legend(loc="lower right")

        fig.tight_layout()
        return self._save_fig(fig, "roc_curves")

    def precision_recall_curves(
        self,
        y_true: np.ndarray,
        y_proba: np.ndarray,
        class_names: list[str],
        title: str = "Precision-Recall Curves",
        figsize: tuple = (8, 6),
    ) -> Path | None:
        """Generate Precision-Recall curves."""
        from collections import Counter

        from sklearn.metrics import average_precision_score, precision_recall_curve
        from sklearn.preprocessing import label_binarize

        n_classes = len(class_names)

        # Skip if too many classes - select top N by frequency
        if n_classes > self.MAX_CLASSES_ROC_PR:
            class_counts = Counter(y_true)
            top_classes = [cls for cls, _ in class_counts.most_common(self.MAX_CLASSES_ROC_PR)]
            print(
                f"  (PR curves: showing top {self.MAX_CLASSES_ROC_PR} "
                f"of {n_classes} classes by frequency)"
            )
        else:
            top_classes = list(range(n_classes))

        plt = self.plt
        fig, ax = plt.subplots(figsize=figsize)

        # Binarize labels
        if n_classes > 2:
            y_true_bin = label_binarize(y_true, classes=range(n_classes))
        else:
            y_true_bin = y_true.reshape(-1, 1)
            if y_proba.ndim > 1:
                y_proba = y_proba[:, 1].reshape(-1, 1)
            else:
                y_proba = y_proba.reshape(-1, 1)

        # Compute PR for selected classes
        for idx, i in enumerate(top_classes):
            if i >= y_proba.shape[1]:
                continue
            name = class_names[i] if i < len(class_names) else f"Class {i}"
            precision, recall, _ = precision_recall_curve(
                y_true_bin[:, i] if y_true_bin.ndim > 1 else y_true_bin,
                y_proba[:, i] if y_proba.ndim > 1 else y_proba,
            )
            ap = average_precision_score(
                y_true_bin[:, i] if y_true_bin.ndim > 1 else y_true_bin,
                y_proba[:, i] if y_proba.ndim > 1 else y_proba,
            )

            color = self.COLORS[idx % len(self.COLORS)]
            ax.plot(recall, precision, color=color, lw=1.5,
                   label=f"{name} (AP = {ap:.3f})")

        ax.set_xlim([0, 1])
        ax.set_ylim([0, 1.02])
        ax.set_xlabel("Recall")
        ax.set_ylabel("Precision")
        ax.set_title(title, fontweight="bold")

        if len(top_classes) > 4:
            ax.legend(loc="center left", bbox_to_anchor=(1, 0.5), fontsize=8)
        else:
            ax.legend(loc="lower left")

        fig.tight_layout()
        return self._save_fig(fig, "precision_recall_curves")

    def featuREDACTED(
        self,
        importance: dict[str, float],
        top_n: int = 20,
        title: str = "Feature Importance",
        figsize: tuple = (10, 8),
        horizontal: bool = True,
    ) -> Path:
        """Generate feature importance bar plot.

        Args:
            importance: Dict mapping feature names to importance scores
            top_n: Number of top features to show
            title: Figure title
            figsize: Figure size
            horizontal: Whether to use horizontal bars

        Returns:
            Path to saved figure
        """
        plt = self.plt
        fig, ax = plt.subplots(figsize=figsize)

        # Sort and get top N
        sorted_imp = sorted(importance.items(), key=lambda x: x[1], reverse=True)[:top_n]
        names, values = zip(*sorted_imp)

        # Reverse for horizontal (so highest is at top)
        if horizontal:
            names = names[::-1]
            values = values[::-1]

        y_pos = np.arange(len(names))
        colors = [self.COLORS[0]] * len(names)

        if horizontal:
            ax.barh(y_pos, values, color=colors, edgecolor="none", height=0.7)
            ax.set_yticks(y_pos)
            ax.set_yticklabels(names)
            ax.set_xlabel("Importance")
        else:
            ax.bar(y_pos, values, color=colors, edgecolor="none", width=0.7)
            ax.set_xticks(y_pos)
            ax.set_xticklabels(names, rotation=45, ha="right")
            ax.set_ylabel("Importance")

        ax.set_title(title, fontweight="bold")
        fig.tight_layout()
        return self._save_fig(fig, "featuREDACTED")

    def training_curves(
        self,
        history: list[dict],
        metrics: list[str] = ["loss", "accuracy"],
        title: str = "Training Curves",
        figsize: tuple = (12, 4),
    ) -> Path:
        """Generate training curves from epoch history.

        Args:
            history: List of dicts with metrics per epoch
            metrics: List of metrics to plot (will plot train/val for each)
            title: Figure title
            figsize: Figure size

        Returns:
            Path to saved figure
        """
        plt = self.plt

        n_metrics = len(metrics)
        fig, axes = plt.subplots(1, n_metrics, figsize=figsize)
        if n_metrics == 1:
            axes = [axes]

        epochs = [h.get("epoch", i + 1) for i, h in enumerate(history)]

        for ax, metric in zip(axes, metrics):
            train_key = f"train_{metric}"
            val_key = f"val_{metric}"

            # Try different key formats
            train_vals = [h.get(train_key) or h.get(f"train{metric}") for h in history]
            val_vals = [h.get(val_key) or h.get(f"val{metric}") for h in history]

            if any(v is not None for v in train_vals):
                train_clean = [(e, v) for e, v in zip(epochs, train_vals) if v is not None]
                if train_clean:
                    e, v = zip(*train_clean)
                    ax.plot(e, v, color=self.COLORS[0], lw=1.5, label="Train")

            if any(v is not None for v in val_vals):
                val_clean = [(e, v) for e, v in zip(epochs, val_vals) if v is not None]
                if val_clean:
                    e, v = zip(*val_clean)
                    ax.plot(e, v, color=self.COLORS[1], lw=1.5, label="Validation")

            ax.set_xlabel("Epoch")
            ax.set_ylabel(metric.replace("_", " ").title())
            ax.set_title(metric.replace("_", " ").title(), fontweight="bold")
            ax.legend()

            # Set y limits for accuracy
            if "accuracy" in metric.lower():
                ax.set_ylim([0, 1.05])

        fig.suptitle(title, fontweight="bold", y=1.02)
        fig.tight_layout()
        return self._save_fig(fig, "training_curves")

    def class_distribution(
        self,
        class_counts: dict[str, int],
        title: str = "Class Distribution",
        figsize: tuple | None = None,
        max_classes: int | None = None,
    ) -> Path:
        """Generate class distribution bar plot.

        For datasets with many classes, shows only top N classes by count.
        """
        plt = self.plt

        # Sort by count
        sorted_counts = sorted(class_counts.items(), key=lambda x: -x[1])

        # Limit classes if too many
        max_classes = max_classes or self.MAX_CLASSES_DISTRIBUTION
        total_classes = len(sorted_counts)
        if total_classes > max_classes:
            sorted_counts = sorted_counts[:max_classes]
            title = f"{title} (Top {max_classes} of {total_classes})"
            print(
                f"  (Class distribution: showing top {max_classes} "
                f"of {total_classes} classes)"
            )

        names, counts = zip(*sorted_counts)

        # Auto-calculate figsize based on number of classes
        if figsize is None:
            width = max(10, min(20, len(names) * 0.4))
            figsize = (width, 6)

        fig, ax = plt.subplots(figsize=figsize)

        colors = [self.COLORS[i % len(self.COLORS)] for i in range(len(names))]

        bars = ax.bar(range(len(names)), counts, color=colors, edgecolor="none")

        ax.set_xticks(range(len(names)))
        fontsize = max(6, 10 - len(names) // 10)
        ax.set_xticklabels(names, rotation=45, ha="right", fontsize=fontsize)
        ax.set_xlabel("Class")
        ax.set_ylabel("Count")
        ax.set_title(title, fontweight="bold")

        # Add count labels on bars (only if not too many)
        if len(names) <= 20:
            for bar, count in zip(bars, counts):
                ax.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + max(counts) * 0.01,
                       f"{count:,}", ha="center", va="bottom", fontsize=8)

        fig.tight_layout()
        return self._save_fig(fig, "class_distribution")

    def metrics_comparison(
        self,
        results: dict[str, dict],
        metrics: list[str] = ["accuracy", "f1_macro"],
        title: str = "Model Comparison",
        figsize: tuple = (10, 6),
    ) -> Path:
        """Generate comparison bar plot for multiple models.

        Args:
            results: Dict mapping model names to metric dicts
            metrics: List of metrics to compare
            title: Figure title
            figsize: Figure size

        Returns:
            Path to saved figure
        """
        plt = self.plt
        fig, ax = plt.subplots(figsize=figsize)

        model_names = list(results.keys())
        n_models = len(model_names)
        n_metrics = len(metrics)

        x = np.arange(n_models)
        width = 0.8 / n_metrics

        for i, metric in enumerate(metrics):
            values = [results[m].get(metric, 0) for m in model_names]
            offset = (i - n_metrics / 2 + 0.5) * width
            bars = ax.bar(x + offset, values, width, label=metric.replace("_", " ").title(),
                         color=self.COLORS[i % len(self.COLORS)])

            # Add value labels
            for bar, val in zip(bars, values):
                ax.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 0.01,
                       f"{val:.3f}", ha="center", va="bottom", fontsize=8)

        ax.set_xticks(x)
        ax.set_xticklabels(model_names, rotation=45, ha="right")
        ax.set_ylabel("Score")
        ax.set_title(title, fontweight="bold")
        ax.legend(loc="upper right")
        ax.set_ylim([0, 1.15])

        fig.tight_layout()
        return self._save_fig(fig, "metrics_comparison")

    def generate_all(
        self,
        result: "TrainingResult",
        predictions: "Predictions | None" = None,
    ) -> list[Path]:
        """Generate all applicable figures for a training result.

        Args:
            result: TrainingResult object
            predictions: Optional predictions for ROC/PR curves

        Returns:
            List of paths to generated figures
        """

        paths: list[Path] = []

        def _add_if_valid(path: Path | None) -> None:
            if path is not None:
                paths.append(path)

        # Confusion matrix
        if result.confusion_matrix is not None:
            _add_if_valid(self.confusion_matrix(
                result.confusion_matrix,
                result.dataset.class_names,
            ))

        # ROC curves (if predictions available)
        if predictions is not None and predictions.y_proba is not None:
            if result.dataset.task == "classification":
                try:
                    _add_if_valid(self.roc_curves(
                        predictions.y_true,
                        predictions.y_proba,
                        result.dataset.class_names,
                    ))
                    _add_if_valid(self.precision_recall_curves(
                        predictions.y_true,
                        predictions.y_proba,
                        result.dataset.class_names,
                    ))
                except Exception as e:
                    print(f"Warning: Could not generate ROC/PR curves: {e}")

        # Feature importance
        if result.featuREDACTED:
            _add_if_valid(self.featuREDACTED(result.featuREDACTED))

        # Training curves
        if result.history:
            history_dicts = [h.to_dict() if hasattr(h, "to_dict") else h for h in result.history]
            _add_if_valid(self.training_curves(history_dicts))

        # Class distribution
        if result.dataset.class_distribution:
            _add_if_valid(self.class_distribution(result.dataset.class_distribution))

        print(f"Generated {len(paths)} figures in {self.output_dir}")
        return paths


def generate_latex_table(
    results: dict[str, dict],
    metrics: list[str] = ["accuracy", "f1_macro", "precision_macro", "recall_macro"],
    caption: str = "Model Performance Comparison",
    label: str = "tab:results",
) -> str:
    """Generate LaTeX table for model comparison.

    Args:
        results: Dict mapping model names to metric dicts
        metrics: List of metrics to include
        caption: Table caption
        label: LaTeX label

    Returns:
        LaTeX table string
    """
    # Header
    header_metrics = [m.replace("_", " ").title() for m in metrics]
    header = " & ".join(["Model"] + header_metrics) + r" \\"

    # Rows
    rows = []
    for model, values in results.items():
        row_vals = [model]
        for metric in metrics:
            val = values.get(metric, 0)
            row_vals.append(f"{val:.3f}")
        rows.append(" & ".join(row_vals) + r" \\")

    # Assemble table
    col_spec = "l" + "c" * len(metrics)
    table = f"""\\begin{{table}}[htbp]
\\centering
\\caption{{{caption}}}
\\label{{{label}}}
\\begin{{tabular}}{{{col_spec}}}
\\toprule
{header}
\\midrule
{chr(10).join(rows)}
\\bottomrule
\\end{{tabular}}
\\end{{table}}"""

    return table
