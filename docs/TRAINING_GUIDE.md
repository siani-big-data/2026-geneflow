# GeneFlow AI - Guía de Entrenamiento

> Documentación completa del pipeline de datos y entrenamiento de modelos.

## Índice

1. [Arquitectura de Datos](#arquitectura-de-datos)
2. [Descarga de Secuencias (NCBI)](#descarga-de-secuencias-ncbi)
3. [Data Augmentation](#data-augmentation)
4. [Generación de Datasets](#generación-de-datasets)
5. [Modelos Disponibles](#modelos-disponibles)
6. [Entrenamiento](#entrenamiento)
7. [Comandos Útiles](#comandos-útiles)

---

## Arquitectura de Datos

```
datalake/
├── raw/                          # Secuencias descargadas de NCBI
│   ├── animalia/
│   │   ├── chordata/
│   │   │   └── mammalia/
│   │   │       └── primates/
│   │   │           └── hominidae/
│   │   │               └── homo/
│   │   │                   └── homo_sapiens/
│   │   │                       ├── 9606.fasta.gz    # Secuencias
│   │   │                       └── 9606.json        # Metadata
│   ├── plantae/
│   ├── fungi/
│   ├── monera/
│   └── protista/
├── augmented/                    # Secuencias con data augmentation
├── datasets/                     # Datasets precomputados para entrenamiento
│   ├── taxonomy_phylum/
│   ├── taxonomy_hierarchical_kingdom_phylum_class/
│   └── quality/
└── synthetic_traces/             # Trazas sintéticas (opcional)
```

---

## Descarga de Secuencias (NCBI)

### Cómo funciona

1. **Species Discovery**: Consulta NCBI y muestrea 200,000 secuencias por reino
2. **Extracción de especies**: Del título de cada secuencia extrae organismo + taxon_id
3. **Descarga adaptativa**: Especies raras (< 10 seq) → descarga todas; comunes → máx 50-200
4. **Estado persistente**: Guarda progreso en `harvest_state.json`

### Comando de descarga

```bash
# Descarga básica (10k especies, 50 seq/especie)
uv run python scripts/download_training_data.py \
  --download-sequences \
  --total-species 10000 \
  --sequences-per-species 50

# Descarga masiva
uv run python scripts/download_training_data.py \
  --download-sequences \
  --total-species 1000000 \
  --sequences-per-species 50
```

### Distribución por reinos

| Reino | Porcentaje | Descripción |
|-------|------------|-------------|
| Animalia | 30% | Metazoa |
| Plantae | 25% | Viridiplantae |
| Monera | 20% | Bacteria + Archaea |
| Fungi | 15% | Hongos |
| Protista | 10% | Eucariotas no clasificados arriba |

### Verificar estado de descarga

```bash
uv run python -c "
from pathlib import Path
from src.ml.datasets.downloader import HarvestState
state = HarvestState.load(Path('harvest_state.json'))
print(state.summary())
"
```

### Contar especies y secuencias en datalake

```bash
uv run python -c "
import json
from pathlib import Path
from collections import defaultdict

raw_dir = Path('datalake/raw')
kingdom_counts = defaultdict(lambda: {'species': 0, 'sequences': 0})

for json_file in raw_dir.rglob('*.json'):
    parts = json_file.relative_to(raw_dir).parts
    kingdom = parts[0] if parts else 'unknown'
    try:
        with open(json_file) as f:
            meta = json.load(f)
            kingdom_counts[kingdom]['species'] += 1
            kingdom_counts[kingdom]['sequences'] += meta.get('sequence_count', 0)
    except: pass

print('Por reino:')
for k in sorted(kingdom_counts.keys()):
    d = kingdom_counts[k]
    print(f'  {k}: {d[\"species\"]:,} especies, {d[\"sequences\"]:,} secuencias')
"
```

### NCBI Rate Limits

- Sin API key: 3 requests/segundo
- Con API key: 10 requests/segundo
- Múltiples API keys: N × 10 requests/segundo

Configurar en `.env`:
```env
NCBI_EMAIL=tu@email.com
NCBI_API_KEYS=key1,key2,key3
```

---

## Data Augmentation

### Propósito

Balancear el dataset generando más secuencias para especies con pocas muestras.

### Técnicas aplicadas

| Técnica | Descripción | Tasa |
|---------|-------------|------|
| Mutaciones puntuales | Cambio de bases aleatorias | 2% |
| Indels | Inserciones/deleciones pequeñas | 0.5% |
| Reverse complement | Cadena complementaria inversa | - |
| Subcortes | Fragmentos aleatorios (70-95%) | - |

### Comando

```bash
# Augmentar a 10 secuencias por especie
uv run python scripts/augment_sequences.py --target 10

# Augmentar a 20 secuencias por especie
uv run python scripts/augment_sequences.py --target 20

# Solo procesar 1000 especies (test)
uv run python scripts/augment_sequences.py --target 10 --max-species 1000

# Cambiar tasa de mutación
uv run python scripts/augment_sequences.py --target 10 --mutation-rate 0.03
```

### Parámetros

| Parámetro | Default | Descripción |
|-----------|---------|-------------|
| `--target` | 10 | Secuencias objetivo por especie |
| `--mutation-rate` | 0.02 | Tasa de mutación (2%) |
| `--max-aug-per-seq` | 5 | Máx augmentaciones por secuencia original |
| `--seed` | 42 | Semilla para reproducibilidad |

### Output

```
datalake/augmented/
├── animalia/...    # Misma estructura que raw/
├── plantae/...
└── ...
```

Cada archivo `.json` incluye:
```json
{
  "augmented": true,
  "original_count": 3,
  "augmented_count": 10
}
```

---

## Generación de Datasets

### Dataset de Taxonomía (un nivel)

```bash
uv run python scripts/generate_training_datasets.py \
  --taxonomy \
  --level phylum \
  --input datalake/raw \
  --output datalake/datasets/taxonomy_phylum
```

Niveles disponibles: `kingdom`, `phylum`, `class`, `order`, `family`, `genus`, `species`

### Dataset de Taxonomía Jerárquico (multi-nivel)

```bash
uv run python scripts/generate_training_datasets.py \
  --taxonomy-hierarchical \
  --levels kingdom phylum class \
  --min-samples-per-class 5 \
  --input datalake/raw \
  --output datalake/datasets/taxonomy_hierarchical_kingdom_phylum_class
```

### Dataset de Calidad (requiere trazas)

```bash
uv run python scripts/generate_training_datasets.py \
  --quality_enhanced \
  --input datalake/processed/traces
```

### Estructura del dataset generado

```
datalake/datasets/taxonomy_hierarchical_kingdom_phylum_class/
├── train.npz           # Datos de entrenamiento
├── val.npz             # Datos de validación
├── test.npz            # Datos de test
├── metadata.json       # Configuración y estadísticas
└── label_encoders.json # Mapeo de clases
```

---

## Modelos Disponibles

### TaxonomyClassifier (un nivel)

Clasificador CNN para un solo nivel taxonómico.

```python
from src.ml.models.taxonomy import TaxonomyClassifier, TaxonomyConfig

config = TaxonomyConfig(
    num_classes=34,  # ej: 34 phyla
    max_seq_length=2000,
    embedding_dim=128,
    num_filters=256,
)
model = TaxonomyClassifier(config)
```

### HierarchicalTaxonomyClassifier (multi-nivel)

Clasificador multi-cabeza para clasificación simultánea en varios niveles.

```
Secuencia DNA → [Encoder CNN compartido] → Representación
                                              ↓
                    ┌─────────┬─────────┬─────────┐
                    ↓         ↓         ↓         ↓
               [Kingdom] [Phylum]  [Class]   [...]
```

**Características:**
- Encoder compartido con conexiones residuales
- Una cabeza de clasificación por nivel
- Loss jerárquico (penaliza inconsistencias)

```python
from src.ml.models.taxonomy import HierarchicalTaxonomyClassifier, HierarchicalTaxonomyConfig

config = HierarchicalTaxonomyConfig(
    num_classes_per_level={"kingdom": 5, "phylum": 34, "class": 96},
    max_seq_length=2000,
    embedding_dim=128,
)
model = HierarchicalTaxonomyClassifier(config)
```

### QualityPredictor (pendiente)

Predice calidad de secuenciación. Requiere datos de trazas/cromatogramas.

---

## Entrenamiento

### Entrenar TaxonomyClassifier

```bash
uv run python scripts/train_taxonomy_enhanced.py \
  --data-dir datalake/raw \
  --level phylum \
  --epochs 50 \
  --batch-size 32
```

### Entrenar HierarchicalTaxonomyClassifier

**Opción A: Desde datos raw**
```bash
uv run python scripts/train_taxonomy_hierarchical.py \
  --data-dir datalake/raw \
  --levels kingdom phylum class \
  --epochs 50 \
  --batch-size 32
```

**Opción B: Desde dataset precomputado (más rápido)**
```bash
uv run python scripts/train_taxonomy_hierarchical.py \
  --dataset-dir datalake/datasets/taxonomy_hierarchical_kingdom_phylum_class \
  --levels kingdom phylum class \
  --epochs 50 \
  --batch-size 32
```

### Parámetros de entrenamiento

| Parámetro | Default | Descripción |
|-----------|---------|-------------|
| `--epochs` | 50 | Número de épocas |
| `--batch-size` | 32 | Tamaño de batch |
| `--lr` | 0.001 | Learning rate |
| `--patience` | 10 | Early stopping patience |
| `--output-dir` | checkpoints/ | Directorio de salida |

### Output del entrenamiento

```
checkpoints/hierarchical_kingdom_phylum_class_20260331_120000/
├── config.json         # Configuración del modelo
├── best.pt             # Mejor modelo (por val_loss)
├── final.pt            # Modelo final
├── history.json        # Historial de métricas
├── loss_curve.png      # Gráfica de loss
├── accuracy_per_level.png
└── training_summary.png
```

### Resultados típicos (HierarchicalTaxonomyClassifier)

Con ~18k muestras, 10 epochs:
- Kingdom: 92.6% accuracy (5 clases)
- Phylum: 73.6% accuracy (34 clases)
- Class: 44.7% accuracy (96 clases)

Con más datos y epochs, mejora significativamente.

---

## Comandos Útiles

### Ver estado actual del datalake

```bash
uv run python -c "
from pathlib import Path
from src.ml.datasets.downloader import HarvestState
state = HarvestState.load(Path('harvest_state.json'))
print(state.summary())
"
```

### Contar secuencias totales

```bash
find datalake/raw -name "*.fasta.gz" | wc -l
```

### Ver distribución de secuencias por especie

```bash
uv run python -c "
import json
from pathlib import Path
from collections import Counter

counts = []
for f in Path('datalake/raw').rglob('*.json'):
    try:
        with open(f) as j:
            counts.append(json.load(j).get('sequence_count', 0))
    except: pass

c = Counter(counts)
print('Distribución de secuencias por especie:')
for n in sorted(c.keys())[:20]:
    print(f'  {n} seq: {c[n]} especies')
"
```

### Limpiar y reiniciar descarga

```bash
# Backup del estado actual
cp harvest_state.json harvest_state.json.backup

# Reiniciar (CUIDADO: borra progreso)
rm harvest_state.json
```

### Verificar integridad de archivos

```bash
uv run python -c "
import gzip
from pathlib import Path

broken = []
for f in Path('datalake/raw').rglob('*.fasta.gz'):
    try:
        with gzip.open(f, 'rt') as gz:
            _ = gz.read(100)
    except:
        broken.append(f)

print(f'Archivos corruptos: {len(broken)}')
for b in broken[:10]:
    print(f'  {b}')
"
```

---

## Flujo de Trabajo Recomendado

1. **Descargar secuencias**
   ```bash
   uv run python scripts/download_training_data.py --download-sequences --total-species 50000
   ```

2. **Augmentar datos** (opcional, para balancear)
   ```bash
   uv run python scripts/augment_sequences.py --target 10
   ```

3. **Generar dataset precomputado**
   ```bash
   uv run python scripts/generate_training_datasets.py --taxonomy-hierarchical --levels kingdom phylum class
   ```

4. **Entrenar modelo**
   ```bash
   uv run python scripts/train_taxonomy_hierarchical.py --dataset-dir datalake/datasets/taxonomy_hierarchical_kingdom_phylum_class --epochs 50
   ```

5. **Evaluar resultados** en `checkpoints/`

---

## Notas Técnicas

### NCBI Sequence Discovery

El sistema usa "sample-based discovery":
- Consulta NCBI: `{kingdom}[Organism] AND 100:50000[Sequence Length] AND biomol_genomic[Properties]`
- Muestrea 200,000 IDs
- Extrae organismo del título de cada secuencia
- Agrupa por especie y cuenta disponibles

### Adaptive Download Targets

| Secuencias disponibles | Target |
|------------------------|--------|
| < 10 (rara) | Todas |
| < 100 | 10 |
| < 1000 | 50 |
| >= 1000 (común) | 100-200 |

Esto asegura diversidad: no sobre-muestreamos especies comunes.

### Hierarchical Loss

El loss jerárquico penaliza inconsistencias taxonómicas:
```
L_total = L_kingdom + L_phylum + L_class + λ * L_consistency
```

Donde `L_consistency` penaliza predecir "Mammalia" (clase) sin predecir "Chordata" (phylum).

---

## Troubleshooting

### "Empty response" de NCBI

NCBI ocasionalmente devuelve respuestas vacías. El sistema reintenta automáticamente.

### Error de path en Windows

Algunos nombres de especies tienen caracteres inválidos. El sistema los sanitiza automáticamente (`sanitize_path_component`).

### Out of memory en entrenamiento

Reduce `--batch-size` o `--max-seq-length`.

### Descarga muy lenta

- Añade más API keys en `.env`
- Verifica conexión a internet
- NCBI puede estar lento (intenta más tarde)
