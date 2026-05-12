# Plan de Refactorización — Módulo Profiles

> Análisis exhaustivo y plan ejecutable para `GeneFlow.ApiNet2.{Domain,Application,Infrastructure,API}/Profiles` y sus tests.
>
> **Reglas obligatorias del proyecto** (aplicadas en todo el plan):
> 1. Una clase pública por archivo. Records/DTOs anidados están prohibidos.
> 2. Solo se permiten comentarios XML doc (`/// <summary>`). Cualquier otro comentario (`//`, `/* */`) se considera smell y debe eliminarse.
> 3. Sin sobreingeniería. Cada cambio justifica el problema concreto que resuelve.

---

## 1. Diagnóstico general del backend (módulo Profiles)

### 1.1 Estado actual

Profiles es un módulo **maduro y compacto**. Aplica Clean Architecture correctamente, CQRS con MediatR, Result Pattern, DDD con `Profile` como Aggregate Root rico (no anémico) acompañado de 7 Value Objects bien encapsulados (`PersonName`, `Bio`, `Location`, `ProfessionalRole`, `Institution`, `ProfilePhoto`, `ResearchIdentifiers`), 1 Smart Enumeration (`ResearchField` con 18 valores), 5 eventos de dominio y auditoría completa vía `FullAuditableAggregateRoot<ProfileId>`. La configuración EF mapea VOs como owned types con conversiones tipadas, soft delete y un índice único `user_id`. Tests existen para Domain (8 archivos) y Application (9 archivos de handlers).

### 1.2 Fortalezas reales

- Aggregate `Profile` (151 líneas) bien dimensionado: 5 métodos públicos cohesivos (`Create`, `UpdateBasicInfo`, `UpdateResearchIdentifiers`, `UpdatePhoto`, `RemovePhoto`), invariantes claros, eventos focalizados.
- Value Objects bien encapsulados con regex compilados (source-generated) para `PersonName`, `ResearchIdentifiers` (ORCID `^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$`), `ProfilePhoto` (URLs http/https o ruta relativa).
- Smart Enumeration `ResearchField` con `FromName` / `FromId` / `.Id` / `.Name` siguiendo la convención del proyecto.
- Handlers de comandos delgados en general (`UpdateProfile`, `UpdateProfilePhoto`, `UpdateResearchIdentifiers` ≤ 94 líneas) que orquestan VO + agregado + persistencia.
- `ProfileUnitOfWork` (55 líneas) implementa correctamente el patrón "commit → dispatch eventos → clear events".
- Configuración EF Core completa: owned types, conversiones, índice único `user_id`, índice por `ResearchField`, soft delete con query filter.
- Eventos de dominio bien diseñados (`ProfileCreatedEvent`, `ProfileUpdatedEvent`, `ProfilePhotoUploadedEvent`, `ProfilePhotoUpdatedEvent`, `ProfilePhotoDeletedEvent`).
- Endpoints autenticados con `RequireAuthorization()` y validación adicional en handlers.
- Pipeline de foto correcto: upload → command crea VO → evento → `ProfilePhotoUploadedEventHandler` almacena vía `IFileStorageService`, genera thumbnail 150x150 con `IImageProcessingService` y publica a EventBus.

### 1.3 Problemas principales

- **Mezcla ADO.NET + EF Core en `ProfileRepository`** (167 líneas). `GetByUserIdAsync` y `GetByUserIdsAsync` ejecutan SQL crudo para resolver `ProfileId` y luego usan EF Core para cargar el agregado. Justificación documentada: "performance con value object conversion" — sin métricas que la respalden.
- **Hardcoded path en Application**: `UploadProfilePhotoCommandHandler.cs:99-101` construye literalmente `/storage/profiles/{id}/photo.{ext}` y `/storage/profiles/{id}/thumbnail.{ext}`. Application no debe conocer rutas físicas.
- **`UploadProfilePhotoCommandHandler` 138 líneas**: con constantes hardcodeadas (`MaxPhotoSize = 10*1024*1024`), whitelist de content types (`AllowedContentTypes`), diccionario de extensiones (`ContentTypeToExtension`) y validación duplicada respecto a `ProfilePhoto` VO.
- **Validación `currentUser.UserId is null` repetida 7 veces** en `ProfileEndpoints.cs:130, 175, 193, 221, 243, 265, 283`.
- **`GetProfileStatsQueryHandler` (83 líneas)** orquesta tres repositorios (`IProfileRepository`, `IStudyRepository`, `ITraceRepository`) directamente desde un query handler.
- **TODO comments en producción**: `GetProfileStatsQueryHandler.cs:77-78` (`// TODO: Add when alignments module is ready`).
- **`ProfileEndpoints.cs` con 337 líneas y 10 endpoints en un único archivo**: aunque cumple la regla "una clase por archivo", es el límite alto.
- **`ProfileErrors.cs` (154 líneas)** clase estática con todos los errores del módulo mezclados (Profile, PersonName, Photo, ResearchField, Bio/Location/Role/Institution).
- **Comentarios `//` no-doc dispersos** en handlers, repositorio y endpoints (regla obligatoria).
- **`CreateProfileCommandHandler` (122 líneas)** llama a `Create(...)` con sólo `PersonName` y luego a `UpdateBasicInfo(...)` inmediatamente. Esa segunda llamada provoca un `SetModified` en una entidad recién creada y un `ProfileUpdatedEvent` redundante con `ProfileCreatedEvent`.
- **Magic numbers**: `10*1024*1024` duplicado en `UploadProfilePhotoCommandHandler` y en `ProfilePhoto.MaxSizeBytes`; thumbnail `150x150` hardcodeado en el event handler.

### 1.4 Riesgos técnicos

| Riesgo | Probabilidad | Impacto |
|---|---|---|
| Bug latente al unificar ORM en `ProfileRepository` por SQL crudo y diferencias en filtrado de `IsDeleted` | Media | Medio |
| Cambio de prefix `/storage/profiles/` rompe URLs públicas ya emitidas a clientes | Baja | Alto |
| Modificar `CreateProfileCommandHandler` para no disparar `ProfileUpdatedEvent` cambia el contrato de eventos consumidos por otros handlers | Baja | Medio |
| Acoplamiento `GetProfileStatsQueryHandler → IStudyRepository / ITraceRepository` puede ocultar dependencias en build cuando esos módulos cambien interfaces | Media | Bajo |
| Validación duplicada (size/MIME) en handler y VO puede divergir y producir mensajes inconsistentes | Media | Bajo |
| El `ProfilePhotoUploadedEvent` se publica sin Outbox: si el `EventHandler` falla, foto persistida en BD pero no en storage | Baja | Alto |
| TODO de Alignments puede convertirse en deuda crónica si nunca llega el módulo | Alta | Bajo |

---

## 2. Code smells detectados

### 2.1 Smells altos (severidad alta)

#### S-01 · Mezcla ADO.NET + EF Core en `ProfileRepository`
- **Ubicación**: `Infrastructure/Profiles/Persistence/Repositories/ProfileRepository.cs:56-69` (`GetByUserIdAsync`), `83-105` (`GetByUserIdsAsync`), `107-127` (`GetProfileIdByUserIdAsync` privado), `129-166` (`GetProfileIdsByUserIdsAsync` privado).
- **Problema**: dos modelos de acceso a datos coexisten. ADO.NET resuelve `ProfileId` desde `user_id`; EF Core hace el fetch del agregado.
- **Por qué es problema**: justificación de performance no respaldada con benchmarks; duplica el filtro `is_deleted = false` (uno en SQL crudo, otro en query filter EF); riesgo de divergencia si cambia el schema.
- **Principios**: SoC, KISS, Persistence Ignorance.
- **Recomendación**: unificar en EF Core puro con `AsNoTracking()` para reads. Si EF realmente no soporta el caso (poco probable: `_context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId)` debe funcionar perfectamente con la conversión de `UserId` ya configurada), documentar el caso con benchmark adjunto.

#### S-02 · `UploadProfilePhotoCommandHandler` con responsabilidades mezcladas y URL hardcodeada
- **Ubicación**: `Application/Profiles/Commands/UploadProfilePhoto/UploadProfilePhotoCommandHandler.cs` (138 líneas).
- **Problema**:
  - Líneas 26-43: constantes locales (`MaxPhotoSize`, `AllowedContentTypes`, `ContentTypeToExtension`).
  - Líneas 65-81: validación de content-type, size y base64 (duplica `ProfilePhoto` VO).
  - Líneas 99-101: construye URL hardcodeada `/storage/profiles/{profileId}/photo.{ext}` y `/storage/profiles/{profileId}/thumbnail.{ext}` desde Application.
  - Líneas 121-129: publica evento manualmente con `IPublisher` cuando ya `ProfileUnitOfWork` despacha eventos del agregado.
- **Principios**: SRP, SoC (Application no decide rutas físicas), DRY.
- **Recomendación**: introducir `IProfilePhotoUrlBuilder` (Application) o `IFileStorageService.GetPublicUrl(string relativeKey)` (Infrastructure abstraction); mover whitelist de MIME y constantes a `ProfilePhoto` VO o `PhotoSettings`; eliminar la doble validación.

### 2.2 Smells medios (severidad media)

#### S-03 · Validación `currentUser.UserId is null` duplicada 7 veces
- **Ubicación**: `API/Endpoints/Profiles/ProfileEndpoints.cs` líneas 130, 175, 193, 221, 243, 265, 283.
- **Problema**: el mismo bloque `if (currentUser.UserId is null) return Results.Unauthorized();` se repite en 7 endpoints `/me/...`.
- **Principio**: DRY.
- **Recomendación**: extraer a un helper privado `static IResult? RequireUserId(ICurrentUserService currentUser, out string userId)` o, mejor, introducir un endpoint filter `RequireAuthenticatedUser` reutilizable. Si ya `RequireAuthorization()` está aplicado, este chequeo solo ocurriría por bugs de configuración: simplificar a un único helper.

#### S-04 · `GetProfileStatsQueryHandler` orquesta 3 repositorios cross-domain
- **Ubicación**: `Application/Profiles/Queries/GetProfileStats/GetProfileStatsQueryHandler.cs` (83 líneas, líneas 18-20 inyecciones, 50-69 cálculos).
- **Problema**: el handler depende de `IProfileRepository`, `IStudyRepository`, `ITraceRepository` y compone 4 queries (count studies miembro, count studies owner, count traces, last activity).
- **Principios**: SRP, CQRS (un query handler debería leer de un read model coherente, no orquestar lecturas cross-aggregate).
- **Recomendación**: introducir `IProfileStatsService` (Application) que reciba `userId` y devuelva `ProfileStats`; o, si crece, una proyección/read model dedicada. Por ahora basta el servicio.

#### S-05 · `ProfileEndpoints.cs` con 337 líneas y 10 endpoints
- **Ubicación**: `API/Endpoints/Profiles/ProfileEndpoints.cs`.
- **Problema**: tamaño en el límite superior. Mezcla endpoints de "mi perfil" con endpoints de "perfiles públicos" y "batch".
- **Principio**: cohesión, tamaño de archivo.
- **Recomendación**: separar en `MyProfileEndpoints.cs` (los `/me/...`), `ProfileQueryEndpoints.cs` (`GET /{userId}`, `POST /batch`) registrados ambos en el mismo grupo `/profiles`. No es urgente, pero mejora navegación.

#### S-06 · Comentarios `//` no-doc dispersos
- **Ubicación**: dispersos en `UploadProfilePhotoCommandHandler`, `ProfileRepository`, `GetProfileStatsQueryHandler`, `ProfileEndpoints` y otros.
- **Principio**: regla obligatoria del proyecto.
- **Recomendación**: eliminar. Los que explican "por qué" se convierten en `/// <remarks>` del miembro o se descartan.

#### S-07 · Validación duplicada handler vs Value Object
- **Ubicación**: `UploadProfilePhotoCommandHandler.cs:65-81` valida MIME y size; `ProfilePhoto.cs` también valida tamaño en `Create`. Hay dos fuentes de verdad.
- **Principio**: DRY, Single Source of Truth.
- **Recomendación**: ProfilePhoto VO valida invariantes de dominio (URL válida, size ≤ máximo). El handler solo valida shape de la entrada (MIME contra whitelist, base64 parseable). Documentar la división.

### 2.3 Smells bajos (severidad baja)

#### S-08 · `ProfileErrors.cs` (154 líneas)
- **Ubicación**: `Domain/Profiles/ProfileErrors.cs`.
- **Problema**: clase estática con errores de Profile, PersonName, Photo, ResearchField, Bio/Location/Role/Institution. Difícil navegar.
- **Recomendación**: particionar en `Domain/Profiles/Errors/`:
  - `ProfileAccountErrors.cs` (NotFound, AlreadyExists).
  - `ProfileBasicInfoErrors.cs` (FirstName/LastName, Bio, Location, Role, Institution).
  - `ProfilePhotoErrors.cs` (PhotoUrl, Size, Format, InvalidPhotoData).
  - `ProfileResearchErrors.cs` (InvalidResearchField, ORCID, Website).

#### S-09 · `CreateProfileCommandHandler` invoca `Create` + `UpdateBasicInfo` redundantemente
- **Ubicación**: `Application/Profiles/Commands/CreateProfile/CreateProfileCommandHandler.cs:69-104` y `107-113`.
- **Problema**: tras `Profile.Create(profileId, userId, name)` se invoca inmediatamente `profile.UpdateBasicInfo(...)` y `profile.UpdateResearchIdentifiers(...)`. Esto provoca `SetModified` y emite `ProfileUpdatedEvent` durante una creación.
- **Principio**: claridad, mínimo asombro.
- **Recomendación**: enriquecer `Profile.Create(...)` para que reciba todos los VOs opcionales; `UpdateBasicInfo`/`UpdateResearchIdentifiers` quedan solo para mutaciones posteriores. Beneficio: un único evento `ProfileCreatedEvent`.

#### S-10 · Magic numbers
- **Ubicación 1**: `UploadProfilePhotoCommandHandler.cs:26` (`MaxPhotoSize = 10 * 1024 * 1024`) duplicado con `ProfilePhoto.MaxSizeBytes`.
- **Ubicación 2**: `ProfilePhotoUploadedEventHandler.cs` thumbnail `150 x 150` hardcodeado.
- **Recomendación**: mover a `PhotoSettings` (Infrastructure config) inyectado vía `IOptions<PhotoSettings>`: `MaxSizeBytes`, `ThumbnailWidth`, `ThumbnailHeight`, `AllowedMimeTypes`.

#### S-11 · TODO comments en producción
- **Ubicación**: `GetProfileStatsQueryHandler.cs:77-78`.
- **Problema**: literales `// TODO: Add when alignments module is ready`.
- **Recomendación**: o se introduce `IAlignmentStatsProvider` con implementación nula que devuelve ceros y se registra en DI (cierra el TODO con un contrato real), o se elimina la propiedad del DTO hasta que el módulo exista. Decidir; no dejar TODO.

#### S-12 · POST `/batch` para una lectura
- **Ubicación**: `ProfileEndpoints.cs:323-335`.
- **Problema**: `POST /api/v1/profiles/batch` es semánticamente una lectura.
- **Recomendación**: aceptable si el body lleva la lista de IDs (GET con body es problemático con varios clientes/proxies). Documentarlo en OpenAPI con `summary` aclarando "POST por necesidad de body". Bajo coste, alto beneficio en claridad. No cambiar verbo.

#### S-13 · `ProfilePhotoUploadedEvent` publicado manualmente fuera de UoW
- **Ubicación**: `UploadProfilePhotoCommandHandler.cs:121-129`.
- **Problema**: el handler invoca `_publisher.Publish(...)` después de `SaveChangesAsync`, mientras que el flujo estándar usa `ProfileUnitOfWork` que despacha automáticamente eventos del agregado.
- **Principio**: consistencia.
- **Recomendación**: si el agregado ya emite el evento al hacer `UpdatePhoto`, eliminar la publicación manual; si no, hacer que `Profile.UpdatePhoto` emita el evento y suprimir el `_publisher.Publish` del handler.

#### S-14 · Acoplamiento Application → Studies/Traces sin contrato explícito
- **Ubicación**: `GetProfileStatsQueryHandler` inyecta `IStudyRepository`, `ITraceRepository`.
- **Recomendación**: encapsular en `IProfileStatsService` con un contrato propio del módulo Profile que internamente delegue a esos repos. Mantiene el módulo Profile libre de dependencias directas a aggregates externos.

---

## 3. Problemas arquitectónicos

### 3.1 Construcción de URL pública en Application
`UploadProfilePhotoCommandHandler` decide la ruta de almacenamiento (`/storage/profiles/{id}/photo.{ext}`). Esa decisión pertenece a Infrastructure (storage backend la conoce). Application debería pedir "dame la URL pública para este recurso" a un servicio.

### 3.2 Repositorio con dos modelos de acceso a datos
Sin métricas que justifiquen el SQL crudo, conviven EF Core (filtro `is_deleted` vía query filter, conversiones de VO ya configuradas) con ADO.NET (otra forma de filtrar y leer columnas). Dos formas de mantener.

### 3.3 Query handler haciendo orquestación cross-aggregate
`GetProfileStatsQueryHandler` viola la idea de "un query handler lee un read model". Mientras `Studies`/`Traces` no expongan una proyección integrada, lo correcto es un servicio dedicado (`IProfileStatsService`) que aísle la composición.

### 3.4 Eventos de dominio no idempotentes ni con garantía de entrega
`ProfilePhotoUploadedEvent` desencadena trabajo en `IFileStorageService` y `IEventBusPublisher`. Si falla el handler de evento, la BD ya quedó comprometida con la URL nueva pero el archivo puede no estar en storage. No hay outbox, no hay reintento, no hay idempotencia explícita. Riesgo bajo a volumen actual; deuda conocida.

### 3.5 `Profile.Create` no acepta todos los VOs iniciales
Resultado: handler hace dos pasos (Create + UpdateBasicInfo + UpdateResearchIdentifiers) en una creación, generando dos eventos donde debería haber uno.

### 3.6 Endpoints públicos sin rate limiting visible
`GET /profiles/{userId}` y `POST /profiles/batch` no tienen `RequireAuthorization` (correcto si son públicos), pero tampoco rate limiting documentado. Posible vector de scraping. Anotar como deuda; no incluir en plan inicial.

---

## 4. Plan de refactorización priorizado

### Fase 1 — Cambios seguros y de bajo riesgo

Sin cambios de comportamiento, sin breaking changes. Cada tarea es un commit atómico con `dotnet test` y `dotnet build` (0 warnings) verdes.

#### Tarea 1.1 · Eliminar comentarios no-doc en módulo Profiles
- **Descripción**: regex `^\s*//(?!/)` en todos los `.cs` bajo `*/Profiles/`. Eliminar o convertir a `/// <remarks>`.
- **Archivos afectados**: estimado 12-18 archivos.
- **Beneficio**: cumple regla obligatoria.
- **Riesgo**: nulo.
- **Dependencias**: ninguna.
- **Criterio**: ningún `// ` (excluyendo `///`) en archivos del módulo Profiles.
- **Tests**: existentes intactos.

#### Tarea 1.2 · Particionar `ProfileErrors.cs` por subdominio
- **Descripción**: dividir en `Domain/Profiles/Errors/`:
  - `ProfileAccountErrors.cs` (NotFound, AlreadyExists).
  - `ProfileBasicInfoErrors.cs` (FirstName/LastName, Bio, Location, ProfessionalRole, Institution).
  - `ProfilePhotoErrors.cs` (PhotoUrlInvalidFormat, PhotoUrlTooLong, PhotoSizeExceedsLimit, InvalidPhotoFormat, InvalidPhotoData, PhotoTooLarge).
  - `ProfileResearchErrors.cs` (InvalidResearchField, ORCID, Website).
- **Archivos afectados**: `ProfileErrors.cs` (a eliminar tras migración), 4 archivos nuevos. Buscar y reemplazar referencias `ProfileErrors.X` por la clase específica.
- **Beneficio**: legibilidad, archivos < 60 líneas, navegación por subdominio.
- **Riesgo**: medio (alcance amplio en Application por referencias).
- **Dependencias**: ninguna.
- **Criterio**: `ProfileErrors.cs` eliminado; `dotnet build` y `dotnet test` verdes.
- **Tests**: validaciones por equality siguen funcionando sin cambios.

#### Tarea 1.3 · Magic numbers a constantes nombradas
- **Descripción**:
  - Eliminar `MaxPhotoSize = 10 * 1024 * 1024` de `UploadProfilePhotoCommandHandler` y usar `ProfilePhoto.MaxSizeBytes` (única fuente de verdad).
  - Crear `Infrastructure/Profiles/Configuration/PhotoSettings.cs`:
    - `MaxSizeBytes` (con default 10 MB).
    - `ThumbnailWidth` (default 150).
    - `ThumbnailHeight` (default 150).
    - `AllowedMimeTypes` (jpeg, png, gif, webp).
  - Inyectar `IOptions<PhotoSettings>` en `UploadProfilePhotoCommandHandler` y `ProfilePhotoUploadedEventHandler`.
- **Archivos afectados**: `UploadProfilePhotoCommandHandler.cs`, `ProfilePhotoUploadedEventHandler.cs`, `PhotoSettings.cs` (nuevo), `Program.cs` o `DependencyInjection.cs` para registro.
- **Beneficio**: configurabilidad sin recompilar; cero literales mágicos.
- **Riesgo**: bajo.
- **Dependencias**: ninguna.
- **Criterio**: `grep "10 \* 1024" ProfileMod*.cs` y `grep "150" ProfilePhotoUploaded*.cs` retornan 0 ocurrencias en Profiles.
- **Tests**: existentes intactos.

#### Tarea 1.4 · Cerrar el TODO de Alignments
- **Descripción**: dos opciones; elegir una:
  - **Opción A (recomendada)**: introducir `IAlignmentStatsProvider` en `Application/Profiles/Abstractions/` con método `Task<AlignmentStats> GetForUserAsync(UserId, CancellationToken)`. Implementación nula `NullAlignmentStatsProvider` registrada en DI hasta que exista módulo. Eliminar TODO.
  - **Opción B**: eliminar las propiedades `TotalAlignments`, `CompletedAlignments` del `ProfileStatsDto` y del `ProfileStatsResponse` hasta que el módulo Alignments exista.
- **Archivos afectados**: `GetProfileStatsQueryHandler.cs`, `ProfileStatsDto.cs`, opcional `IAlignmentStatsProvider.cs` y `NullAlignmentStatsProvider.cs`.
- **Beneficio**: cero TODOs en producción; contrato claro.
- **Riesgo**: bajo. Opción B es breaking si hay clientes consumiendo la propiedad.
- **Dependencias**: ninguna técnica.
- **Criterio**: ningún `TODO` en módulo Profiles.

#### Tarea 1.5 · Extraer helper `RequireUserId` en `ProfileEndpoints`
- **Descripción**: introducir un endpoint filter o helper estático que centraliza la verificación. Si `RequireAuthorization()` ya garantiza autenticación, simplificar a un único método privado:

```csharp
private static (string? UserId, IResult? Failure) ResolveUserId(ICurrentUserService currentUser)
    => currentUser.UserId is null
        ? (null, Results.Unauthorized())
        : (currentUser.UserId, null);
```

- **Archivos afectados**: `ProfileEndpoints.cs` (337 → ~290 líneas).
- **Beneficio**: DRY.
- **Riesgo**: nulo (refactor mecánico).
- **Dependencias**: ninguna.
- **Criterio**: `currentUser.UserId is null` aparece una sola vez en el archivo.
- **Tests**: existentes intactos.

#### Tarea 1.6 · Documentar el doble propósito de validación handler vs VO
- **Descripción**: añadir `/// <remarks>` en `UploadProfilePhotoCommandHandler` y `ProfilePhoto` indicando qué se valida en cada nivel:
  - Handler: shape de input (MIME contra whitelist, base64 parseable).
  - VO: invariantes de dominio (URL válida, size ≤ máximo).
- **Beneficio**: deja claro por qué hay dos validaciones (una para input externo, otra para invariante).
- **Riesgo**: nulo.
- **Criterio**: comentarios XML doc presentes en ambos archivos.

---

### Fase 2 — Mejoras de diseño

Cambios internos sin afectar contratos públicos. Requieren tests verdes antes y después.

#### Tarea 2.1 · Mover construcción de URL pública a Infrastructure
- **Descripción**: introducir `IProfilePhotoUrlBuilder` en `Application/Profiles/Abstractions/` con método `(string Url, string ThumbnailUrl) BuildUrls(ProfileId profileId, string extension)`. Implementación `ProfilePhotoUrlBuilder` en `Infrastructure/Profiles/Services/` que use `PhotoSettings.PublicBasePath` (configurable).
- **Archivos afectados**: `IProfilePhotoUrlBuilder.cs` (nuevo, Application); `ProfilePhotoUrlBuilder.cs` (nuevo, Infrastructure); `UploadProfilePhotoCommandHandler.cs` deja de hardcodear `/storage/profiles/...`; `PhotoSettings.cs` añade `PublicBasePath`.
- **Beneficio**: Application desconoce el layout físico; testeable; cambio de prefix sin tocar Application.
- **Riesgo**: bajo si el path generado es idéntico.
- **Dependencias**: 1.3 (PhotoSettings).
- **Criterio**: `grep "/storage/profiles" Application/` retorna 0 ocurrencias.
- **Tests**: tests del handler usan fake builder; añadir test del builder real.

#### Tarea 2.2 · Enriquecer `Profile.Create` para evitar el doble-update en creación
- **Descripción**: cambiar la firma de `Profile.Create(...)` para aceptar todos los VOs opcionales (Bio, Location, ProfessionalRole, Institution, ResearchField, ResearchIdentifiers). Constructor privado los inicializa en lugar de `Empty`. `CreateProfileCommandHandler` deja de llamar a `UpdateBasicInfo` y `UpdateResearchIdentifiers` durante la creación.
- **Archivos afectados**: `Profile.cs` (Create method), `CreateProfileCommandHandler.cs` (122 → ~75 líneas).
- **Beneficio**: una sola escritura, un solo evento (`ProfileCreatedEvent`), código más simple.
- **Riesgo**: medio. Si algún consumidor espera `ProfileUpdatedEvent` durante la creación, romperá. Verificar que ningún `INotificationHandler<ProfileUpdatedEvent>` actúe sobre el caso "creación".
- **Dependencias**: ninguna técnica.
- **Criterio**: `CreateProfileCommandHandler` < 80 líneas y dispara un único evento.
- **Tests**: ajustar `CreateProfileCommandHandlerTests`; añadir test que verifique solo `ProfileCreatedEvent`.

#### Tarea 2.3 · Introducir `IProfileStatsService`
- **Descripción**: nuevo servicio `Application/Profiles/Services/ProfileStatsService.cs` que encapsula la composición de stats. `GetProfileStatsQueryHandler` (83 → ~25 líneas) solo valida userId y delega.
- **Archivos afectados**: `IProfileStatsService.cs` (nuevo), `ProfileStatsService.cs` (nuevo), `GetProfileStatsQueryHandler.cs` (simplificado).
- **Beneficio**: SRP en handler; servicio testeable aislado.
- **Riesgo**: bajo.
- **Dependencias**: ninguna técnica.
- **Criterio**: `GetProfileStatsQueryHandler` < 30 líneas; dependencias del handler bajan de 3 repositorios a 1 servicio.
- **Tests**: nuevos tests de `ProfileStatsService`; tests del handler se simplifican.

#### Tarea 2.4 · Unificar ORM en `ProfileRepository` (EF Core puro)
- **Descripción**: eliminar `GetProfileIdByUserIdAsync` y `GetProfileIdsByUserIdsAsync` privados (ADO.NET). Reescribir:
  - `GetByUserIdAsync(UserId)` → `_context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId)`.
  - `GetByUserIdsAsync(IEnumerable<UserId>)` → `_context.Profiles.Where(p => userIds.Contains(p.UserId)).ToListAsync()`.
  - `ExistsForUserAsync(UserId)` → `_context.Profiles.AnyAsync(p => p.UserId == userId)`.
- **Archivos afectados**: `ProfileRepository.cs` (167 → ~110 líneas).
- **Beneficio**: un único modelo de acceso, mantenibilidad, no más SQL crudo, query filter `IsDeleted` aplicado consistentemente.
- **Riesgo**: medio. Validar con benchmark que la conversión `UserId → string` en EF traduce a SQL correcto. Si EF no traduce el filtro por VO, se acepta promover `UserId` a comparación por valor primitivo dentro del repo.
- **Dependencias**: tests de integración con BD real antes de tocar.
- **Criterio**: 0 referencias a `DbConnection` y `CreateCommand` en `ProfileRepository`.
- **Tests**: tests de integración (PostgreSQL real con `WebApplicationFactory`) verifican paridad de comportamiento.

#### Tarea 2.5 · Unificar publicación de eventos `ProfilePhotoUploaded`
- **Descripción**: eliminar la publicación manual del handler (`_publisher.Publish(...)`). Hacer que `Profile.UpdatePhoto` emita `ProfilePhotoUploadedEvent` cuando el cambio sea por upload (o renombrar el evento existente para cubrir ambos casos). El `ProfileUnitOfWork` ya despacha eventos del agregado, así que el evento se entrega tras commit.
- **Archivos afectados**: `Profile.cs`, `UploadProfilePhotoCommandHandler.cs`.
- **Beneficio**: consistencia; un único punto de publicación; evita doble-emisión accidental.
- **Riesgo**: bajo.
- **Dependencias**: ninguna.
- **Criterio**: `_publisher` desaparece de `UploadProfilePhotoCommandHandler`.
- **Tests**: actualizar test que verifique evento publicado vía dispatcher de UoW.

#### Tarea 2.6 · Separar `ProfileEndpoints.cs` en endpoints "yo" vs "otros"
- **Descripción**: dividir en `MyProfileEndpoints.cs` (los `/me/...`, 7 endpoints) y `ProfileQueryEndpoints.cs` (`GET /{userId}`, `POST /batch`, 2 endpoints), ambos registrados en el mismo grupo. `CreateProfile` puede vivir con los `/me/` o en un `ProfileLifecycleEndpoints.cs`.
- **Archivos afectados**: 1 archivo dividido en 2-3 archivos.
- **Beneficio**: archivos < 200 líneas; cohesión por audiencia.
- **Riesgo**: bajo.
- **Dependencias**: 1.5 (helper RequireUserId).
- **Criterio**: ningún archivo de endpoints > 200 líneas.

---

### Fase 3 — Refactorización arquitectónica

Cambios estructurales. **Solo abordar si Fase 1 y 2 están consolidadas** y métricas en producción justifican.

#### Tarea 3.1 · Outbox Pattern para `ProfilePhotoUploadedEvent` y `ProfilePhotoDeletedEvent`
- **Descripción**: tabla `outbox_messages`; los handlers de storage/eventbus se vuelven consumers del outbox. Persistencia atómica con commit del agregado.
- **Beneficio**: garantía de entrega para side-effects de almacenamiento físico.
- **Riesgo**: alto. Coste operacional alto.
- **Criterio de inicio**: evidencia en producción de fotos cuya URL existe en BD pero no en storage.

#### Tarea 3.2 · Read model dedicado para `ProfileStats`
- **Descripción**: proyección que se actualiza por eventos (cuando se crea estudio, cuando se procesa traza, cuando cambia profile). Lectura `O(1)`.
- **Beneficio**: latencia de stats independiente del tamaño de Studies/Traces.
- **Riesgo**: alto. Requiere infraestructura de proyecciones.
- **Criterio de inicio**: latencia de `GET /me/stats` por encima de SLA o crecimiento de datos que evidencie problema.

#### Tarea 3.3 · Rate limiting en endpoints públicos
- **Descripción**: aplicar rate limiting (ASP.NET Core 7+ built-in) a `GET /profiles/{userId}` y `POST /profiles/batch`.
- **Beneficio**: anti-scraping.
- **Riesgo**: bajo.
- **Criterio de inicio**: revisión de seguridad lo prioriza.

---

## 5. Patrones de diseño recomendados

| Patrón | Problema que resuelve | Dónde aplicarlo | Alternativas más simples | Riesgo de sobreingeniería | Ejemplo breve |
|---|---|---|---|---|---|
| **URL Builder** | Hardcoded paths en Application | `IProfilePhotoUrlBuilder` (Tarea 2.1) | Constante en `PhotoSettings.PublicBasePath` con interpolación en el handler | Bajo: ya hay un par de URLs distintas (foto + thumbnail) | `_urlBuilder.BuildUrls(profileId, extension)` retorna ambas |
| **Service Layer (orquestación)** | Query handler con 3 dependencias cross-aggregate | `IProfileStatsService` (Tarea 2.3) | Dejar como está | Bajo: encapsulación natural | Handler delega a `_stats.GetAsync(userId)` |
| **Null Object** | TODO de Alignments | `NullAlignmentStatsProvider` (Tarea 1.4) | Eliminar campos del DTO | Bajo si el campo seguirá existiendo | Provider devuelve `new AlignmentStats(0, 0)` |
| **Options pattern** | Magic numbers de foto | `PhotoSettings` con `IOptions<>` (Tarea 1.3) | Constantes en VO | Bajo | `IOptions<PhotoSettings>.Value.MaxSizeBytes` |

**Patrones rechazados explícitamente:**

| Patrón | Por qué NO |
|---|---|
| **Specification** | No hay queries complejas combinables. Los repositorios son específicos por necesidad. |
| **Repository genérico `IRepository<T>`** | Los repositorios son cohesivos por agregado, así debe ser en DDD. |
| **AutoMapper** | Mappings manuales en `ProfileMappings` son legibles. |
| **Event Sourcing** | No hay requisito de auditoría temporal del perfil. |
| **CQRS read/write split físico** | Sin problema de escala. |
| **Decorator** para logging | `ILogger` inyectado y usado con discreción es suficiente. |
| **Cache distribuido para fotos** | Storage ya da URLs públicas; cachear en aplicación es coste sin métrica. |
| **Mover validación de URL a FluentValidation** | La URL es invariante de dominio: pertenece al VO `ProfilePhoto`. |

---

## 6. Estrategia de testing antes y después

### 6.1 Tests que deben existir antes de refactorizar

#### Red de seguridad
- **`ProfileTests`** (ya existe): cubrir
  - `Create` con sólo `PersonName` válido → éxito + `ProfileCreatedEvent`.
  - `Create` con `PersonName` inválido → error específico.
  - `UpdateBasicInfo` con todos los campos válidos → éxito + `ProfileUpdatedEvent`.
  - `UpdateResearchIdentifiers` con ORCID válido → éxito.
  - `UpdateResearchIdentifiers` con ORCID inválido → error.
  - `UpdatePhoto` con URL válida → éxito + `ProfilePhotoUpdatedEvent`.
  - `RemovePhoto` → asigna `ProfilePhoto.Empty` + evento.
- **Value Object tests** (ya existen): cubrir todos los caminos de cada `Create` (válido/inválido).
- **`UploadProfilePhotoCommandHandlerTests`**: cubrir
  - MIME no permitido → error.
  - Tamaño > máximo → error.
  - Base64 inválido → error.
  - Caso feliz → URL generada con prefix esperado, evento publicado.
- **`GetProfileStatsQueryHandlerTests`**: cubrir todos los conteos (estudios miembro, owner, traces) con datos sembrados.
- **`ProfileRepositoryIntegrationTests`** (a añadir antes de Tarea 2.4): verificar paridad EF Core puro vs ADO.NET con dataset realista. Casos:
  - `GetByUserIdAsync` con perfil no eliminado → encuentra.
  - `GetByUserIdAsync` con perfil soft-deleted → no encuentra.
  - `GetByUserIdsAsync` con mezcla de existentes/no existentes/eliminados → solo válidos.
  - `ExistsForUserAsync` true/false.

### 6.2 Tests a añadir
- `ProfilePhotoUrlBuilderTests` (tras Tarea 2.1).
- `ProfileStatsServiceTests` (tras Tarea 2.3).
- `NullAlignmentStatsProviderTests` (tras Tarea 1.4 opción A).
- `MyProfileEndpointsTests`, `ProfileQueryEndpointsTests` (tras Tarea 2.6) usando `GeneFlowWebApplicationFactory`.

### 6.3 Mocks o fakes necesarios
- `FakeFileStorageService` (probablemente ya existe en tests).
- `FakeImageProcessingService` que devuelve thumbnail dummy.
- `FakeEventBusPublisher` capturando publicaciones.
- `FakeCurrentUserService` con `UserId` configurable.

### 6.4 Cómo asegurar que el comportamiento no cambia
- 100% verde antes y después de cada tarea.
- Tras Tarea 2.1: snapshot del par `(Url, ThumbnailUrl)` en tests para confirmar que el path generado es idéntico.
- Tras Tarea 2.4: tests de integración sobre PostgreSQL real con dataset sembrado.
- Tras Tarea 2.5: test que verifique que `ProfilePhotoUploadedEvent` se entrega exactamente una vez tras commit.
- Mutation testing con Stryker (config existente) sobre `Domain/Profiles` objetivo > 70%.

---

## 7. Orden recomendado de ejecución

```
Fase 1 (orden estricto, commits atómicos):
  1.1  Eliminar comentarios no-doc                ← regla obligatoria
  1.2  Particionar ProfileErrors.cs
  1.3  Magic numbers a PhotoSettings
  1.4  Cerrar TODO de Alignments (Null Object)
  1.5  Helper RequireUserId en endpoints
  1.6  Documentar validación handler vs VO

Fase 2 (en este orden por dependencias):
  2.1  IProfilePhotoUrlBuilder                    ← depende de 1.3
  2.2  Profile.Create enriquecido                 ← independiente
  2.3  IProfileStatsService                       ← independiente
  2.5  Unificar publicación de evento de foto    ← independiente
  2.4  Unificar ORM en ProfileRepository          ← último, requiere integration tests
  2.6  Separar ProfileEndpoints.cs                ← depende de 1.5

Fase 3:
  Reevaluar tras Fase 2 con métricas en mano.
  No ejecutar por estética.
```

Cada paso debe: tener un único responsable, ser revisable en < 200 líneas de diff, pasar `dotnet test` y `dotnet build` (0 warnings) verdes, ser reversible vía `git revert` sin romper otros pasos.

---

## 8. Cambios que NO recomiendo hacer

| Anti-propuesta | Por qué NO |
|---|---|
| Reescribir `Profile.cs` desde cero | Está bien dimensionado (151 líneas, 5 métodos cohesivos). Refactor incremental > rewrite. |
| Convertir `Profile` en agregados separados (`ProfileBasicInfo`, `ProfilePhoto`, `ProfileResearch`) | Las invariantes son débiles entre subgrupos pero los datos pertenecen al mismo agregado conceptual. Separar añade orquestación sin valor. |
| Repository genérico `IRepository<TEntity, TId>` | Los repositorios son cohesivos por agregado. |
| Adoptar AutoMapper para `ProfileMappings` | Mappings explícitos son más legibles. |
| Migrar Result Pattern a `OneOf<TSuccess, TError>` | Funciona, está integrado, tiene `ToHttpResult()`. Churn sin valor. |
| Cache distribuido para fotos / perfiles | Sin métricas de carga que lo justifiquen. |
| Event Sourcing para histórico de perfil | No hay requisito de auditoría temporal. |
| Mover la validación de URL/ORCID a FluentValidation | Son invariantes de dominio: pertenecen al VO. |
| Reemplazar `POST /profiles/batch` por `GET` con query string gigante | Inviable a partir de N IDs por límite de URL; mantener POST. |
| Cambiar el prefix `/storage/profiles/` masivamente sin coordinación con frontend | Rompería URLs ya emitidas. Hacer solo si Tarea 2.1 introduce builder con compatibilidad hacia atrás. |
| AbstractFactory para `ProfilePhoto` | `Create` estático bastará. |
| Migrar ImageSharp a otra librería | Recientemente actualizada (3.1.7 → 3.1.12 por CVE). Sin razón funcional. |
| Cachear thumbnails en CDN como parte del refactor | Decisión de infra; fuera del alcance del refactor de código. |
| Renombrar masivamente VOs/handlers para "consistencia" | Ruido en git blame y bloquea PRs. |

---

## 9. Métricas de calidad sugeridas

**Baseline a medir HOY (antes de empezar Fase 1):**

| Métrica | Herramienta | Baseline hoy | Objetivo post-refactor |
|---|---|---|---|
| Líneas por archivo (max en Profiles) | `cloc` / manual | 337 (`ProfileEndpoints.cs`) | < 200 |
| Líneas por método (max) | SonarLint / Roslyn | ~138 (`UploadProfilePhotoCommandHandler.Handle`) | < 60 |
| Líneas en `ProfileRepository` | manual | 167 | ~110 (post 2.4) |
| Líneas en `CreateProfileCommandHandler` | manual | 122 | ~75 (post 2.2) |
| Líneas en `GetProfileStatsQueryHandler` | manual | 83 | ~25 (post 2.3) |
| Métodos públicos en `Profile` aggregate | manual | 5 | 5 (sin cambios) |
| Comentarios no-doc en módulo | regex `// ` excluyendo `///` | varios | 0 (regla obligatoria) |
| Archivos con > 1 clase pública | regex/análisis | 0 (verificar) | 0 (regla obligatoria) |
| Referencias a `DbConnection`/SQL crudo en repo | grep | 4+ ocurrencias | 0 (post 2.4) |
| Cobertura tests Domain/Profiles | `coverlet` | ~85% | ≥ 90% |
| Cobertura tests Application/Profiles | `coverlet` | ~75% | ≥ 85% |
| Cobertura tests Infrastructure/Profiles | `coverlet` | ~30% | ≥ 60% |
| Mutation score Domain/Profiles | Stryker.NET | sin medir | > 70% |
| TODO comments | grep | 2 (líneas 77-78) | 0 |
| Eventos por creación de perfil | manual | 2 (`Created` + `Updated`) | 1 (`Created`, post 2.2) |
| Warnings de compilación módulo | `dotnet build` | 0 | 0 (mantener) |

Configurar el CI para fallar PR si:
- aparece `// ` no `///` en archivos del módulo Profiles.
- aparece más de un `class` o `record` público por archivo.
- aparece `DbConnection` o `CreateCommand` en `Infrastructure/Profiles/`.
- cobertura baja respecto al baseline.
- nuevos warnings de compilación.

---

## 10. Resultado final esperado

Tras Fase 1 y 2 ejecutadas:

```
Domain/Profiles/
├── Profile.cs                                  (~155 líneas, sin cambios estructurales)
├── ProfileId.cs
├── IProfileRepository.cs
├── IProfileUnitOfWork.cs
├── Errors/                                     (NUEVO — split de ProfileErrors.cs)
│   ├── ProfileAccountErrors.cs
│   ├── ProfileBasicInfoErrors.cs
│   ├── ProfilePhotoErrors.cs
│   └── ProfileResearchErrors.cs
├── Enumerations/
│   └── ResearchField.cs
├── ValueObjects/
│   ├── PersonName.cs
│   ├── Bio.cs
│   ├── Location.cs
│   ├── ProfessionalRole.cs
│   ├── Institution.cs
│   ├── ProfilePhoto.cs
│   └── ResearchIdentifiers.cs
└── Events/
    ├── ProfileCreatedEvent.cs
    ├── ProfileUpdatedEvent.cs
    ├── ProfilePhotoUploadedEvent.cs
    ├── ProfilePhotoUpdatedEvent.cs
    └── ProfilePhotoDeletedEvent.cs

Application/Profiles/
├── Abstractions/                               (NUEVO)
│   ├── IProfilePhotoUrlBuilder.cs
│   ├── IProfileStatsService.cs
│   └── IAlignmentStatsProvider.cs              (Null Object si Tarea 1.4 opción A)
├── Commands/
│   ├── CreateProfile/
│   │   └── CreateProfileCommandHandler.cs      (~75 líneas, antes 122)
│   ├── UpdateProfile/
│   ├── UpdateProfilePhoto/
│   ├── DeleteProfilePhoto/
│   ├── UploadProfilePhoto/
│   │   └── UploadProfilePhotoCommandHandler.cs (~85 líneas, antes 138)
│   └── UpdateResearchIdentifiers/
├── Queries/
│   ├── GetCurrentUserProfile/
│   ├── GetProfileByUserId/
│   ├── GetProfilesByUserIds/
│   └── GetProfileStats/
│       └── GetProfileStatsQueryHandler.cs      (~25 líneas, antes 83)
├── Services/                                   (NUEVO)
│   ├── ProfileStatsService.cs
│   └── NullAlignmentStatsProvider.cs           (si Tarea 1.4 opción A)
├── DTOs/
│   ├── ProfileDto.cs
│   ├── ProfileSummaryDto.cs
│   └── ProfileStatsDto.cs
├── Mappings/
│   └── ProfileMappings.cs
└── EventHandlers/
    ├── ProfilePhotoUploadedEventHandler.cs
    └── ProfilePhotoDeletedEventHandler.cs

Infrastructure/Profiles/
├── Configuration/                              (NUEVO)
│   └── PhotoSettings.cs
├── Persistence/
│   ├── Context/
│   │   └── ProfileContext.cs
│   ├── Configurations/
│   │   └── ProfileConfiguration.cs
│   ├── Repositories/
│   │   ├── ProfileRepository.cs                (~110 líneas, EF Core puro, antes 167)
│   │   └── ProfileUnitOfWork.cs
│   └── Migrations/
└── Services/                                   (NUEVO)
    └── ProfilePhotoUrlBuilder.cs

API/Endpoints/Profiles/                         (separación de responsabilidades)
├── MyProfileEndpoints.cs                       (~180 líneas, /me/...)
└── ProfileQueryEndpoints.cs                    (~80 líneas, /{userId}, /batch)

API/Contracts/Profiles/                         (sin cambios estructurales)
├── Requests/
└── Responses/
```

### 10.1 Beneficios concretos esperados

- `ProfileRepository.cs` de 167 → ~110 líneas; un único modelo de acceso a datos.
- `UploadProfilePhotoCommandHandler.cs` de 138 → ~85 líneas; sin URL hardcodeada; sin validación duplicada.
- `CreateProfileCommandHandler.cs` de 122 → ~75 líneas; un único evento al crear.
- `GetProfileStatsQueryHandler.cs` de 83 → ~25 líneas; orquestación encapsulada en servicio.
- `ProfileEndpoints.cs` de 337 → 2 archivos < 200 líneas; helper centralizado.
- `ProfileErrors.cs` (154 líneas) particionado en 4 archivos < 60 líneas.
- 0 comentarios no-doc, 0 archivos con clases múltiples, 0 TODOs, 0 magic numbers literales en handlers.
- Configurabilidad de foto vía `PhotoSettings` (max size, dimensiones de thumbnail, MIME types, base path público).
- Cobertura de tests Application ≥ 85%, Infrastructure ≥ 60%, mutation score Domain > 70%.

### 10.2 Lo que NO va a cambiar (deliberadamente)

- Result Pattern, MediatR, Clean Architecture base, smart enumerations, value objects existentes que ya funcionan.
- Nombres de endpoints públicos, contratos JSON externos, formato de URL de foto (mismo prefix `/storage/profiles/`).
- Eventos de dominio existentes (excepto consolidación en creación, Tarea 2.2).
- Stack tecnológico (EF Core, PostgreSQL, ImageSharp, MinIO).
- Comportamiento observable desde el cliente: cualquier flujo (crear perfil, ver perfil, actualizar foto, batch lookup, stats) responde igual antes y después.

---

## 11. Suposiciones y preguntas abiertas

### 11.1 Suposiciones explícitas
- `dotnet test` está verde antes de comenzar. Si no, ejecutar primero las correcciones necesarias.
- Ningún `INotificationHandler<ProfileUpdatedEvent>` actúa sobre el caso "creación" actual (Tarea 2.2). **Verificar antes**.
- El prefix de almacenamiento `/storage/profiles/` es estable y debe mantenerse para no romper URLs ya emitidas a clientes.
- Los frontends no dependen del valor 0 hardcodeado de `TotalAlignments` / `CompletedAlignments` (Tarea 1.4 opción B sería breaking; opción A es segura).
- EF Core con la conversión actual de `UserId → string` traduce correctamente `p => p.UserId == userId`. **Verificar con benchmark antes de Tarea 2.4**.
- El sistema actual no requiere outbox (volumen bajo). Reevaluar tras métricas.

### 11.2 Preguntas pendientes (sugeridas para confirmar antes de Fase 2)
- ¿Hay módulo Alignments en hoja de ruta cercana? Si sí, opción A de Tarea 1.4 es preferible; si no se planea, opción B (eliminar campos) puede ser limpia.
- ¿Existe métrica de latencia de `GET /me/stats`? Determina si Tarea 3.2 (read model) es necesario.
- ¿Hay clientes que esperan recibir `ProfileUpdatedEvent` durante la creación? Determina riesgo de Tarea 2.2.
- ¿`IFileStorageService` ya tiene método `GetPublicUrl`? Si sí, Tarea 2.1 puede reusar; si no, conviene crear `IProfilePhotoUrlBuilder` específico.
- ¿Se han medido benchmarks comparando ADO.NET vs EF Core en `GetByUserIdAsync`? Si se documentó la decisión original, recuperar; si no, ejecutar antes de Tarea 2.4.
- ¿Hay rate limiting en gateway (Nginx, Cloudflare) que cubra los endpoints públicos? Determina prioridad de Tarea 3.3.

---

## 12. Próximo paso

Confirmar la aprobación del plan. Una vez aprobado, ejecutar **Fase 1 — Tarea 1.1 (eliminar comentarios no-doc)** primero por ser regla obligatoria de bajo riesgo, seguida del resto de Fase 1 en el orden indicado en §7. Cada tarea es un commit atómico con `dotnet test` y `dotnet build` (0 warnings) verdes.
