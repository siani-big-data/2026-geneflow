# GeneFlow Docker Infrastructure

Estructura modular de Docker para el ecosistema GeneFlow.

## Estructura

```
docker/
├── docker-compose.yml          # Orquestador principal
├── .env                        # Variables de entorno
├── core/                       # Infraestructura compartida
│   └── docker-compose.core.yml
├── apinet/                     # API .NET
│   └── docker-compose.apinet.yml
├── datalake/                   # Datalake
│   └── docker-compose.datalake.yml
├── ai/                         # Workers AI
│   └── docker-compose.ai.yml
└── analysis/                   # Workers de análisis
    └── docker-compose.analysis.yml
```

## Naming Convention

```
geneflow-{modulo}-{servicio}

Ejemplos:
- geneflow-core-redis
- geneflow-core-postgres
- geneflow-core-minio
- geneflow-apinet-api
- geneflow-datalake-consumer
- geneflow-ai-worker
- geneflow-analysis-trace
```

## Comandos

### Iniciar Infraestructura Core

```bash
cd docker
docker compose up -d
```

### Core + Admin Tools (pgAdmin, Redis Commander, Mailpit)

```bash
docker compose --profile admin up -d
```

### Core + Datalake

```bash
docker compose -f docker-compose.yml -f datalake/docker-compose.datalake.yml up -d
```

### Core + API .NET

```bash
docker compose -f docker-compose.yml -f apinet/docker-compose.apinet.yml up -d
```

### Todo junto

```bash
docker compose \
  -f docker-compose.yml \
  -f apinet/docker-compose.apinet.yml \
  -f datalake/docker-compose.datalake.yml \
  -f analysis/docker-compose.analysis.yml \
  up -d
```

### Ver logs

```bash
# Todos los servicios
docker compose logs -f

# Servicio específico
docker compose logs -f geneflow-core-redis
```

### Detener

```bash
# Detener servicios
docker compose down

# Detener y eliminar volúmenes (CUIDADO: borra datos)
docker compose down -v
```

## Puertos

| Servicio | Puerto | URL |
|----------|--------|-----|
| PostgreSQL | 5432 | localhost:5432 |
| Redis | 6379 | localhost:6379 |
| MinIO API | 9000 | http://localhost:9000 |
| MinIO Console | 9001 | http://localhost:9001 |
| pgAdmin | 5050 | http://localhost:5050 |
| Redis Commander | 8081 | http://localhost:8081 |
| Mailpit | 8025 | http://localhost:8025 |
| API .NET | 5286 | http://localhost:5286 |
| Datalake | 8080 | http://localhost:8080 |

## Credenciales por defecto (Desarrollo)

| Servicio | Usuario | Password |
|----------|---------|----------|
| PostgreSQL | geneflow | geneflow |
| MinIO | geneflow | geneflow123 |
| pgAdmin | admin@geneflow.dev | admin |

## Volúmenes

- `geneflow-redis-data`: Datos de Redis
- `geneflow-postgres-data`: Base de datos PostgreSQL
- `geneflow-minio-data`: Archivos en MinIO
- `geneflow-datalake-data`: Datos del datalake

## Red

Todos los servicios están conectados a la red `geneflow-network`.
