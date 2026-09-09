<div align="center">

<pre>
 ██████╗ ███████╗███╗   ██╗███████╗███████╗██╗      ██████╗ ██╗    ██╗
██╔════╝ ██╔════╝████╗  ██║██╔════╝██╔════╝██║     ██╔═══██╗██║    ██║
██║  ███╗█████╗  ██╔██╗ ██║█████╗  █████╗  ██║     ██║   ██║██║ █╗ ██║
██║   ██║██╔══╝  ██║╚██╗██║██╔══╝  ██╔══╝  ██║     ██║   ██║██║███╗██║
╚██████╔╝███████╗██║ ╚████║███████╗██║     ███████╗╚██████╔╝╚███╔███╔╝
 ╚═════╝ ╚══════╝╚═╝  ╚═══╝╚══════╝╚═╝     ╚══════╝ ╚═════╝  ╚══╝╚══╝
</pre>

**Plataforma SaaS distribuida y orientada a eventos para el análisis de secuencias genéticas Sanger**

[![Tech Stack](https://skillicons.dev/icons?i=dotnet,cs,nextjs,react,ts,tailwind,python,fastapi,pytorch,postgres,redis,docker)](https://skillicons.dev)

</div>

---

## 🧬 Sobre el proyecto

**GeneFlow** es una plataforma web colaborativa para el análisis de secuencias genéticas obtenidas mediante **secuenciación Sanger**. Reúne en un único entorno accesible desde el navegador la visualización interactiva de cromatogramas, la gestión colaborativa de estudios, un motor de análisis bioinformático extensible y capacidades de inteligencia artificial, manteniendo el diseño centrado en el usuario como criterio principal.

El análisis de datos Sanger sigue dependiendo, en buena medida, de aplicaciones de escritorio antiguas, monousuario y sin trabajo colaborativo. GeneFlow cubre ese hueco llevando al navegador un flujo de trabajo completo (carga, visualización, análisis, colaboración y clasificación automática) sobre una arquitectura de ingeniería del software contemporánea.

Bajo la interfaz, GeneFlow es un **sistema distribuido y orientado a eventos** formado por cinco servicios independientes que cooperan a través de un bus de eventos (**Redis Streams**) y un **registro inmutable de eventos** como fuente única de verdad, siguiendo los patrones **CQRS** y **Event Sourcing**.

## 🏗️ Arquitectura

```
                        ┌──────────────────────────┐
                        │    geneflow-frontend     │   Next.js · React · TS
                        │      (interfaz web)      │
                        └────────────┬─────────────┘
                                     │  REST  ·  SSE (tiempo real)
                        ┌────────────▼─────────────┐
                        │     geneflow-backend     │   .NET 8 · CQRS · DDD
                        │   (API + único productor │
                        │      de eventos)         │
                        └────────────┬─────────────┘
                                     │  publica eventos de dominio
      ┌──────────────────────────────┼──────────────────────────────┐
      │              BUS DE EVENTOS  ·  Redis Streams                 │
      │   users · studies · traces · alignments · ai · system ...     │
      └───────┬───────────────────┬───────────────────┬──────────────┘
              │ consume           │ consume           │ consume
      ┌───────▼────────┐  ┌───────▼────────┐  ┌───────▼────────────┐
      │ geneflow-      │  │ geneflow-ai    │  │ geneflow-datalake  │
      │ analysis       │  │ (IA · copiloto │  │ (event store       │
      │ (bioinformática)│  │  · BLAST · ML) │  │  inmutable · JSONL)│
      └────────────────┘  └────────────────┘  └────────────────────┘
```

El **servicio principal** (backend) es el único que produce eventos de dominio. El resto de servicios (motor de análisis, IA y datalake) los consumen de forma **asíncrona y desacoplada**, lo que permite escalar y evolucionar cada pieza por separado. La interfaz dialoga con el backend por **REST** y recibe el progreso de las operaciones largas por **Server-Sent Events (SSE)**. Los binarios pesados (trazas, cromatogramas) viajan por almacenamiento de objetos compatible con **S3 (MinIO)**, no por la mensajería.

## ✨ Características

- **Visualización interactiva de cromatogramas** sobre lienzo HTML5, con virtualización por ventana, zoom, minimapa y capas superpuestas (calidad, anotaciones, recortes, motivos).
- **Gestión colaborativa de estudios** con roles diferenciados, organizaciones, invitaciones, trazabilidad y control de versiones.
- **Carga y procesamiento de trazas** en los formatos habituales: AB1, SCF, FASTQ y FASTA.
- **Motor de análisis bioinformático**: evaluación de calidad (Phred, Q20/Q30), recorte (algoritmo de Mott), detección de heterocigotos (códigos IUPAC), marcos abiertos de lectura (ORF), motivos, sitios de restricción, alineamiento (Needleman-Wunsch y Smith-Waterman) y consenso.
- **Inteligencia artificial**: clasificación taxonómica jerárquica (redes convolucionales), detección de heterocigotos, clasificación de calidad por base, predicción de recorte y un **asistente conversacional** que coordina las herramientas de la plataforma y consulta bases de datos externas (BLAST/NCBI).
- **Arquitectura distribuida orientada a eventos**: CQRS + Event Sourcing, datalake con *write-ahead log* (WAL), cola de mensajes fallidos (DLQ), *replay* y proyecciones reconstruibles.
- **Seguridad**: autenticación JWT, inicio de sesión federado (Google y GitHub), doble factor (TOTP o correo), y suscripciones con pasarela de pago.
- **Experiencia de usuario**: interfaz multilingüe (español e inglés), accesible (ARIA, navegación por teclado), con tema claro y oscuro.
- **Calidad**: contenedorización con Docker, integración y entrega continuas (GitHub Actions) y una batería con más de 5.000 ejecuciones de prueba.

## 📦 Módulos del proyecto

Este repositorio reúne los **cinco servicios** que componen la plataforma. Cada carpeta contiene su propio `README.md` con el detalle técnico y las instrucciones de puesta en marcha.

### 🖥️ `geneflow-frontend` — Interfaz web
Aplicación web con la que interactúa el usuario. Ofrece el panel de trabajo, el visor de cromatogramas, la gestión de estudios y trazas, los flujos de análisis y el asistente conversacional.
- **Stack:** Next.js 16, React 19, TypeScript, Tailwind CSS v4.
- **Estructura:** rutas con App Router y segmento de idioma, estado de cliente con Zustand y estado de servidor con TanStack Query, visor de cromatogramas en `canvas`, y un cliente HTTP/SSE tipado hacia el backend.

### ⚙️ `geneflow-backend` — API y núcleo de negocio
Servicio principal y **único productor de eventos de dominio**. Gestiona identidad, perfiles, organizaciones, estudios, trazas, suscripciones y la coordinación general.
- **Stack:** .NET 8, ASP.NET Core, Entity Framework Core, PostgreSQL 16, Redis.
- **Estructura:** *Clean Architecture* en cuatro capas (Domain, Application, Infrastructure, API) sobre 15 contextos delimitados (DDD), con CQRS y patrón *Result*.

### 🔬 `geneflow-analysis` — Motor de análisis bioinformático
Trabajador orientado a eventos que procesa las trazas de secuenciación: lectura de formatos, calidad, recorte, heterocigotos, ORF, motivos, restricción, alineamiento y consenso.
- **Stack:** Python 3.12, BioPython, Redis Streams.
- **Estructura:** consumidores (*workers*) sobre grupos de Redis, *parsers* por formato, analizadores del dominio y publicación de resultados como eventos.

### 🤖 `geneflow-ai` — Servicio de inteligencia artificial
Capa inteligente de la plataforma: cuatro modelos de aprendizaje profundo y el **asistente conversacional** (copiloto) con coordinación de herramientas y búsqueda de homología (BLAST).
- **Stack:** Python 3.12, FastAPI, PyTorch, Redis Streams, Qdrant, proveedores de modelos de lenguaje.
- **Estructura:** módulo `copilot` (agente + herramientas), modelos de ML (clasificación taxonómica, heterocigotos, calidad, recorte) y puente con el motor de análisis.

### 🗄️ `geneflow-datalake` — Almacén inmutable de eventos
*Event store* que consume **todos** los eventos del bus y los persiste en JSONL. Es la fuente única de verdad: habilita *replay*, auditoría y proyecciones.
- **Stack:** Python 3.12, FastAPI, Redis Streams.
- **Estructura:** consumidor con deduplicación por `eventId`, *buffer* + WAL, particionado por categoría y día, reintentos con DLQ, *mounters* (proyecciones) y una API de consulta/operación.

## 🛠️ Tecnologías

| Capa / servicio | Tecnologías |
|---|---|
| Frontend | Next.js · React · TypeScript · Tailwind CSS |
| Backend | .NET 8 · ASP.NET Core · Entity Framework Core · C# |
| Análisis e IA | Python 3.12 · FastAPI · BioPython · PyTorch |
| Datos | PostgreSQL 16 · Redis (Streams y caché) · MinIO (S3) · Qdrant |
| Infraestructura | Docker · GitHub Actions (CI/CD) |

## 🚀 Puesta en marcha

Cada servicio se ejecuta de forma independiente y se contenedoriza con Docker. La infraestructura compartida (Redis, PostgreSQL, MinIO) se levanta con Docker Compose, y sobre ella se añaden los servicios necesarios para cada escenario. Consulta el `README.md` de cada módulo para los detalles concretos de configuración, variables de entorno y ejecución.

## 📄 Contexto académico

Proyecto desarrollado como **Trabajo Fin de Título (TFT)** del Grado en Ingeniería Informática de la **Escuela de Ingeniería Informática de la Universidad de Las Palmas de Gran Canaria (ULPGC)**.

**Autor:** Eduardo Marrero González

---

<div align="center">

*GeneFlow — leer, analizar y colaborar sobre secuencias Sanger en un único lugar.*

</div>
