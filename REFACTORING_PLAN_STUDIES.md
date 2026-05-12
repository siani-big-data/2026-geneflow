# Plan de Refactorización — Módulo Studies

> Reglas obligatorias: **una clase por archivo**, **sin comentarios que no sean XML docs**, **sin sobreingeniería**.
> Cada cambio debe justificar el problema concreto que resuelve. No se introducen patrones por moda.

---

## 0. Supuestos y alcance

- El módulo Studies cubre el aggregate principal `Study` (con owned entities `StudyMember`, `StudyPaper`, `StudyStar`, `StudyView`), el aggregate separado `StudyInvitation`, sus repositorios, handlers, queries, endpoints y tests.
- El plan se ejecuta sobre el código actual: `Study.cs` (447 líneas), `StudyRepository.cs` (~500 líneas, 46 métodos públicos), `IStudyRepository` (27 firmas), 22 commands, 12 queries, 5 archivos de endpoints (~1.250 líneas), 14 eventos de dominio.
- Las dependencias transversales (`ICurrentUserService`, `IFileStorageService`, `IDateTimeProvider`, `Result`, `FullAuditableAggregateRoot`) ya existen y son confiables.
- El módulo cumple actualmente las dos reglas del proyecto: 0 violaciones de "una clase por archivo", 0 comentarios `//` no-doc detectados. La refactorización debe **mantener** ese cumplimiento.

---

## 1. Diagnóstico

### 1.1 Estado general

El módulo Studies es **arquitectónicamente sólido**: DDD aplicado correctamente, decisión bien fundada de modelar `StudyInvitation` como aggregate separado (lifecycle propio, expiración, resend), owned entities donde corresponde, eventos de dominio para los flujos principales. Las reglas de proyecto se respetan.

Sin embargo, presenta **erosión por crecimiento**: el repositorio acumula 46 métodos públicos mezclando lectura, escritura, analítica y engagement; la cascada de borrado hacia invitations no existe; varios métodos del repositorio mezclan ADO.NET crudo con EF Core; el DTO de miembros incluye campos que pertenecen al bounded context de Identity sin un contrato explícito; los endpoints repiten 7 veces el mismo null-check de autenticación.

### 1.2 Inventario por capa

| Capa | Archivos | Líneas aprox. |
|------|----------|---------------|
| Domain (entidades + VOs + enums + eventos + errors + interfaces) | 32 | 2.050 |
| Application (commands, queries, handlers, DTOs, mappings, events) | 74 | 4.500 |
| Infrastructure (repos, UoW, configuraciones, migrations) | 11 | 1.800 |
| API (endpoints) | 5 | 1.250 |
| Tests (Domain + Application + API) | 43 | 10.000 |

### 1.3 Decisiones correctas que se conservan

- `StudyInvitation` como aggregate separado.
- `StudyMember`, `StudyPaper`, `StudyStar`, `StudyView` como owned/sub-entidades.
- Smart enumerations para `StudyRole`, `StudyStatus`, `ResearchField`, `InvitationStatus`.
- Result pattern uniforme en handlers.
- Soft delete via `FullAuditableAggregateRoot`.

---

## 2. Code smells (con file:line)

### 2.1 Críticos (P0)

| # | Smell | Ubicación | Problema concreto |
|---|-------|-----------|-------------------|
| C1 | **God Repository** | `Infrastructure/Studies/Persistence/Repositories/StudyRepository.cs` (1-500), `Domain/Studies/IStudyRepository.cs` | 46 métodos públicos mezclando 5 responsabilidades (CRUD, queries analíticas, permisos, engagement, conteos). Viola ISP descaradamente. |
| C2 | **Falta cascada Study.Delete → Invitations** | `Application/Studies/Commands/DeleteStudy/DeleteStudyCommandHandler.cs` (~46), `Domain/Studies/IStudyInvitationRepository.cs` | Soft-delete del Study deja invitations `Pending` huérfanas. Inconsistencia eventual sin garantía: el `StudyDeletedEvent` no tiene handler que las cancele. |
| C3 | **ADO.NET crudo mezclado con EF Core** | `StudyRepository.cs` líneas ~274-362 | 7 métodos (`CountByMemberAsync`, `CountMembersInUserStudiesAsync`, `GetStudyIdsByMemberAsync`, `GetMemberRoleAsync`, `CountByOwnerIdAsync`, `CountMembersAsync`, `GetOwnerIdAsync`) abren `DbConnection`, escriben SQL en string, parsean a mano. Resto del repo usa LINQ. |

### 2.2 Altos (P1)

| # | Smell | Ubicación | Problema concreto |
|---|-------|-----------|-------------------|
| H1 | **Queries complejas inline sin Specification** | `StudyRepository.cs` líneas 69-123 (`GetByMemberAsync`, 55 líneas) y 126-188 (`GetPublishedAsync`, 63 líneas) | 5+ filtros combinables (search, status, field, tags, sort). Cada nueva combinación = nuevo método o reescritura inline. |
| H2 | **DTO cross-context sin contrato explícito** | `Application/Studies/Dtos/StudyMemberDto.cs` líneas 6-18 | `UserName`, `UserEmail`, `UserAvatarUrl` provienen de Identity. No está claro en `StudyMappings` cómo se rellenan. Acoplamiento implícito entre bounded contexts. |
| H3 | **Null-check de autenticación repetido 7 veces** | `API/Endpoints/Studies/StudyEndpoints.cs` líneas ~205-209, 251-252, 278-279, 306-307, 329-330, 354, 374 | `if (currentUser.UserId is null) return Results.Unauthorized();` en casi cada endpoint. Mismo patrón en los otros 4 archivos de endpoints. |
| H4 | **Token de invitación generado en domain con `Random.Shared`** | `Domain/Studies/StudyInvitation.cs` líneas 125-129 | `Random.Shared.NextBytes()` invocado dentro del aggregate. No determinístico para tests, no reemplazable en producción si se quisiera otro algoritmo. |
| H5 | **Lógica de creación dispersa en handler** | `Application/Studies/Commands/CreateStudy/CreateStudyCommandHandler.cs` líneas 47-103 | Handler llama secuencialmente `Study.Create`, luego `UpdateInstitution`, `UpdatePrincipalInvestigator`, loop de `AddTag`. Si falla cualquiera, se devuelve un Study a medio-construir vía evento ya emitido. |

### 2.3 Medios (P2)

| # | Smell | Ubicación | Problema concreto |
|---|-------|-----------|-------------------|
| M1 | Magic numbers de permisos | `Domain/Studies/Enumerations/StudyRole.cs` líneas 10-13 | `Owner=100, Admin=80, Editor=50, Viewer=10` sin nombre constante. Comparaciones tipo `PermissionLevel >= Editor.PermissionLevel` sin contexto. |
| M2 | Aggregate borderline | `Domain/Studies/Study.cs` (447 líneas) | Acceptable hoy, pero las regiones Tags, Papers y Permissions tienen lógica que se beneficiaría de un VO/método unificado. |
| M3 | Lógica de tags duplicada | `Study.cs` líneas 298-361 | `AddTag`, `RemoveTag`, `SetTags` repiten `Trim().ToLowerInvariant()` + dedup + max-length. |
| M4 | Permisos sin método central | `Study.cs` líneas 107, 152, 170, 185, 239 | Mezcla de patrones: `CanUserEdit()`, `GetMember()?.Role.CanManageMembers`, comparaciones directas con `StudyRole.Owner`. |
| M5 | Logging excesivo en repo | `StudyRepository.cs` ~20 `LogDebug` calls | Cada método pequeño loguea. Útil en debug, ruido en producción. |
| M6 | Backing field `_tags` sin XML doc | `Study.cs` ~línea 43 y `StudyConfiguration.cs` ~líneas 127-135 | Workaround documentado solo en commit message. Próximo desarrollador no entiende por qué. |
| M7 | Increment de métricas en Study, no en VO | `Study.cs` `IncrementViews/Stars/DecrementStars` | `StudyMetrics` ya existe como VO; los increments podrían vivir ahí (cohesión). |

### 2.4 Bajos (P3)

| # | Smell | Ubicación | Problema concreto |
|---|-------|-----------|-------------------|
| L1 | Validación duplicada role en SendInvitation | `SendInvitationCommandHandler.cs` ~49 vs `StudyInvitation.Create` | Handler comprueba `role == Owner`; el aggregate también. DRY menor. |
| L2 | UserId convertido a string ad-hoc | múltiples handlers | A veces `UserId`, a veces `userId.ToString()`. Inconsistencia leve. |
| L3 | SQL hardcoded sin constantes | `StudyRepository.cs` 7 bloques SQL | Si se mantiene ADO.NET, al menos extraer a constantes nombradas. |

---

## 3. Problemas arquitectónicos

### 3.1 Violación ISP en `IStudyRepository`

27 métodos en una sola interfaz mezclan: lectura básica, búsqueda paginada, conteos analíticos, autorización (`GetMemberRole`, `IsPublicStudy`, `GetOwnerId`), engagement (`AddStar`, `AddView`, `IsStarred`, `HasRecentView*`). Un handler de queries simples carga la superficie completa.

### 3.2 Inconsistencia eventual entre Study y StudyInvitation

Al ser aggregates separados, la consistencia entre ellos depende de eventos. Hoy:
- `StudyDeletedEvent` se emite, pero ningún handler cancela `Pending` invitations.
- `StudyInvitationRepository` no tiene método `CancelByStudyIdAsync`.

Resultado: invitaciones aceptables sobre estudios borrados.

### 3.3 Mezcla de paradigmas de acceso a datos

7 métodos del repo abren `DbConnection`, escriben SQL en string crudo y leen `IDataReader`. Resto usa LINQ. El paradigma debe ser uno solo (LINQ + `FromSql` para casos puntuales). La mezcla rompe homogeneidad y dificulta mocking en tests.

### 3.4 Cross-context implícito en `StudyMemberDto`

El DTO incluye `UserName`, `UserEmail`, `UserAvatarUrl` que vienen del módulo Identity/Profile. No hay contrato (`IUserProfileLookup`) que lo formalice. Riesgo: accidentalmente se traen vía join EF a tablas de otro bounded context, atando el modelo.

### 3.5 Eventos de dominio incompletos

Existen 12 eventos. Faltan al menos:
- `StudyMemberInvitedEvent` (trigger de email).
- `StudyInvitationAcceptedEvent` (notificaciones).
- `StudySettingsChangedEvent`.

No es urgente, pero limita extensión por handlers.

### 3.6 Token de invitación no reemplazable

`StudyInvitation.GenerateToken()` usa `Random.Shared.NextBytes` directamente. No se puede:
- Hacer determinístico en tests.
- Sustituir por un esquema más fuerte (HMAC, tokens firmados) sin tocar el aggregate.

---

## 4. Plan de refactorización en 3 fases

### Fase 1 — Cambios seguros, sin breaking

**Objetivo**: pagar deuda evidente sin afectar consumidores.

1. **F1.1** — Extraer constantes de niveles de permiso en `StudyRole.cs`. Reemplazar `100/80/50/10` literales por `OwnerPermissionLevel`, `AdminPermissionLevel`, `EditorPermissionLevel`, `ViewerPermissionLevel` (privadas, const).
2. **F1.2** — Documentar con XML doc el workaround del backing field `_tags` en `Study.cs` y en `StudyConfiguration.cs` (regla 1 cumplida; explicación necesaria).
3. **F1.3** — Crear `IInvitationTokenGenerator` (Application) y `RandomInvitationTokenGenerator` (Infrastructure). Inyectar en `SendInvitationCommandHandler`; pasar token ya generado a `StudyInvitation.Create`.
4. **F1.4** — Añadir firma `CancelByStudyIdAsync(StudyId, CancellationToken)` en `IStudyInvitationRepository` e implementar en `StudyInvitationRepository`. Cancela todas las invitations `Pending` del estudio llamando al método de dominio `inv.Cancel()`.
5. **F1.5** — Crear handler `StudyDeletedInvitationCleanupHandler : IDomainEventHandler<StudyDeletedEvent>` que invoque `CancelByStudyIdAsync`.
6. **F1.6** — Eliminar logging redundante en repositorio: mantener `LogDebug` en operaciones write y queries paginadas; eliminar en lectores triviales (`IsPublicStudy`, `IsStarred`, `GetOwnerId`).
7. **F1.7** — Centralizar el null-check de auth en endpoints mediante un helper estático único `RequireUserId(ICurrentUserService) : Result<UserId>` en `API/Extensions` (reutilizable por todos los módulos). Sustituir las 7 ocurrencias.

### Fase 2 — Mejoras de diseño

**Objetivo**: separar responsabilidades, reducir complejidad de queries, formalizar cross-context.

8. **F2.1** — Split de `IStudyRepository` por ISP en 4 interfaces en `Domain/Studies/Repositories/`:
   - `IStudyReadRepository`: `GetByIdAsync`, `GetByIdWithMembersAsync`, `GetByMemberAsync`, `GetPublishedAsync`, `GetFeaturedAsync`, `IsPublicStudy`.
   - `IStudyWriteRepository`: `AddAsync`, `Update`, `Delete`.
   - `IStudyAnalyticsRepository`: `CountByMemberAsync`, `CountMembersAsync`, `CountByOwnerIdAsync`, `CountMembersInUserStudiesAsync`, `GetMemberRoleAsync`, `GetOwnerIdAsync`.
   - `IStudyEngagementRepository`: `AddStarAsync`, `RemoveStarAsync`, `IsStarredAsync`, `AddViewAsync`, `HasRecentViewByUserAsync`, `HasRecentViewByIpHashAsync`.
   - Mantener `IStudyRepository` como interfaz combinada (extends de las 4) marcada `[Obsolete("Inject specific repositories")]` para migración gradual.
9. **F2.2** — Convertir los 7 métodos de ADO.NET a LINQ o `FromSqlInterpolated` cuando el LINQ no exprese el agregado. Eliminar `DbConnection.GetDbConnection()` directo.
10. **F2.3** — Crear `StudyTagSet` como value object (`Domain/Studies/ValueObjects/StudyTagSet.cs`) con `Add/Remove/SetAll/Count`, encapsulando normalización (Trim, ToLowerInvariant, dedup) y límites (`MaxTags`, `MaxTagLength`). `Study` delega.
11. **F2.4** — Mover `IncrementViews/IncrementStars/DecrementStars` desde `Study` a métodos de `StudyMetrics` (VO inmutable que devuelve nuevo `StudyMetrics`). `Study` reasigna su instancia.
12. **F2.5** — Añadir método unificado `Study.RequirePermission(UserId, Func<StudyRole, bool>) : Result` que centraliza "el usuario es miembro y su rol cumple la condición". Sustituir patrones dispersos.
13. **F2.6** — Introducir `IUserProfileLookup` en `Application/Common` con un método batch `GetSummariesAsync(IEnumerable<UserId>) : Task<IReadOnlyDictionary<UserId, UserProfileSummary>>`. Implementación en Infrastructure. `GetStudyMembersQueryHandler` enriquece `StudyMemberDto` explícitamente. Removidos los joins implícitos.

### Fase 3 — Refactor arquitectónico (opcional/futuro)

**Objetivo**: prepararse para crecimiento real.

14. **F3.1** — Specification objects (`StudyByMemberSpecification`, `PublishedStudiesSpecification`, `FeaturedStudiesSpecification`) en `Infrastructure/Studies/Specifications/`. Sólo si se añade un quinto criterio de filtrado o un segundo consumidor de la query. **No introducir antes**: actualmente 3 lectores independientes no justifica el coste.
15. **F3.2** — Eventos faltantes (`StudyMemberInvitedEvent`, `StudyInvitationAcceptedEvent`, `StudySettingsChangedEvent`) sólo cuando aparezca el primer suscriptor real (notificaciones, audit log externo). **No emitir eventos sin consumidor.**
16. **F3.3** — Considerar `StudyEngagement` como aggregate separado (Stars + Views) sólo si las métricas requieren consistencia transaccional propia o particionado independiente. Hoy no.

---

## 5. Patrones de diseño aplicables (sólo donde aportan)

| Patrón | Aplicación | Justificación |
|--------|-----------|---------------|
| **Domain Event Handler** | `StudyDeletedInvitationCleanupHandler` | Único modo limpio de mantener consistencia eventual entre aggregates separados sin acoplar `DeleteStudyCommandHandler` al repositorio de invitations. |
| **ISP (Interface Segregation)** | Split `IStudyRepository` | Reduce superficie por consumidor; permite mocks más pequeños en tests; facilita reemplazar implementación de engagement sin tocar lectura. |
| **Value Object** | `StudyTagSet`, métodos en `StudyMetrics` | Encapsula invariantes (normalización, límites) y reduce la superficie pública del aggregate. |
| **Strategy (token generation)** | `IInvitationTokenGenerator` | Permite tests determinísticos y futuro cambio a tokens firmados sin tocar el aggregate. |
| **Specification** | Sólo Fase 3 | Hoy hay 3 queries paginadas con filtros propios; el coste del patrón supera el beneficio. Aplicar cuando aparezca la 4ª. |

**Patrones explícitamente descartados**: AutoMapper (mapeos manuales son cortos y claros), generic repository (oculta intención), Mediator pipelines extra (los actuales bastan), CQRS con bases separadas (over-engineering), Event Sourcing (no hay requisito de auditoría temporal completa), Outbox (introducirlo cuando exista bus externo).

---

## 6. Estrategia de testing

### 6.1 Estado actual

- Domain: ~85% cubierto, `StudyTests.cs` con 1.067 líneas exhaustivo.
- Application: ~75-80%. 14 commands + 9 queries con tests dedicados.
- API: ~60-70%. Falta cobertura de paths de auth fallida y permisos denegados.

### 6.2 Tests a añadir (orden por riesgo)

1. **`DeleteStudyCommandHandlerTests`**: caso "Study con 3 invitations Pending → tras delete las 3 quedan `Cancelled`" (cubre F1.4 + F1.5).
2. **`StudyDeletedInvitationCleanupHandlerTests`**: idempotencia, no fallo si no hay invitations.
3. **`SendInvitationCommandHandlerTests`** con `IInvitationTokenGenerator` mockeado: token determinístico (cubre F1.3).
4. **`StudyTagSetTests`**: normalización, dedup, límites (cubre F2.3).
5. **`StudyMetricsTests`**: increments/decrements (cubre F2.4).
6. **`StudyTests.RequirePermission_*`**: matriz Owner/Admin/Editor/Viewer × condiciones (cubre F2.5).
7. **API tests de auth**: cada endpoint con `currentUser.UserId == null` → `401`. Hoy no se testea.
8. **API tests de permisos**: Viewer intenta `ChangeMemberRole` → `403`.

### 6.3 Tests a NO modificar

`StudyTests.cs` actual cubre comportamiento; los cambios internos (StudyTagSet, métodos en StudyMetrics) deben dejar los tests pasando sin tocarlos. Si algún test rompe por refactor interno, es señal de que testea implementación, no comportamiento — corregir el test.

### 6.4 Mutation testing

Stryker.NET ya configurado. Tras Fase 1+2, ejecutar mutation testing sobre `Domain/Studies/` y `Application/Studies/Commands/Delete*` para validar que los tests detectan cambios reales. Objetivo: mutation score Domain ≥ 70%.

---

## 7. Orden de ejecución (con dependencias)

```
Fase 1 (paralelizables salvo F1.4→F1.5):
  F1.1 ── independiente
  F1.2 ── independiente
  F1.3 ── independiente  (refactor leve de SendInvitation handler)
  F1.4 ── independiente  → F1.5
  F1.6 ── independiente
  F1.7 ── independiente  (helper compartido; no es Studies-specific pero cabe aquí)

Fase 2 (depende de Fase 1 finalizada):
  F2.1 ── ISP split          → habilita F2.6
  F2.2 ── ADO.NET → LINQ     (independiente de F2.1, pero aprovecha repo splitting)
  F2.3 ── StudyTagSet        → modifica Study.cs
  F2.4 ── métodos en Metrics → modifica Study.cs (coordinar con F2.3)
  F2.5 ── RequirePermission  → modifica Study.cs (coordinar con F2.3, F2.4)
  F2.6 ── IUserProfileLookup → independiente

Fase 3 (sólo cuando aparezca el detonante):
  F3.1, F3.2, F3.3
```

Recomendación: cerrar Fase 1 completa antes de abrir Fase 2. F2.3+F2.4+F2.5 tocan `Study.cs` y conviene hacerlas en una sola pasada para evitar conflictos de merge.

---

## 8. Cambios NO recomendados (anti-overengineering)

- **No** convertir `StudyMember` en aggregate separado: hoy su lifecycle está casado con Study; separarlo introduce consistencia eventual sin beneficio.
- **No** introducir AutoMapper: los mappings actuales son lineales y legibles.
- **No** crear `BaseStudyEvent` ni jerarquías de eventos: añade indirección sin valor.
- **No** convertir `Study` en árbol de aggregates más finos antes de que las regiones superen 600 líneas.
- **No** introducir Specification antes de la 4ª query con filtros combinables.
- **No** emitir eventos sin consumidor real (`StudySettingsChangedEvent`, `StudyMemberInvitedEvent`) — añadir cuando exista handler.
- **No** reescribir tests existentes que pasan: refactor interno ⇒ tests verdes sin tocarse.
- **No** introducir Outbox/Inbox antes de tener un bus de eventos externo.
- **No** migrar a `OneOf<T, Error>` u otro pattern alternativo a `Result`: el actual es consistente en todo el proyecto.
- **No** parametrizar el max de tags/papers vía configuración antes de tener un caso de uso real que lo requiera.

---

## 9. Métricas de calidad objetivo

| Métrica | Antes | Después de Fase 1+2 |
|---------|-------|---------------------|
| `Study.cs` líneas | 447 | ≤ 350 |
| `StudyRepository.cs` métodos públicos por interfaz | 27 (en una) | ≤ 8 por interfaz tras split |
| Métodos con ADO.NET crudo en repo | 7 | 0 |
| Null-checks de auth duplicados en endpoints | 7 (Studies) + N en otros | 0 (helper único) |
| Eventos `StudyDeletedEvent` con handler | 0 | 1 (cleanup invitations) |
| Comentarios `//` no-doc | 0 | 0 (mantener) |
| Archivos con > 1 clase pública | 0 | 0 (mantener) |
| Cobertura de tests Application/Studies | ~75-80% | ≥ 85% |
| Cobertura de tests API/Studies (auth + permisos) | ~60-70% | ≥ 80% |
| Mutation score Domain/Studies (Stryker) | sin medir | ≥ 70% |
| Magic numbers de permission level | 4 | 0 (constantes nombradas) |

---

## 10. Resultado esperado

Al completar Fase 1+2:

- **Consistencia**: borrar un Study cancela automáticamente sus invitations pendientes vía evento.
- **Mantenibilidad**: cuatro repositorios segregados por responsabilidad, cada handler inyecta sólo lo que usa.
- **Homogeneidad**: el repo accede a datos exclusivamente por LINQ (con `FromSqlInterpolated` puntual si fuera necesario).
- **Determinismo en tests**: el token de invitación es inyectable; el repo de engagement se mockea sin arrastrar el de escritura.
- **Reducción de código**: `Study.cs` baja a ~330 líneas; `StudyTagSet` y métodos de `StudyMetrics` absorben lógica antes dispersa.
- **Cross-context explícito**: `StudyMemberDto` se enriquece vía `IUserProfileLookup`; ningún join EF cruza bounded contexts.
- **Endpoints más planos**: el null-check de auth desaparece de los 5 archivos vía helper único.
- **Reglas de proyecto preservadas**: 0 violaciones de "una clase por archivo", 0 comentarios no-doc.

Lo que **no cambia** y debe permanecer:
- La decisión de modelar `StudyInvitation` como aggregate separado.
- Los 14 eventos de dominio actuales y sus consumidores.
- El uso de Result pattern.
- La estructura de tests existentes que pasan tras el refactor.

---

## 11. Próximo paso sugerido

Comenzar por **F1.4 + F1.5** (cascada de borrado + handler): es el único cambio P0 con impacto directo en consistencia funcional. El resto de Fase 1 es paralelizable.
