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
        "name": "Quality Enhanced",
        "cmd": [
            "uv", "run", "python", "scripts/train_quality_precomputed.py",
            "--data", "datalake/datasets/quality_enhanced",
            "--epochs", "200",
            "--patience", "50",
        ],
    },
    {
        "name": "Taxonomy Hierarchical",
        "cmd": [
            "uv", "run", "python", "scripts/train_taxonomy_hierarchical.py",
            "--dataset-dir", "datalake/datasets/taxonomy",
            "--checkpoint-dir", "checkpoints/taxonomy",
            "--epochs", "200",
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
