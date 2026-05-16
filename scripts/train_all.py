#!/usr/bin/env python3
"""
Train all models sequentially.
Run: uv run python scripts/train_all.py
"""

import subprocess
import sys
from datetime import datetime

MODELS = [
    {
        "name": "Quality Classifier",
        "cmd": [
            "uv", "run", "python", "scripts/train_quality_classifier.py",
            "--data-dir", "datalake/datasets/quality_context",
            "--checkpoint-dir", "checkpoints/quality_classifier",
            "--epochs", "300",
            "--patience", "50",
        ],
    },
    {
        "name": "Taxonomy CNN",
        "cmd": [
            "uv", "run", "python", "scripts/train_taxonomy.py",
            "--data-dir", "datalake/curated",
            "--levels", "kingdom",
            "--output-dir", "checkpoints/taxonomy_cnn",
            "--epochs", "200",
        ],
    },
    {
        "name": "Taxonomy RF",
        "cmd": [
            "uv", "run", "python", "scripts/train_taxonomy_rf.py",
            "--data-dir", "datalake/curated",
            "--level", "kingdom",
            "--checkpoint-dir", "checkpoints/taxonomy_rf",
        ],
    },
]


def main():
    print("=" * 70)
    print(f"SEQUENTIAL TRAINING - Started at {datetime.now()}")
    print("=" * 70)

    results = []

    for i, model in enumerate(MODELS, 1):
        print(f"\n[{i}/{len(MODELS)}] Training: {model['name']}")
        print(f"Started at: {datetime.now()}")
        print("-" * 70)

        try:
            subprocess.run(model["cmd"], check=True)
            results.append((model["name"], "SUCCESS"))
            print(f"\n{model['name']} completed successfully!")
        except subprocess.CalledProcessError as e:
            results.append((model["name"], f"FAILED (code {e.returncode})"))
            print(f"\n{model['name']} failed with code {e.returncode}")
        except KeyboardInterrupt:
            print("\nTraining interrupted by user")
            sys.exit(1)

    print("\n" + "=" * 70)
    print(f"ALL TRAINING COMPLETE - Finished at {datetime.now()}")
    print("=" * 70)
    print("\nResults:")
    for name, status in results:
        print(f"  {name}: {status}")


if __name__ == "__main__":
    main()
