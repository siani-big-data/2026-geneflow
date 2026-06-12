# Plan de implementación — Funcionalidades inspiradas en GitHub

> Documento de planificación para incorporar a GeneFlow un conjunto de
> funcionalidades sociales, de organización, trazabilidad y colaboración
> análogas a las de GitHub, adaptadas al dominio bioinformático.
>
> Cubre **backend (.NET)** y **frontend (Next.js)**.
> Las funcionalidades se agrupan en **7 fases incrementales**, ordenadas por
> dependencias técnicas. Cada fase es entregable de forma independiente.

---

## 0. Principios arquitectónicos

### Backend (`geneflow-backend/GeneFlow.ApiNet2`)

- **Clean Architecture + DDD** por contexto delimitado.
- **CQRS (MediatR)**: comandos para escritura, queries para lectura.
- **Event-driven** sobre Redis Streams (`geneflow:events:{category}`).
- **Idempotencia** en comandos y consumidores.
- **Result pattern**, sin excepciones para lógica de negocio.
- Naming de eventos en pasado (`StudyStarredEvent`).
- Migraciones EF Core: una por contexto y release, nombre
  `{Phase}_{Context}_{Description}`.

### Frontend (`geneflow-frontend`)

Convenciones existentes detectadas en la base de código actual:

| Capa | Carpeta | Convención |
|---|---|---|
| Routing | `app/[locale]/` | Route groups `(auth)` y `(platform)` |
| Páginas | `app/[locale]/(platform)/...` | `page.tsx`, layouts anidados |
| Primitivas UI | `app/components/ui/` | kebab-case, Radix UI |
| Layout | `app/components/layout/` | sidebar, page-header |
| Compartido | `app/components/shared/` | empty-state, loading-spinner, status-badge |
| Por feature | `app/components/{feature}/` | studies, payment, pipelines, auth |
| Servicios | `app/services/{x}.service.ts` | wrapper sobre `api-client.ts` |
| Hooks | `app/hooks/use-{x}.ts` | TanStack Query + helpers |
| Stores | `app/stores/{x}-store.ts` | Zustand (auth-store, ui-store) |
| Tipos | `app/types/{x}.ts` | un archivo por dominio |
| API client | `app/lib/api-client.ts` | axios + interceptores JWT |
| i18n | `messages/{en,es}.json` + `app/i18n/` | next-intl |
| Providers | `app/providers/` | query-provider, theme-provider |

**Convención adoptada por estas fases:**

- Cada fase añade su carpeta `app/components/{phase}/`.
- Cada fase añade `app/types/{phase}.ts`, `app/services/{phase}.service.ts`,
  uno o varios `app/hooks/use-{x}.ts`.
- Todas las cadenas pasan por `next-intl` (claves nuevas namespaced por
  feature, p.ej. `activity.timeline.empty`).
- Componentes accesibles (Radix primitives o ARIA equivalente).
- Server Components por defecto; Client Components solo donde haya
  interacción (`'use client'`).

---

## Nuevos contextos delimitados (backend)

| Contexto | Responsabilidad | Stream |
|---|---|---|
| `Social` | Stars, follows, watches | `geneflow:events:social` |
| `Activity` | Feed agregado, timeline por estudio, audit log | `geneflow:events:activity` |
| `Discussions` | Comentarios, hilos, reacciones, menciones | `geneflow:events:discussions` |
| `Notifications` | Centro de notificaciones in-app | `geneflow:events:notifications` |
| `Organization` | Labels, milestones, plantillas, README | `geneflow:events:organization` |
| `Releases` | Snapshots, citaciones, releases | `geneflow:events:releases` |
| `Search` | Búsqueda global indexada | (consumer de varios streams) |
| `Orgs` | Organizaciones, miembros, equipos, propiedad de estudios | `geneflow:events:orgs` |

---

## Componentes UI transversales nuevos (frontend)

Estas piezas se introducen una sola vez y las consumen varias fases:

| Componente | Ubicación | Uso | Introducido en |
|---|---|---|---|
| `command.tsx` | `components/ui/` | Command palette / autocompletado | Fase 5 (lo usa Fase 4 para `@menciones`) |
| `combobox.tsx` | `components/ui/` | Selección con búsqueda (labels, usuarios) | Fase 3 |
| `popover.tsx` | `components/ui/` | Mentions, reactions, label picker | Fase 3 |
| `markdown.tsx` | `components/ui/` | Render seguro (react-markdown + DOMPurify) | Fase 3 (README) |
| `markdown-editor.tsx` | `components/ui/` | Editor con preview y @mentions | Fase 3 (README) / Fase 4 (comentarios) |
| `infinite-scroll.tsx` | `components/shared/` | Listas paginadas con cursor | Fase 1 (timeline) |
| `KeyboardShortcutsProvider` | `providers/` | Ctrl+K, atajos globales | Fase 5 |
| `NotificationsProvider` | `providers/` | Suscriptor SSE para notificaciones | Fase 4 |
| `RealtimeProvider` | `providers/` | Cliente SSE genérico | Fase 1 (timeline live) |

---

# Fase 1 — Fundación de Actividad y Auditoría

**Objetivo:** sustrato común de *event sourcing ligero* que alimente feed,
timeline, audit log y notificaciones del resto de fases.

## 1.1 Backend

### Dominio (`Activity` context)

- Entidad `ActivityEvent { Id, ActorUserId, Verb, ObjectType, ObjectId,
  StudyId?, OccurredAt, Visibility, PayloadJson }`.
- Value objects: `ActivityVerb` (created, updated, deleted, joined,
  uploaded, analyzed, …), `ActivityObjectType` (Study, Trace, Annotation,
  Comment, …).

### Aplicación

- Queries: `GetMyActivityQuery`, `GetStudyTimelineQuery`, `GetAuditLogQuery`.
- Sin comandos directos: la entidad se materializa desde eventos del bus.

### Infraestructura

- Migración `Phase1_Activity_AddActivityEventTable`.
- Repositorio `ActivityEventRepository` (paginación por cursor sobre
  `OccurredAt`).
- **Consumer `ActivityProjectionWorker`** suscrito con patrón a todos los
  streams `geneflow:events:*` (excepto el propio `activity`). Mapea cada
  evento al esquema unificado y persiste.

### API

- `GET /api/activity/me?cursor=…`
- `GET /api/activity/studies/{id}?cursor=…`
- `GET /api/activity/audit?from=…&to=…` (autorización: Owner/Admin)

## 1.2 Frontend

### Tipos y servicios

- `app/types/activity.ts`: `ActivityEvent`, `ActivityVerb`, `ActivityObjectType`.
- `app/services/activity.service.ts`: `getMyActivity`, `getStudyTimeline`,
  `getAuditLog`.

### Hooks

- `app/hooks/use-activity.ts`: `useMyActivity`, `useStudyTimeline`,
  `useAuditLog` (cursor-based con `useInfiniteQuery`).

### Componentes

- `app/components/activity/ActivityTimeline.tsx` — lista con virtualización.
- `app/components/activity/ActivityItem.tsx` — render por `verb + objectType`.
- `app/components/activity/ActivityIcon.tsx` — icono según verbo.
- `app/components/shared/infinite-scroll.tsx` (transversal).

### Páginas / integraciones

- Nueva pestaña *Activity* en la página de estudio (cuando se cree la
  ruta `[locale]/(platform)/studies/[id]/page.tsx`).
- Sección *Activity* en el dashboard del usuario.
- Nueva página `[locale]/(platform)/settings/security/audit-log/page.tsx`.

### Providers

- `RealtimeProvider` (SSE) para anexar nuevos `ActivityEvent` al cache de
  React Query al vuelo (reutiliza la infraestructura SSE existente).

### i18n

- Claves: `activity.timeline.empty`, `activity.verbs.created`,
  `activity.verbs.uploaded`, `activity.objects.study`, etc.

## 1.3 Tests

- **BE unit**: proyección de cada `verb + objectType` a `ActivityEvent`,
  validación de queries, filtros de visibilidad.
- **BE integración**: `ActivityProjectionWorker` consumiendo todos los
  streams sobre Testcontainers Redis + Postgres, idempotencia ante
  reentrega.
- **BE E2E API**: paginación por cursor de
  `/api/activity/{me|studies/{id}|audit}`, autorización del audit log,
  SSE de timeline.
- **FE E2E**: timeline de estudio carga inicial + scroll infinito,
  aparición en vivo de un evento nuevo vía SSE, página de audit log
  oculta para roles no admin.

---

# Fase 2 — Social primitivo (Stars, Follow, Pin)

**Objetivo:** vínculos sociales básicos y descubrimiento entre usuarios.

## 2.1 Backend

### Dominio (`Social` context)

- `Star { UserId, StudyId, StarredAt }` (PK compuesta).
- `Follow { FollowerId, FolloweeId, FollowedAt }` (PK compuesta).
- `PinnedStudy { UserId, StudyId, Order }` (máx. 6 por usuario, invariante
  en dominio).

### Eventos

- `StudyStarredEvent`, `StudyUnstarredEvent`,
  `UserFollowedEvent`, `UserUnfollowedEvent`,
  `StudyPinnedEvent`, `StudyUnpinnedEvent`.

### API

- `POST /api/studies/{id}/star` · `DELETE /api/studies/{id}/star`
- `GET /api/users/{id}/stars` · `GET /api/studies/{id}/stargazers`
- `POST /api/users/{id}/follow` · `DELETE /api/users/{id}/follow`
- `GET /api/users/{id}/followers` · `GET /api/users/{id}/following`
- `PUT /api/profiles/me/pinned` (body: array ordenado de studyIds)

## 2.2 Frontend

### Tipos y servicios

- `app/types/social.ts`: `Star`, `Follow`, `PinnedStudy`, `Stargazer`.
- `app/services/social.service.ts`.

### Hooks

- `app/hooks/use-stars.ts`: `useToggleStar(studyId)`, `useStargazers(studyId)`,
  `useMyStarredStudies()`.
- `app/hooks/use-follows.ts`: `useFollow(userId)`, `useFollowers(userId)`,
  `useFollowing(userId)`.
- `app/hooks/use-pinned-studies.ts`: lectura + mutación con optimismo.

### Componentes

- `app/components/social/StarButton.tsx` (con contador + optimista).
- `app/components/social/FollowButton.tsx`.
- `app/components/social/PinnedStudiesGrid.tsx` (drag&drop con `@dnd-kit`).
- `app/components/social/UserCard.tsx` (avatar + nombre + bio + follow).
- `app/components/social/StargazersDialog.tsx`.

### Páginas

- Modificar header de estudio para incluir `StarButton`.
- Modificar `(platform)/profile/page.tsx` para mostrar *Pinned studies* y
  contadores de followers/following.
- **Nueva** `(platform)/users/[handle]/page.tsx` (perfil público) con
  tabs: Overview, Studies, Activity.

### i18n

- `social.star.*`, `social.follow.*`, `social.pinned.*`, `social.profile.*`.

## 2.3 Tests

- **BE unit**: invariantes (no duplicar `Star`, no auto-follow, máx 6
  `PinnedStudy`), handlers de cada comando con todas sus ramas.
- **BE integración**: PK compuestas en Postgres, publicación de
  `StudyStarredEvent` / `UserFollowedEvent` en Redis tras la mutación.
- **BE E2E API**: `POST/DELETE /api/studies/{id}/star`,
  `/api/users/{id}/follow`, `PUT /api/profiles/me/pinned` con permisos
  y errores (409 al duplicar, 400 al pasar de 6 pinned).
- **FE E2E**: star/unstar optimista, follow/unfollow desde perfil,
  drag&drop de pinned studies, perfil público `users/[handle]` con
  pestañas y contadores correctos.

---

# Fase 3 — Organización y metadatos

**Objetivo:** dotar a los estudios de estructura colaborativa (issues-like
del estilo GitHub: labels, milestones, README, plantillas).

## 3.1 Backend

### Dominio (`Organization` context)

- `Label { Id, StudyId, Name, Color, Description }`.
- `Milestone { Id, StudyId, Title, Description, DueDate, Status }`.
- `StudyTemplate { Id, OwnerId, Name, BlueprintJson, Visibility }`.
- Asociaciones N:M: `Trace ↔ Label`, `Annotation ↔ Label`.
- Asociación N:1: `Trace ↔ Milestone`.
- Campo nuevo: `Study.ReadmeMarkdown` (text, nullable).

### Eventos

- `LabelCreatedEvent`, `LabelAppliedEvent`, `LabelRemovedEvent`,
  `MilestoneCreatedEvent`, `MilestoneClosedEvent`,
  `StudyTemplateInstantiatedEvent`, `ReadmeUpdatedEvent`.

### API

- `GET/POST/PUT/DELETE /api/studies/{id}/labels[/{labelId}]`.
- `POST /api/traces/{id}/labels` · `DELETE /api/traces/{id}/labels/{labelId}`.
- `GET/POST/PUT/DELETE /api/studies/{id}/milestones[/{milestoneId}]`.
- `GET/POST/DELETE /api/templates` · `POST /api/studies/from-template/{templateId}`.
- `PUT /api/studies/{id}/readme`.

## 3.2 Frontend

### Tipos y servicios

- `app/types/organization.ts`: `Label`, `Milestone`, `StudyTemplate`,
  `Readme`.
- `app/services/labels.service.ts`, `milestones.service.ts`,
  `templates.service.ts`, `readme.service.ts`.

### Hooks

- `use-labels.ts`, `use-milestones.ts`, `use-templates.ts`, `use-readme.ts`.

### Componentes UI transversales

- `components/ui/popover.tsx`, `combobox.tsx`, `markdown.tsx`,
  `markdown-editor.tsx`.

### Componentes de feature

- `app/components/organization/LabelChip.tsx`, `LabelPicker.tsx`
  (popover + combobox), `LabelManager.tsx` (CRUD desde settings del estudio).
- `app/components/organization/MilestoneCard.tsx`, `MilestoneForm.tsx`,
  `MilestoneProgress.tsx`.
- `app/components/organization/ReadmeEditor.tsx` (split editor + preview).
- `app/components/organization/ReadmeRenderer.tsx`.
- `app/components/organization/TemplatePicker.tsx`,
  `CreateFromTemplateDialog.tsx`.

### Páginas

- Añadir pestañas a la página de estudio: `Milestones`,
  `Settings/Labels`, `README` (visible en home del estudio).
- Modificar `CreateStudyDialog` para incluir *Create from template*.

### i18n

- `organization.labels.*`, `organization.milestones.*`,
  `organization.readme.*`, `organization.templates.*`.

## 3.3 Tests

- **BE unit**: invariantes de `Label` (nombre único por estudio,
  color válido), `Milestone` (fecha futura, transiciones de estado),
  validación de `BlueprintJson` de plantilla, sanitización del
  `ReadmeMarkdown`.
- **BE integración**: asociaciones N:M (`Trace ↔ Label`,
  `Annotation ↔ Label`) en Postgres, instanciación de plantilla
  generando estudio + trazas seed, persistencia del README.
- **BE E2E API**: CRUD de labels/milestones con permisos por rol,
  `POST /api/studies/from-template/{id}`, `PUT /api/studies/{id}/readme`
  con validación de tamaño y MIME.
- **FE E2E**: aplicar/quitar label desde el visor, crear milestone
  y arrastrar trazas a él, editar README con preview en vivo,
  crear estudio desde plantilla.

---

# Fase 4 — Comunicación (comentarios, menciones, reacciones, notificaciones)

**Objetivo:** comunicación nativa en torno a los artefactos científicos.

## 4.1 Backend

### Dominio

- `Discussions` context:
  - `Discussion { Id, StudyId, Title, Category, AuthorId, CreatedAt,
    IsLocked }`.
  - `Comment { Id, ParentType (Discussion|Annotation|Trace), ParentId,
    AuthorId, BodyMarkdown, CreatedAt, EditedAt }`.
  - `Reaction { Id, CommentId, UserId, Emoji }`.
  - `Mention { Id, CommentId, MentionedUserId }` (extraída del body).
- `Notifications` context:
  - `Notification { Id, UserId, Type, Subject, Url, IsRead, CreatedAt }`.
  - `Watch { UserId, StudyId, Level (All|Mentions|None) }`.

### Consumers

- `MentionNotifier` (consume `CommentCreatedEvent`, extrae @, crea
  notificaciones).
- `WatchNotifier` (consume cambios y notifica a watchers según level).
- `InvitationNotifier`, `RoleChangeNotifier`, `AnalysisFailureNotifier`.

### Eventos

- `DiscussionCreatedEvent`, `CommentCreatedEvent`, `CommentEditedEvent`,
  `CommentDeletedEvent`, `ReactionAddedEvent`, `ReactionRemovedEvent`,
  `NotificationCreatedEvent`, `NotificationReadEvent`,
  `WatchUpdatedEvent`.

### API

- `GET/POST /api/studies/{id}/discussions[/{discussionId}]`.
- `GET/POST /api/discussions/{id}/comments`.
- `POST /api/{annotations|traces}/{id}/comments`.
- `POST /api/comments/{id}/reactions/{emoji}` · `DELETE …`.
- `GET /api/notifications?unreadOnly=…&cursor=…`.
- `POST /api/notifications/{id}/read` · `POST /api/notifications/read-all`.
- `PUT /api/studies/{id}/watch` (body `{ level }`).
- **SSE** `GET /api/notifications/stream` (reutiliza patrón SSE existente).

## 4.2 Frontend

### Tipos y servicios

- `app/types/discussions.ts`, `app/types/notifications.ts`.
- `app/services/discussions.service.ts`, `notifications.service.ts`,
  `watch.service.ts`.

### Hooks

- `use-discussions.ts`, `use-comments.ts`, `use-reactions.ts`,
  `use-mentions.ts` (autocompletado de usuarios del estudio),
  `use-notifications.ts`, `use-watch.ts`.

### Stores

- `app/stores/notifications-store.ts`: contador de no-leídas, cache local,
  receptor del `NotificationsProvider`.

### Componentes UI transversales

- `components/ui/command.tsx` (autocompletado de @menciones),
  `components/ui/markdown-editor.tsx` (extensión: mention plugin).

### Componentes de feature

- `app/components/discussions/DiscussionList.tsx`, `DiscussionDetail.tsx`,
  `NewDiscussionDialog.tsx`.
- `app/components/comments/CommentThread.tsx`, `CommentItem.tsx`,
  `CommentForm.tsx`, `CommentActions.tsx`.
- `app/components/comments/ReactionPicker.tsx`, `ReactionsBar.tsx`.
- `app/components/notifications/NotificationBell.tsx` (en header,
  contador unread).
- `app/components/notifications/NotificationList.tsx`,
  `NotificationItem.tsx`.
- `app/components/notifications/WatchSelector.tsx` (en header de estudio).

### Providers

- `app/providers/notifications-provider.tsx` (cliente SSE: añade
  notificaciones al store al vuelo).

### Páginas

- Pestaña *Discussions* en la página de estudio.
- Sección de comentarios bajo cada anotación (en visor de cromatograma).
- **Nueva** `(platform)/notifications/page.tsx` con lista paginada y
  filtros.
- Header global: añadir `NotificationBell` al lado del avatar.

### i18n

- `discussions.*`, `comments.*`, `notifications.*`, `watch.*`.

## 4.3 Tests

- **BE unit**: extracción de `@menciones` y normalización a
  `Mention`, invariantes de `Reaction` (un emoji por usuario y
  comentario), edición/borrado con autoría, niveles de `Watch`.
- **BE integración**: cadena `CommentCreatedEvent → MentionNotifier
  → Notification` sobre Redis + Postgres, `WatchNotifier` filtrando
  por nivel, idempotencia de reacciones.
- **BE E2E API**: comentarios sobre `Discussion|Annotation|Trace`
  con permisos en estudios privados, paginación de notificaciones,
  SSE `/api/notifications/stream` recibiendo eventos en vivo.
- **FE E2E**: redactar comentario con `@mención` autocompletada,
  añadir/quitar reacción, recepción de notificación en tiempo real,
  marcar como leída e ir a la URL contextual.

---

# Fase 5 — Descubrimiento global

**Objetivo:** convertir el corpus público y la actividad propia en algo
navegable y buscable.

## 5.1 Backend

### Dominio

- `Search` context:
  - Tabla `SearchIndex { ObjectType, ObjectId, OwnerId, Title, Body,
    Tags, Tsv (tsvector), UpdatedAt }`.
  - Consumer único `SearchIndexer` que escucha `studies`, `traces`,
    `discussions`, `users` y mantiene el índice.

### Aplicación

- `GlobalSearchQuery(q, type?, owner?, tag?, cursor)`.
- `ExploreTrendingQuery`, `ExploreRecentQuery`, `FeedQuery(cursor)`
  (combina follows + watches + propio).

### API

- `GET /api/search?q=…&type=…&owner=…&tag=…&cursor=…`.
- `GET /api/explore/trending` · `GET /api/explore/recent` ·
  `GET /api/explore/featured`.
- `GET /api/feed?cursor=…`.

## 5.2 Frontend

### Tipos y servicios

- `app/types/search.ts`, `app/types/feed.ts`, `app/types/explore.ts`.
- `app/services/search.service.ts`, `explore.service.ts`, `feed.service.ts`.

### Hooks

- `use-search.ts`, `use-explore.ts`, `use-feed.ts` (infinite cursor).

### Componentes UI transversales

- `components/ui/command.tsx` (palette Ctrl+K).

### Componentes de feature

- `app/components/search/CommandPalette.tsx`.
- `app/components/search/SearchResults.tsx`, `SearchResultItem.tsx`.
- `app/components/explore/TrendingSection.tsx`, `FeaturedSection.tsx`,
  `RecentSection.tsx`.
- `app/components/feed/FeedItem.tsx`, `FeedList.tsx`.

### Providers

- `app/providers/keyboard-shortcuts-provider.tsx` (Ctrl/⌘+K abre palette,
  G+S va a settings, etc.).

### Páginas

- Rework `(platform)/discover/page.tsx` con secciones Trending /
  Recent / Featured.
- Integrar `FeedList` como widget principal del dashboard.
- Palette accesible desde cualquier página del shell.

### i18n

- `search.*`, `explore.*`, `feed.*`, `shortcuts.*`.

## 5.3 Tests

- **BE unit**: construcción del `tsvector` por tipo de objeto,
  ranking de `GlobalSearchQuery`, composición del feed
  (follows + watches + propio).
- **BE integración**: `SearchIndexer` consumiendo streams
  `studies`/`traces`/`discussions`/`users` y actualizando
  `SearchIndex` en Postgres, reindexado tras edición/borrado.
- **BE E2E API**: `GET /api/search` con filtros combinados,
  `/api/explore/{trending,recent,featured}`, `/api/feed` paginado
  por cursor, respeto de visibilidad en resultados.
- **FE E2E**: command palette abierta con Ctrl/⌘+K, navegación por
  teclado entre resultados, paginación infinita en feed y discover,
  atajos globales (`G S` → settings).

---

# Fase 7 — Organizaciones (cuentas de equipo estilo GitHub)

**Objetivo:** introducir un segundo tipo de propietario, las
**Organizations**, que agrupan personas en torno a un laboratorio,
grupo de investigación o empresa, con sus propios estudios, miembros,
equipos y permisos. Habilita compartir, facturar y colaborar bajo una
entidad común sin depender de un único usuario propietario.

> Esta fase reaprovecha toda la infraestructura social, de
> notificaciones, actividad, descubrimiento y releases ya entregada en
> fases anteriores. La diferencia clave es que el dueño de un estudio
> deja de ser exclusivamente un `User` y pasa a ser un
> `OwnerRef = User | Org`.

## 7.1 Backend

### Dominio (`Orgs` context)

- `Org { Id, Handle, Name, Description, AvatarUrl, WebsiteUrl,
  Location, Visibility (Public|Private), CreatedAt, BillingPlanId? }`.
  - `Handle` único global, validado contra reservadas y contra
    handles de usuario (mismo namespace que `User.Handle`).
- `OrgMember { OrgId, UserId, Role (Owner|Admin|Member), JoinedAt }`
  (PK compuesta). Invariante: siempre ≥ 1 `Owner`.
- `Team { Id, OrgId, Slug, Name, Description, ParentTeamId? }`
  (jerarquía con un solo nivel de anidación).
- `TeamMember { TeamId, UserId, Role (Maintainer|Member) }`.
- `TeamStudyAccess { TeamId, StudyId, Permission (Read|Write|Admin) }`.
- `OrgInvitation { Id, OrgId, InvitedEmail, InvitedUserId?, Role,
  Token, ExpiresAt, Status }`.
- Cambios en agregados existentes:
  - `Study.OwnerType ∈ {User, Org}`, `Study.OwnerId` referencia a
    cualquiera de los dos. Resolución vía value object `OwnerRef`.
  - Migración de datos: estudios actuales conservan `OwnerType = User`.

### Aplicación

- Comandos: `CreateOrgCommand`, `UpdateOrgCommand`, `DeleteOrgCommand`,
  `InviteOrgMemberCommand`, `AcceptOrgInvitationCommand`,
  `ChangeOrgMemberRoleCommand`, `RemoveOrgMemberCommand`,
  `CreateTeamCommand`, `AddTeamMemberCommand`,
  `GrantTeamStudyAccessCommand`, `TransferStudyOwnershipCommand`
  (User→Org, Org→Org, Org→User).
- Queries: `GetOrgByHandleQuery`, `ListMyOrgsQuery`,
  `ListOrgMembersQuery`, `ListOrgStudiesQuery`, `ListOrgTeamsQuery`,
  `ResolveEffectivePermissionsQuery(userId, studyId)` (combina
  rol en org + miembro de team + acceso directo).
- Política unificada `IStudyAuthorizationService`: cualquier
  endpoint existente que comprobaba `Study.OwnerId == currentUser`
  pasa a llamar a `ResolveEffectivePermissionsQuery`.

### Eventos

- `OrgCreatedEvent`, `OrgUpdatedEvent`, `OrgDeletedEvent`,
  `OrgMemberInvitedEvent`, `OrgMemberJoinedEvent`,
  `OrgMemberRoleChangedEvent`, `OrgMemberRemovedEvent`,
  `TeamCreatedEvent`, `TeamMemberAddedEvent`,
  `TeamStudyAccessGrantedEvent`,
  `StudyOwnershipTransferredEvent`.

### Infraestructura

- Migración `Phase7_Orgs_AddOrgsTables` (orgs, org_members, teams,
  team_members, team_study_access, org_invitations).
- Migración `Phase7_Orgs_AddStudyOwnerTypeColumn` (default `User`
  + backfill).
- Índice único sobre `(LOWER(handle))` compartido con tabla de
  usuarios (constraint cruzado con vista materializada o trigger).
- Consumer `InvitationNotifier` (reutiliza el de la Fase 4) escucha
  `OrgMemberInvitedEvent` y crea notificación + email.
- Adaptadores ya existentes (search, activity) consumen los nuevos
  eventos sin cambios estructurales, solo extendiendo
  `ActivityObjectType` con `Org`, `Team`.

### API

- Orgs:
  - `POST /api/orgs` · `GET /api/orgs/{handle}` ·
    `PATCH /api/orgs/{handle}` · `DELETE /api/orgs/{handle}`.
  - `GET /api/orgs/{handle}/studies?cursor=…`.
  - `GET /api/me/orgs`.
- Miembros e invitaciones:
  - `GET /api/orgs/{handle}/members`.
  - `POST /api/orgs/{handle}/invitations`
    (body: `{ email | userId, role }`).
  - `POST /api/invitations/{token}/accept` ·
    `POST /api/invitations/{token}/decline`.
  - `PATCH /api/orgs/{handle}/members/{userId}` (cambio de rol).
  - `DELETE /api/orgs/{handle}/members/{userId}`.
- Teams:
  - `GET/POST /api/orgs/{handle}/teams[/{slug}]`.
  - `PATCH/DELETE /api/orgs/{handle}/teams/{slug}`.
  - `POST/DELETE /api/orgs/{handle}/teams/{slug}/members/{userId}`.
  - `PUT /api/orgs/{handle}/teams/{slug}/access/{studyId}`
    (body: `{ permission }`).
- Transferencia:
  - `POST /api/studies/{id}/transfer`
    (body: `{ toOwnerType, toOwnerHandle }`).

## 7.2 Frontend

### Tipos y servicios

- `app/types/orgs.ts`: `Org`, `OrgMember`, `Team`, `TeamMember`,
  `TeamStudyAccess`, `OrgInvitation`, `OwnerRef`.
- `app/services/orgs.service.ts`, `teams.service.ts`,
  `org-invitations.service.ts`, `ownership.service.ts`.

### Hooks

- `use-orgs.ts`: `useOrg(handle)`, `useMyOrgs()`,
  `useOrgStudies(handle)`, `useCreateOrg()`, `useUpdateOrg()`.
- `use-org-members.ts`: `useOrgMembers`, `useInviteMember`,
  `useChangeMemberRole`, `useRemoveMember`.
- `use-teams.ts`: `useTeams`, `useTeam`, `useTeamMembers`,
  `useGrantTeamStudyAccess`.
- `use-org-invitations.ts`: `useMyInvitations`, `useAcceptInvitation`,
  `useDeclineInvitation`.
- `use-ownership.ts`: `useTransferStudyOwnership`.

### Stores

- `app/stores/active-org-store.ts` (Zustand): organización
  actualmente seleccionada en el contexto del shell (persistida en
  `localStorage`). Reset al hacer logout.

### Componentes UI transversales

- `app/components/ui/owner-avatar.tsx`: avatar uniforme para
  `OwnerRef` (User u Org). Lo consumen tarjetas de estudio,
  cabeceras y selector.

### Componentes de feature

- `app/components/orgs/OrgSwitcher.tsx`: combobox en el sidebar para
  cambiar entre cuenta personal y orgs (estilo selector de GitHub).
- `app/components/orgs/CreateOrgDialog.tsx` (handle, nombre, plan).
- `app/components/orgs/OrgHeader.tsx` (avatar, handle, nav).
- `app/components/orgs/OrgOverview.tsx` (READMÉ + pinned studies de
  la org, reutiliza `PinnedStudiesGrid` y `ReadmeRenderer`).
- `app/components/orgs/OrgMembersList.tsx`,
  `OrgMemberRow.tsx`, `InviteMemberDialog.tsx`.
- `app/components/orgs/TeamsList.tsx`, `TeamDetail.tsx`,
  `TeamMembersList.tsx`, `TeamAccessTable.tsx`.
- `app/components/orgs/OrgSettingsForm.tsx`,
  `OrgBillingPanel.tsx` (placeholder integrable con la fase de
  facturación existente).
- `app/components/orgs/TransferOwnershipDialog.tsx`
  (selector de destino + confirmación por handle).
- `app/components/orgs/InvitationBanner.tsx` (en dashboard si hay
  invitaciones pendientes).

### Páginas

- **Nueva** `(platform)/orgs/new/page.tsx` (crear org).
- **Nueva** `(platform)/orgs/[handle]/page.tsx` (overview pública).
- **Nueva** `(platform)/orgs/[handle]/studies/page.tsx`.
- **Nueva** `(platform)/orgs/[handle]/people/page.tsx`.
- **Nueva** `(platform)/orgs/[handle]/teams/page.tsx` y
  `(platform)/orgs/[handle]/teams/[slug]/page.tsx`.
- **Nueva** `(platform)/orgs/[handle]/settings/page.tsx`
  (con secciones General, Members, Teams, Billing, Danger zone).
- **Nueva** `(platform)/invitations/page.tsx`
  (lista de invitaciones pendientes del usuario).
- Modificar `CreateStudyDialog` para incluir selector de
  propietario (`OwnerRef` = personal u org del usuario donde tenga
  permiso de crear).
- Modificar página de estudio: nueva acción *Transfer ownership* en
  Settings.
- Modificar sidebar global para mostrar `OrgSwitcher`.

### Providers

- Sin providers nuevos; se reutilizan `RealtimeProvider` y
  `NotificationsProvider` para eventos de invitación, cambio de rol
  y transferencia.

### i18n

- `orgs.*` (`overview`, `create`, `header`, `settings`,
  `dangerZone`), `orgs.members.*`, `orgs.teams.*`,
  `orgs.invitations.*`, `orgs.transfer.*`, `orgs.switcher.*`.

## 7.3 Tests

- **BE unit**: invariantes (`Org` siempre con ≥ 1 Owner, handle no
  colisiona con `User.Handle`, no se puede borrar org con estudios),
  resolución de permisos efectivos (`org role × team membership ×
  direct access`), validación de invitación expirada / aceptada dos
  veces.
- **BE integración**: migración `Phase7_Orgs_AddStudyOwnerTypeColumn`
  con backfill correcto, FK compuestas en Postgres, consumer de
  `OrgMemberInvitedEvent` generando notificación, propagación de
  `StudyOwnershipTransferredEvent` al indexer de búsqueda.
- **BE E2E API**: ciclo completo crear org → invitar miembro →
  aceptar → crear team → conceder acceso a estudio → transferir
  estudio User→Org, con todos los códigos de error esperados
  (403 al actuar sin rol, 409 al duplicar handle, 410 con token
  expirado).
- **FE E2E**: crear org desde el dialog, cambiar al contexto org con
  `OrgSwitcher`, invitar a usuario por email, aceptar invitación
  desde otra cuenta (segundo browser context), crear team y darle
  acceso a un estudio, transferir un estudio personal a la org y
  comprobar que el botón `StarButton` y los permisos siguen
  funcionando, accesibilidad de los diálogos modales.

---

## Aspectos transversales

### Permisos

- `Study.Visibility` ∈ {Private, Team, Public} controla quién lee.
- `StudyMember.Role` ∈ {Owner, Admin, Member, Viewer} controla quién
  comenta, etiqueta, crea releases, hace fork, etc.
- Acciones públicas (star, fork, follow) requieren solo autenticación.

### Eventos

- Convención: nombre en pasado, payload mínimo (IDs + contexto),
  versionado con campo `event_version`.
- Cada fase actualiza `docs/EVENT_CATALOG.md`.

### Migraciones

- Una migración por contexto y release. Nombrado:
  `{Phase}_{Context}_{Description}`.

### Frontend transversal

- **Acceso uniforme al API**: todos los servicios pasan por
  `app/lib/api-client.ts` (axios + interceptor JWT).
- **Estado servidor**: TanStack Query con `useInfiniteQuery` para todo lo
  paginado por cursor (timelines, feed, búsqueda, notificaciones).
- **Estado cliente**: Zustand solo para UI persistente (notif. contador,
  drawer abierto, paleta Ctrl+K abierta).
- **Toast** vía Sonner existente en éxito/error de mutaciones.
- **Accesibilidad**: todos los nuevos componentes interactivos usan
  Radix o ARIA propio + foco visible + soporte teclado.
- **i18n**: cada PR de feature incluye `en.json` y `es.json` con las
  nuevas claves. No se permiten cadenas hardcodeadas.
- **Skeletons**: cada lista nueva trae su skeleton (siguiendo
  `components/ui/skeleton.tsx`).

### Documentación

Cada fase entregada actualiza:

- `docs/EVENT_CATALOG.md` con nuevos eventos.
- `docs/DATA_CONTRACTS.md` con nuevos DTO.
- Swagger / OpenAPI generado automáticamente.
- Storybook (si se introduce) con los nuevos componentes UI transversales.

### Testing

Toda fase entrega tres capas de tests en backend y una en frontend.
Se considera **bloqueante** para mezclar a `master`:

- Cobertura mínima dominio + aplicación: **80 %**.
- Verde en CI: unitarios, integración, E2E backend y E2E frontend.

#### Backend — Unitarios (`tests/GeneFlow.ApiNet2.UnitTests`)

- **Framework**: xUnit + FluentAssertions + NSubstitute.
- **Aislamiento**: sin red, sin DB, sin Redis. Mocks de
  repositorios, `IEventBusPublisher`, `IClock`, `ICurrentUser`.
- **Qué se prueba**:
  - Invariantes de dominio (constructores, métodos de agregado,
    value objects).
  - Validadores FluentValidation por comando/query.
  - Handlers MediatR (lógica de aplicación) con todas sus ramas
    (éxito, conflicto, no encontrado, no autorizado).
  - Proyectores y mappers de eventos.
- **Nomenclatura**: `{Subject}_Should_{Behaviour}_When_{Condition}`.
- **Convención por fase**: una clase de test por agregado / handler nuevo.

#### Backend — Integración (`tests/GeneFlow.ApiNet2.IntegrationTests`)

- **Framework**: xUnit + `WebApplicationFactory` + `Testcontainers`
  (PostgreSQL, Redis, MinIO) + Respawn para reset entre tests.
- **Qué se prueba**:
  - Repositorios EF Core contra Postgres real (queries, migraciones,
    índices, paginación por cursor).
  - Publicación y consumo real sobre Redis Streams
    (`geneflow:events:*`), incluyendo idempotencia y reintentos.
  - Consumers/workers (p.ej. `ActivityProjectionWorker`,
    `MentionNotifier`, `SearchIndexer`) end-to-end dentro del proceso.
  - Outbox + dispatcher de eventos de dominio.
- **Setup**: una `Fixture` por contexto delimitado, base de datos
  efímera por colección, seed mínimo determinista.

#### Backend — E2E de API (`tests/GeneFlow.ApiNet2.ApiE2ETests`)

- **Framework**: xUnit + `HttpClient` sobre `WebApplicationFactory`
  con stack real (Testcontainers) + autenticación JWT real emitida
  por el endpoint dev de identidad.
- **Qué se prueba**:
  - Flujo completo HTTP por feature, incluyendo autorización,
    paginación, filtros y SSE.
  - Contratos OpenAPI: validación del JSON de respuesta contra el
    schema generado.
  - Permisos por rol (`Owner`, `Admin`, `Member`, `Viewer`) y por
    visibilidad (`Private`, `Team`, `Public`).
  - Casos de error: 400/401/403/404/409/422 con `ProblemDetails`.
- **Convención**: un archivo `*EndpointsTests.cs` por grupo de
  endpoints nuevo de la fase.

#### Frontend — E2E (`geneflow-frontend/tests/e2e`)

- **Framework**: Playwright (Chromium + WebKit) contra el backend
  arrancado vía `docker-compose.dev.yml` con datos seedeados.
- **Qué se prueba**:
  - Happy path por feature nueva (crear, listar, editar, borrar).
  - Interacciones en tiempo real (SSE de actividad y notificaciones).
  - Permisos vistos desde la UI (botones ocultos según rol).
  - Accesibilidad básica (`@axe-core/playwright`) en cada página nueva.
  - i18n: ejecución en `en` y `es`, comprobando ausencia de claves
    crudas (`feature.foo.bar`).
  - Estados vacíos, loading skeletons y errores.
- **Convención**: un `*.spec.ts` por flujo de usuario, fixtures
  reutilizables en `tests/e2e/fixtures/` para login y seed por rol.

#### CI

- Pipeline por PR:
  `unit → integration → api-e2e → build frontend → frontend-e2e`.
- Cancelar pipeline en cuanto falle la capa más barata.
- Artefactos en caso de fallo: trazas de Playwright, dumps de
  Postgres y logs de Redis.
- **Smoke** post-deploy: 1 endpoint crítico por fase
  (p.ej. `GET /api/activity/me`, `GET /api/notifications`,
  `GET /api/search?q=test`).

---

## Secuenciación recomendada

```
Fase 1 (Activity)  ──┬──► Fase 2 (Social)
                     ├──► Fase 3 (Organization)
                     └──► Fase 4 (Comm.) ──► Fase 5 (Discovery)
                                              │
                                              ▼
                                          Fase 6 (Releases)
                                              │
                                              ▼
                                          Fase 7 (Orgs)
```

- **Fase 1** es prerrequisito casi universal: feed, audit log,
  notificaciones y búsqueda dependen de su consumer.
- **Fases 2 y 3** son independientes entre sí y pueden ir en paralelo.
- **Fase 4** depende de la 1 (para watchers/notif).
- **Fase 5** depende de 1 y 4 (feed combinado y palette).
- **Fase 6** consolida la trazabilidad acumulada.
- **Fase 7 (Orgs)** se deja al final porque introduce un segundo
  tipo de propietario y necesita que el resto de la plataforma
  (social, comms, releases, search) ya esté en marcha para integrarse
  sin reescribir endpoints.

## Resumen entregables por fase

| Fase | Contextos BE | Carpetas FE | Páginas FE nuevas |
|---|---|---|---|
| 1 | Activity | `components/activity/`, `providers/realtime-provider` | `settings/security/audit-log` |
| 2 | Social | `components/social/` | `users/[handle]` |
| 3 | Organization | `components/organization/` | (tabs en estudio) |
| 4 | Discussions, Notifications | `components/discussions/`, `comments/`, `notifications/`, `providers/notifications-provider` | `notifications` |
| 5 | Search | `components/search/`, `explore/`, `feed/`, `providers/keyboard-shortcuts-provider` | (rework `discover`) |
| 6 | Releases | `components/releases/`, `compare/` | `compare` |
| 7 | Orgs | `components/orgs/`, `ui/owner-avatar`, `stores/active-org-store` | `orgs/new`, `orgs/[handle]` y subrutas, `invitations` |
