# Plan Maestro de Tests - GeneFlow.ApiNet2

## Objetivos de Cobertura por Capa

| Capa | Line Coverage | Branch Coverage | Method Coverage |
|------|---------------|-----------------|-----------------|
| **Domain** | **≥95%** | **≥90%** | **≥98%** |
| **Application (Handlers)** | **≥90%** | **≥85%** | **≥95%** |
| **Infrastructure** | **≥75%** | **≥70%** | **≥85%** |
| **API (Endpoints)** | **≥85%** | **≥80%** | **≥90%** |
| **Global** | **≥85%** | **≥80%** | **≥90%** |

### Por Tipo de Código

| Código | Line | Branch | Justificación |
|--------|------|--------|---------------|
| Entidades/Aggregates | 95%+ | 90%+ | Core del negocio |
| Value Objects | 98%+ | 95%+ | Inmutables, fáciles de testear |
| Enumerations | 100% | 100% | Pocos casos, todos testeables |
| Command Handlers | 90%+ | 85%+ | Cada path debe testearse |
| Query Handlers | 85%+ | 80%+ | Menos ramas típicamente |
| Validators | 95%+ | 90%+ | Cada regla debe validarse |
| Repositories | 75%+ | 70%+ | Integración con DB |
| External Services | 70%+ | 65%+ | Mocks/Stubs necesarios |

### Thresholds CI/CD

```yaml
# Fallar build si:
line_coverage < 80%      # Error
line_coverage < 85%      # Warning
branch_coverage < 75%    # Error
branch_coverage < 80%    # Warning
```

---

## Inventario de Módulos

| # | Módulo | LOC Domain | LOC Application | Tests Existentes | Cobertura Actual | Plan |
|---|--------|------------|-----------------|------------------|------------------|------|
| 1 | **Pipelines** | 1,554 | ~1,200 | 0 | 0% | `01_PIPELINES_TEST_PLAN.md` |
| 2 | **Identity** | 2,161 | ~1,100 | ~30 | ~25% | `02_IDENTITY_TEST_PLAN.md` |
| 3 | **Studies** | 1,547 | ~1,400 | ~25 | ~30% | `03_STUDIES_TEST_PLAN.md` |
| 4 | **Traces** | 2,275 | ~1,500 | ~15 | ~15% | `04_TRACES_TEST_PLAN.md` |
| 5 | **Subscriptions** | ~400 | ~300 | ~8 | ~40% | `05_SUBSCRIPTIONS_TEST_PLAN.md` |
| 6 | **Profiles** | ~500 | ~400 | ~15 | ~50% | `06_PROFILES_TEST_PLAN.md` |
| 7 | **PaymentMethods** | ~200 | ~200 | 0 | 0% | `07_PAYMENTMETHODS_TEST_PLAN.md` |
| 8 | **Plans** | ~300 | ~100 | ~8 | ~60% | `08_PLANS_TEST_PLAN.md` |
| 9 | **Analysis** | ~200 | ~150 | 0 | 0% | `09_ANALYSIS_TEST_PLAN.md` |
| 10 | **Usage** | ~150 | ~200 | 0 | 0% | `10_USAGE_TEST_PLAN.md` |

---

## Estructura de Cada Plan de Módulo

Cada plan de módulo seguirá esta estructura:

```
1. ANÁLISIS DEL MÓDULO
   - Archivos de dominio (entidades, VOs, enums, eventos)
   - Archivos de aplicación (commands, queries, handlers, DTOs)
   - Archivos de API (endpoints)
   - Archivos de infraestructura (repositorios, servicios)

2. TESTS EXISTENTES
   - Lista de tests actuales
   - Cobertura estimada actual

3. TESTS UNITARIOS DE DOMINIO (Objetivo: 95%+ cobertura)
   - Tests por cada entidad
   - Tests por cada value object
   - Tests por cada enumeración
   - Tests de eventos de dominio
   - Casos de borde y edge cases

4. TESTS DE HANDLERS (Objetivo: 90%+ cobertura)
   - Tests por cada command handler
   - Tests por cada query handler
   - Casos de éxito, error, validación, permisos

5. TESTS DE INTEGRACIÓN API (Objetivo: 85%+ cobertura)
   - Tests por cada endpoint
   - Autenticación/autorización
   - Validación de requests/responses
   - Códigos HTTP correctos

6. TESTS E2E (Flujos críticos)
   - Flujos completos de negocio
   - Escenarios multi-step

7. MATRIZ DE CASOS DE PRUEBA
   - Tabla detallada con cada caso
   - Prioridad (P0/P1/P2)
   - Categoría (Unit/Integration/E2E)
```

---

## Orden de Implementación Recomendado

### Fase 1: Módulos Críticos sin Cobertura (Semanas 1-3)
1. **Pipelines** - 0% cobertura, funcionalidad crítica
2. **PaymentMethods** - 0% cobertura, impacta billing

### Fase 2: Módulos con Baja Cobertura (Semanas 4-6)
3. **Traces** - 15% cobertura, core del negocio
4. **Identity** - 25% cobertura, seguridad

### Fase 3: Módulos con Cobertura Media (Semanas 7-8)
5. **Studies** - 30% cobertura
6. **Subscriptions** - 40% cobertura

### Fase 4: Módulos con Mejor Cobertura (Semanas 9-10)
7. **Profiles** - 50% cobertura
8. **Plans** - 60% cobertura
9. **Analysis** - 0% cobertura (pequeño)
10. **Usage** - 0% cobertura (pequeño)

---

## Convenciones de Nomenclatura

### Archivos de Test
```
Tests/
├── Domain/
│   └── {Module}/
│       ├── {Entity}Tests.cs
│       ├── {EntityId}Tests.cs
│       ├── ValueObjects/
│       │   └── {ValueObject}Tests.cs
│       └── Enumerations/
│           └── {Enum}Tests.cs
├── Application/
│   └── {Module}/
│       ├── Commands/
│       │   └── {Command}HandlerTests.cs
│       └── Queries/
│           └── {Query}HandlerTests.cs
├── API/
│   └── {Module}/
│       └── {Endpoint}Tests.cs
└── E2E/
    └── {Flow}Tests.cs
```

### Nombres de Tests
```csharp
// Patrón: {Método}_{Escenario}_{ResultadoEsperado}
[Fact]
public void Create_WithValidData_ShouldReturnSuccess()

[Fact]
public void Create_WithEmptyName_ShouldReturnValidationError()

[Fact]
public void AddStep_WhenMaxStepsReached_ShouldReturnMaxStepsExceededError()
```

---

## Herramientas y Dependencias

```xml
<!-- Ya agregados a .csproj -->
<PackageReference Include="Bogus" Version="35.4.0" />
<PackageReference Include="Respawn" Version="6.2.1" />
<PackageReference Include="Testcontainers" Version="3.7.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.7.0" />
<PackageReference Include="Testcontainers.Redis" Version="3.7.0" />
<PackageReference Include="Testcontainers.MsSql" Version="3.7.0" />
```

---

## Métricas de Éxito

| Métrica | Objetivo |
|---------|----------|
| Cobertura de línea | ≥85% |
| Cobertura de rama | ≥85% |
| Tests unitarios | <5s total |
| Tests integración | <2min total |
| Tests E2E | <5min total |
| Todos los tests | <8min total |

---

## Comandos de Verificación

```bash
# Ejecutar todos los tests
dotnet test

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Generar reporte
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport

# Solo unitarios
dotnet test --filter "Category!=Integration&Category!=E2E"

# Solo integración
dotnet test --filter "Category=Integration"

# Solo E2E
dotnet test --filter "Category=E2E"

# Por módulo
dotnet test --filter "FullyQualifiedName~Pipelines"
```

---

## Configuración de Cobertura

### Archivo .runsettings

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura,opencover</Format>
          <Exclude>
            [*]*.Migrations.*,
            [*]*.Program,
            [*]*.Startup
          </Exclude>
          <ExcludeByFile>
            **/Migrations/**/*.cs,
            **/obj/**/*.cs
          </ExcludeByFile>
          <ExcludeByAttribute>
            Obsolete,
            GeneratedCodeAttribute,
            CompilerGeneratedAttribute,
            ExcludeFromCodeCoverageAttribute
          </ExcludeByAttribute>
          <SingleHit>false</SingleHit>
          <UseSourceLink>true</UseSourceLink>
          <IncludeTestAssembly>false</IncludeTestAssembly>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### GitHub Actions Workflow

```yaml
name: Tests & Coverage

on:
  push:
    branches: [master, develop]
  pull_request:
    branches: [master]

jobs:
  test:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_PASSWORD: test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
      redis:
        image: redis:7-alpine
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test with Coverage
        run: |
          dotnet test --no-build \
            --collect:"XPlat Code Coverage" \
            --settings:.runsettings \
            --results-directory:./coverage

      - name: Generate Report
        uses: danielpalme/ReportGenerator-GitHub-Action@5
        with:
          reports: 'coverage/**/coverage.cobertura.xml'
          targetdir: 'coveragereport'
          reporttypes: 'HtmlInline;Cobertura;MarkdownSummaryGithub'

      - name: Check Coverage Thresholds
        run: |
          LINE_COV=$(grep -oP 'line-rate="\K[0-9.]+' coverage/**/coverage.cobertura.xml | head -1)
          BRANCH_COV=$(grep -oP 'branch-rate="\K[0-9.]+' coverage/**/coverage.cobertura.xml | head -1)

          LINE_PCT=$(echo "$LINE_COV * 100" | bc)
          BRANCH_PCT=$(echo "$BRANCH_COV * 100" | bc)

          echo "Line Coverage: $LINE_PCT%"
          echo "Branch Coverage: $BRANCH_PCT%"

          # Fail if below thresholds
          if (( $(echo "$LINE_PCT < 80" | bc -l) )); then
            echo "::error::Line coverage $LINE_PCT% is below 80% threshold"
            exit 1
          fi

          if (( $(echo "$BRANCH_PCT < 75" | bc -l) )); then
            echo "::error::Branch coverage $BRANCH_PCT% is below 75% threshold"
            exit 1
          fi

          # Warn if below target
          if (( $(echo "$LINE_PCT < 85" | bc -l) )); then
            echo "::warning::Line coverage $LINE_PCT% is below 85% target"
          fi

          if (( $(echo "$BRANCH_PCT < 80" | bc -l) )); then
            echo "::warning::Branch coverage $BRANCH_PCT% is below 80% target"
          fi

      - name: Upload Coverage Report
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coveragereport/

      - name: Add Coverage PR Comment
        uses: marocchino/sticky-pull-request-comment@v2
        if: github.event_name == 'pull_request'
        with:
          recreate: true
          path: coveragereport/SummaryGithub.md
```

### Thresholds por Fase de Proyecto

| Fase | Line | Branch | Acción si falla |
|------|------|--------|-----------------|
| **Desarrollo** | 70% | 65% | Warning |
| **PR a develop** | 80% | 75% | Block merge |
| **PR a master** | 85% | 80% | Block merge |
| **Release** | 85% | 80% | Block release |

---

## RESUMEN CONSOLIDADO DE TESTS

### Por Módulo

| Módulo | Domain | Handlers | API | E2E | **Total** |
|--------|--------|----------|-----|-----|-----------|
| **Pipelines** | 161 | 106 | 30 | 8 | **305** |
| **Identity** | 159 | 129 | 45 | 12 | **345** |
| **Studies** | 156 | 157 | 54 | 10 | **377** |
| **Traces** | 164 | 177 | 50 | 12 | **403** |
| **Subscriptions** | 48 | 48 | 12 | 6 | **114** |
| **Profiles** | 73 | 54 | 12 | 0 | **139** |
| **PaymentMethods** | 17 | 30 | 10 | 0 | **57** |
| **Plans** | 38 | 12 | 6 | 0 | **56** |
| **Analysis** | 0 | 22 | 8 | 0 | **30** |
| **Usage** | 0 | 15 | 8 | 0 | **23** |
| **E2E Global** | - | - | - | 14 | **14** |
| **Infrastructure** | - | - | - | - | **108** |
| **TOTAL** | **816** | **750** | **235** | **62** | **~1,971** |

### Por Categoría

| Categoría | Existentes | Nuevos | Total |
|-----------|------------|--------|-------|
| Tests Unitarios Domain | ~120 | ~696 | **~816** |
| Tests de Handlers | ~100 | ~650 | **~750** |
| Tests de Integración API | ~10 | ~225 | **~235** |
| Tests E2E | 0 | ~62 | **~62** |
| Tests Infrastructure | 0 | ~108 | **~108** |
| **TOTAL** | **~230** | **~1,741** | **~1,971** |

### Cobertura Proyectada

| Módulo | Actual | Proyectada |
|--------|--------|------------|
| Pipelines | 0% | **92%** |
| Identity | 25% | **88%** |
| Studies | 30% | **90%** |
| Traces | 15% | **87%** |
| Subscriptions | 40% | **89%** |
| Profiles | 50% | **86%** |
| PaymentMethods | 0% | **85%** |
| Plans | 60% | **88%** |
| Analysis | 0% | **85%** |
| Usage | 0% | **85%** |
| **PROMEDIO** | **~22%** | **~88%** |

---

## CRONOGRAMA DE IMPLEMENTACIÓN

### Fase 1: Infraestructura Base (Semana 1)
- [x] Actualizar csproj con dependencias
- [x] Crear fixtures (PostgreSQL, Redis)
- [x] Crear base classes (HandlerTestBase, IntegrationTestBase)
- [ ] Crear builders y fakers

### Fase 2: Pipelines (Semanas 2-4)
- [ ] Domain tests (161 tests)
- [ ] Handler tests (106 tests)
- [ ] API tests (30 tests)
- [ ] E2E tests (8 tests)

### Fase 3: Identity (Semanas 5-6)
- [ ] Domain tests adicionales (119 tests)
- [ ] Handler tests adicionales (94 tests)
- [ ] API tests (37 tests)
- [ ] E2E tests (12 tests)

### Fase 4: Studies (Semanas 7-8)
- [ ] Domain tests adicionales (43 tests)
- [ ] Handler tests (122 tests)
- [ ] API tests (54 tests)
- [ ] E2E tests (10 tests)

### Fase 5: Traces (Semanas 9-11)
- [ ] Domain tests (140 tests)
- [ ] Handler tests (151 tests)
- [ ] API tests (50 tests)
- [ ] E2E tests (12 tests)

### Fase 6: Módulos Secundarios (Semanas 12-13)
- [ ] Subscriptions (79 tests nuevos)
- [ ] Profiles (72 tests nuevos)
- [ ] PaymentMethods (57 tests)
- [ ] Plans (31 tests)
- [ ] Analysis & Usage (53 tests)

### Fase 7: Infrastructure & E2E Global (Semana 14)
- [ ] Infrastructure services (108 tests)
- [ ] E2E global (14 tests)

---

## ARCHIVOS CREADOS

### Planes de Test
```
docs/TEST_PLANS/
├── 00_MASTER_TEST_PLAN.md           # Este archivo
├── 01_PIPELINES_TEST_PLAN.md        # Plan detallado Pipelines
├── 02_IDENTITY_TEST_PLAN.md         # Plan detallado Identity
├── 03_STUDIES_TEST_PLAN.md          # Plan detallado Studies
├── 04_TRACES_TEST_PLAN.md           # Plan detallado Traces
├── 05_SUBSCRIPTIONS_TEST_PLAN.md    # Plan detallado Subscriptions
├── 06_PROFILES_TEST_PLAN.md         # Plan detallado Profiles
├── 07_PAYMENTMETHODS_TEST_PLAN.md   # Plan detallado PaymentMethods
├── 08_PLANS_TEST_PLAN.md            # Plan detallado Plans
├── 09_ANALYSIS_USAGE_TEST_PLAN.md   # Plan detallado Analysis & Usage
└── 10_E2E_INFRASTRUCTURE_TEST_PLAN.md # Plan E2E e Infrastructure
```

### Infraestructura de Tests Creada
```
GeneFlow.ApiNet2.Tests/
├── Common/
│   ├── Base/
│   │   ├── EndpointTestBase.cs
│   │   ├── HandlerTestBase.cs
│   │   └── IntegrationTestBase.cs
│   ├── Builders/
│   │   └── UserBuilder.cs
│   └── Fixtures/
│       ├── IntegrationTestCollection.cs
│       ├── PostgreSqlContainerFixture.cs
│       └── RedisContainerFixture.cs
└── GeneFlow.ApiNet2.Tests.csproj     # Actualizado con dependencias
```

---

## PRÓXIMOS PASOS

1. **Completar Builders**: Crear StudyBuilder, TraceBuilder, PipelineBuilder
2. **Crear Fakers**: UserFaker, StudyFaker, TraceFaker con Bogus
3. **Implementar Pipelines Tests**: Comenzar con domain tests
4. **Configurar CI/CD**: Agregar jobs de cobertura en pipeline
