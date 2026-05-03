# Plan de Tests: Módulo Studies

## Cobertura Actual: ~30%
## Tests Estimados: ~377 tests

### Objetivos de Cobertura por Capa

| Capa | Archivos | Line | Branch | Method |
|------|----------|------|--------|--------|
| **Domain** | Study.cs, Entities/*, ValueObjects/*, Enumerations/* | **≥95%** | **≥90%** | **≥98%** |
| **Application** | Commands/*, Queries/* | **≥90%** | **≥85%** | **≥95%** |
| **Infrastructure** | Repositories/*, Services/* | **≥75%** | **≥70%** | **≥85%** |
| **API** | Endpoints/* | **≥85%** | **≥80%** | **≥90%** |

### Objetivos Específicos por Archivo

| Archivo | Line | Branch | Prioridad |
|---------|------|--------|-----------|
| `Study.cs` (447 LOC) | 95% | 92% | P0 |
| `StudyInvitation.cs` (131 LOC) | 95% | 90% | P0 |
| `StudyMember.cs` (47 LOC) | 98% | 95% | P0 |
| `StudyPaper.cs` (91 LOC) | 95% | 90% | P1 |
| `StudyStar.cs` (28 LOC) | 100% | 100% | P2 |
| `StudyView.cs` (37 LOC) | 100% | 100% | P2 |
| `StudyTitle.cs` (42 LOC) | 100% | 100% | P1 |
| `StudyDescription.cs` (40 LOC) | 100% | 100% | P1 |
| `StudySettings.cs` (41 LOC) | 100% | 100% | P1 |
| `StudyMetrics.cs` (33 LOC) | 100% | 100% | P1 |
| `StudyStatus.cs` (45 LOC) | 100% | 100% | P0 |
| `StudyRole.cs` (30 LOC) | 100% | 100% | P0 |
| `InvitationStatus.cs` (25 LOC) | 100% | 100% | P0 |
| Command Handlers (Invitations) | 90% | 85% | P0 |
| Command Handlers (Members) | 90% | 85% | P0 |
| `StudyEndpoints.cs` | 85% | 80% | P0 |
| `StudyInvitationEndpoints.cs` | 85% | 80% | P0 |
| `StudyMemberEndpoints.cs` | 85% | 80% | P0 |

---

## 1. ANÁLISIS DEL MÓDULO

### 1.1 Archivos de Dominio (1,547 LOC)

| Archivo | LOC | Tests Existentes | Faltantes |
|---------|-----|------------------|-----------|
| `Study.cs` | 447 | ~45 | ~15 |
| `StudyErrors.cs` | 79 | 0 | 0 |
| `StudyId.cs` | 40 | ~5 | 0 |
| `StudyPaperId.cs` | 40 | 0 | 5 |
| `StudyInvitationId.cs` | 41 | 0 | 5 |
| `Entities/StudyInvitation.cs` | 131 | ~8 | ~10 |
| `Entities/StudyMember.cs` | 47 | ~5 | 0 |
| `Entities/StudyPaper.cs` | 91 | ~6 | ~8 |
| `Entities/StudyStar.cs` | 28 | ~4 | 0 |
| `Entities/StudyView.cs` | 37 | ~4 | 0 |
| `ValueObjects/StudyTitle.cs` | 42 | ~5 | 0 |
| `ValueObjects/StudyDescription.cs` | 40 | ~5 | 0 |
| `ValueObjects/StudyMetrics.cs` | 33 | ~5 | 0 |
| `ValueObjects/StudySettings.cs` | 41 | ~5 | 0 |
| `Enumerations/InvitationStatus.cs` | 25 | ~4 | 0 |
| `Enumerations/ResearchField.cs` | 26 | ~4 | 0 |
| `Enumerations/StudyRole.cs` | 30 | ~4 | 0 |
| `Enumerations/StudyStatus.cs` | 45 | ~4 | 0 |
| Events (14) | ~130 | 0 | 0 |

### 1.2 Archivos de Aplicación (~1,400 LOC)

**Commands con Tests:**
- CreateStudyCommandHandler ✓
- UpdateStudyCommandHandler ✓
- DeleteStudyCommandHandler ✓
- AddStudyMemberCommandHandler ✓
- StarStudyCommandHandler ✓

**Commands SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `AcceptInvitationCommandHandler` | ~60 | P0 |
| `SendInvitationCommandHandler` | ~80 | P0 |
| `CancelInvitationCommandHandler` | ~50 | P1 |
| `ResendInvitationCommandHandler` | ~50 | P1 |
| `ChangeMemberRoleCommandHandler` | ~50 | P1 |
| `RemoveStudyMemberCommandHandler` | ~50 | P1 |
| `LeaveStudyCommandHandler` | ~40 | P1 |
| `TransferOwnershipCommandHandler` | ~60 | P0 |
| `ChangeStudyStatusCommandHandler` | ~50 | P1 |
| `UpdateStudySettingsCommandHandler` | ~50 | P2 |
| `FeatureStudyCommandHandler` | ~40 | P2 |
| `UnstarStudyCommandHandler` | ~30 | P2 |
| `AddStudyPaperCommandHandler` | ~60 | P1 |
| `RemoveStudyPaperCommandHandler` | ~40 | P2 |
| `DuplicateStudyCommandHandler` | ~80 | P1 |

**Queries con Tests:**
- GetStudyByIdQueryHandler ✓
- GetUserStudiesQueryHandler ✓

**Queries SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `GetStudyMembersQueryHandler` | ~40 | P1 |
| `GetStudyInvitationsQueryHandler` | ~40 | P1 |
| `GetMyInvitationsQueryHandler` | ~40 | P1 |
| `GetStudyPapersQueryHandler` | ~40 | P2 |
| `GetStudyStatsQueryHandler` | ~50 | P2 |
| `IsStudyStarredQueryHandler` | ~25 | P2 |

---

## 2. TESTS EXISTENTES

### Domain Tests
- `StudyTests.cs` - ~45 tests
- `StudyIdTests.cs` - ~5 tests
- `StudyInvitationTests.cs` - ~8 tests
- `StudyMemberTests.cs` - ~5 tests
- `StudyPaperTests.cs` - ~6 tests
- `StudyStarTests.cs` - ~4 tests
- `StudyViewTests.cs` - ~4 tests
- `StudyTitleTests.cs` - ~5 tests
- `StudyDescriptionTests.cs` - ~5 tests
- `StudyMetricsTests.cs` - ~5 tests
- `StudySettingsTests.cs` - ~5 tests
- `InvitationStatusTests.cs` - ~4 tests
- `ResearchFieldTests.cs` - ~4 tests
- `StudyRoleTests.cs` - ~4 tests
- `StudyStatusTests.cs` - ~4 tests

### Handler Tests
- `CreateStudyCommandHandlerTests.cs` - ~6 tests
- `UpdateStudyCommandHandlerTests.cs` - ~5 tests
- `DeleteStudyCommandHandlerTests.cs` - ~5 tests
- `AddStudyMemberCommandHandlerTests.cs` - ~5 tests
- `StarStudyCommandHandlerTests.cs` - ~4 tests
- `GetStudyByIdQueryHandlerTests.cs` - ~5 tests
- `GetUserStudiesQueryHandlerTests.cs` - ~5 tests

**Total Existentes: ~144 tests**

---

## 3. TESTS UNITARIOS DE DOMINIO FALTANTES

### 3.1 StudyTests.cs - Tests Adicionales (15 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Duplicate_ShouldCreateCopyWithNewId` | Duplicar estudio |
| 2 | `Duplicate_ShouldCopyTitle` | Copiar título |
| 3 | `Duplicate_ShouldCopySettings` | Copiar configuración |
| 4 | `Duplicate_ShouldNotCopyMembers` | No copiar miembros |
| 5 | `Duplicate_ShouldNotCopyMetrics` | No copiar métricas |
| 6 | `SetInstitution_ValidValue_ShouldSet` | Establecer institución |
| 7 | `SetPrincipalInvestigator_ValidValue_ShouldSet` | Establecer PI |
| 8 | `UpdateSettings_ShouldUpdateSettings` | Actualizar settings |
| 9 | `UpdateSettings_ShouldRaiseEvent` | Evento de settings |
| 10 | `AddPaper_ByEditor_ShouldAddPaper` | Agregar paper |
| 11 | `AddPaper_ByViewer_ShouldReturnError` | Sin permisos |
| 12 | `RemovePaper_ExistingPaper_ShouldRemove` | Remover paper |
| 13 | `GetPaper_ById_ShouldReturnPaper` | Obtener paper |
| 14 | `HasPaper_ExistingDoi_ShouldReturnTrue` | Verificar paper existe |
| 15 | `CanUserAdmin_AdminRole_ShouldReturnTrue` | Verificar permisos admin |

### 3.2 StudyInvitationTests.cs - Tests Adicionales (10 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Accept_PendingInvitation_ShouldAccept` | Aceptar invitación |
| 2 | `Accept_ExpiredInvitation_ShouldReturnError` | Invitación expirada |
| 3 | `Accept_AlreadyAccepted_ShouldReturnError` | Ya aceptada |
| 4 | `Decline_PendingInvitation_ShouldDecline` | Rechazar invitación |
| 5 | `Cancel_PendingInvitation_ShouldCancel` | Cancelar invitación |
| 6 | `Resend_ShouldUpdateExpiresAt` | Reenviar actualiza expiración |
| 7 | `Resend_ShouldIncrementResendCount` | Incrementar contador |
| 8 | `Resend_MaxResends_ShouldReturnError` | Máximo reenvíos |
| 9 | `IsExpired_AfterExpiry_ShouldReturnTrue` | Verificar expiración |
| 10 | `CanBeResent_ValidState_ShouldReturnTrue` | Puede reenviarse |

### 3.3 StudyPaperTests.cs - Tests Adicionales (8 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithDoi_ShouldCreate` | Crear con DOI |
| 2 | `Create_WithPubMedId_ShouldCreate` | Crear con PubMed ID |
| 3 | `UpdateCitation_ShouldUpdateFields` | Actualizar cita |
| 4 | `UpdateCitation_ShouldSetModifiedAt` | Timestamp modificación |
| 5 | `ValidateDoi_ValidFormat_ShouldReturnTrue` | DOI válido |
| 6 | `ValidateDoi_InvalidFormat_ShouldReturnFalse` | DOI inválido |
| 7 | `Equality_SameDoi_ShouldBeEqual` | Igualdad por DOI |
| 8 | `SetAbstract_LongAbstract_ShouldTruncate` | Truncar abstract |

### 3.4 IDs Faltantes

#### StudyPaperIdTests.cs (5 tests)
#### StudyInvitationIdTests.cs (5 tests)

---

## 4. TESTS DE HANDLERS FALTANTES

### 4.1 Command Handler Tests

#### AcceptInvitationCommandHandlerTests.cs (8 tests)
```
Tests/Application/Studies/Commands/AcceptInvitationCommandHandlerTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ValidInvitation_ShouldAccept` | Aceptar exitosamente |
| 2 | `Handle_InvitationNotFound_ShouldReturnError` | No encontrada |
| 3 | `Handle_ExpiredInvitation_ShouldReturnError` | Expirada |
| 4 | `Handle_AlreadyAccepted_ShouldReturnError` | Ya aceptada |
| 5 | `Handle_UserAlreadyMember_ShouldReturnError` | Ya es miembro |
| 6 | `Handle_ShouldAddUserToStudy` | Agregar a estudio |
| 7 | `Handle_ShouldPersistChanges` | Persistir cambios |
| 8 | `Handle_ShouldSendNotification` | Enviar notificación |

#### SendInvitationCommandHandlerTests.cs (10 tests)
```
Tests/Application/Studies/Commands/SendInvitationCommandHandlerTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ValidData_ShouldSendInvitation` | Enviar exitosamente |
| 2 | `Handle_StudyNotFound_ShouldReturnError` | Estudio no encontrado |
| 3 | `Handle_UserNotAdmin_ShouldReturnPermissionError` | Sin permisos |
| 4 | `Handle_InvalidEmail_ShouldReturnValidationError` | Email inválido |
| 5 | `Handle_AlreadyMember_ShouldReturnError` | Ya es miembro |
| 6 | `Handle_PendingInvitation_ShouldReturnError` | Ya invitado |
| 7 | `Handle_ShouldSetExpirationDate` | Establecer expiración |
| 8 | `Handle_ShouldPersistInvitation` | Persistir invitación |
| 9 | `Handle_ShouldSendEmail` | Enviar email |
| 10 | `Handle_MaxMembersReached_ShouldReturnError` | Máximo miembros |

#### CancelInvitationCommandHandlerTests.cs (5 tests)
#### ResendInvitationCommandHandlerTests.cs (6 tests)
#### ChangeMemberRoleCommandHandlerTests.cs (7 tests)
#### RemoveStudyMemberCommandHandlerTests.cs (6 tests)
#### LeaveStudyCommandHandlerTests.cs (5 tests)
#### TransferOwnershipCommandHandlerTests.cs (7 tests)
#### ChangeStudyStatusCommandHandlerTests.cs (6 tests)
#### UpdateStudySettingsCommandHandlerTests.cs (5 tests)
#### FeatureStudyCommandHandlerTests.cs (5 tests)
#### UnstarStudyCommandHandlerTests.cs (4 tests)
#### AddStudyPaperCommandHandlerTests.cs (7 tests)
#### RemoveStudyPaperCommandHandlerTests.cs (5 tests)
#### DuplicateStudyCommandHandlerTests.cs (8 tests)

### 4.2 Query Handler Tests

#### GetStudyMembersQueryHandlerTests.cs (5 tests)
#### GetStudyInvitationsQueryHandlerTests.cs (5 tests)
#### GetMyInvitationsQueryHandlerTests.cs (5 tests)
#### GetStudyPapersQueryHandlerTests.cs (4 tests)
#### GetStudyStatsQueryHandlerTests.cs (5 tests)
#### IsStudyStarredQueryHandlerTests.cs (3 tests)

---

## 5. TESTS DE INTEGRACIÓN API

### 5.1 StudyEndpointsTests.cs (18 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetStudy_Authenticated_ShouldReturn200` | GET /studies/{id} | 200 | Obtener estudio |
| 2 | `GetStudy_NotMember_PrivateStudy_ShouldReturn403` | GET /studies/{id} | 403 | Sin acceso |
| 3 | `GetStudy_NotMember_PublicStudy_ShouldReturn200` | GET /studies/{id} | 200 | Estudio público |
| 4 | `GetUserStudies_ShouldReturn200` | GET /studies | 200 | Listar estudios |
| 5 | `CreateStudy_ValidData_ShouldReturn201` | POST /studies | 201 | Crear |
| 6 | `UpdateStudy_ByOwner_ShouldReturn200` | PUT /studies/{id} | 200 | Actualizar |
| 7 | `UpdateStudy_ByViewer_ShouldReturn403` | PUT /studies/{id} | 403 | Sin permisos |
| 8 | `DeleteStudy_ByOwner_ShouldReturn204` | DELETE /studies/{id} | 204 | Eliminar |
| 9 | `ChangeStatus_ValidTransition_ShouldReturn200` | POST /studies/{id}/status | 200 | Cambiar estado |
| 10 | `ChangeStatus_InvalidTransition_ShouldReturn400` | POST /studies/{id}/status | 400 | Transición inválida |
| 11 | `UpdateSettings_ShouldReturn200` | PUT /studies/{id}/settings | 200 | Actualizar settings |
| 12 | `GetStats_ShouldReturn200` | GET /studies/{id}/stats | 200 | Obtener estadísticas |
| 13 | `DuplicateStudy_ShouldReturn201` | POST /studies/{id}/duplicate | 201 | Duplicar |
| 14 | `FeatureStudy_ByAdmin_ShouldReturn200` | POST /studies/{id}/feature | 200 | Destacar |
| 15 | `FeatureStudy_ByNonAdmin_ShouldReturn403` | POST /studies/{id}/feature | 403 | Sin permisos |
| 16 | `StarStudy_ShouldReturn200` | POST /studies/{id}/star | 200 | Dar estrella |
| 17 | `UnstarStudy_ShouldReturn200` | DELETE /studies/{id}/star | 200 | Quitar estrella |
| 18 | `IsStarred_ShouldReturn200` | GET /studies/{id}/starred | 200 | Verificar estrella |

### 5.2 StudyMemberEndpointsTests.cs (12 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetMembers_ShouldReturn200` | GET /studies/{id}/members | 200 | Listar miembros |
| 2 | `AddMember_ByAdmin_ShouldReturn201` | POST /studies/{id}/members | 201 | Agregar |
| 3 | `AddMember_ByViewer_ShouldReturn403` | POST /studies/{id}/members | 403 | Sin permisos |
| 4 | `RemoveMember_ByAdmin_ShouldReturn204` | DELETE /studies/{id}/members/{userId} | 204 | Remover |
| 5 | `RemoveMember_Owner_ShouldReturn400` | DELETE /studies/{id}/members/{userId} | 400 | No puede remover owner |
| 6 | `ChangeMemberRole_ShouldReturn200` | PUT /studies/{id}/members/{userId}/role | 200 | Cambiar rol |
| 7 | `LeaveStudy_ByMember_ShouldReturn200` | POST /studies/{id}/leave | 200 | Abandonar |
| 8 | `LeaveStudy_ByOwner_ShouldReturn400` | POST /studies/{id}/leave | 400 | Owner no puede |
| 9 | `TransferOwnership_ByOwner_ShouldReturn200` | POST /studies/{id}/transfer | 200 | Transferir |
| 10 | `TransferOwnership_ByAdmin_ShouldReturn403` | POST /studies/{id}/transfer | 403 | Solo owner |
| 11 | `TransferOwnership_ToViewer_ShouldReturn400` | POST /studies/{id}/transfer | 400 | Debe ser admin |
| 12 | `GetMemberRole_ShouldReturn200` | GET /studies/{id}/members/me | 200 | Mi rol |

### 5.3 StudyInvitationEndpointsTests.cs (14 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetInvitations_ByAdmin_ShouldReturn200` | GET /studies/{id}/invitations | 200 | Listar invitaciones |
| 2 | `SendInvitation_ValidEmail_ShouldReturn201` | POST /studies/{id}/invitations | 201 | Enviar |
| 3 | `SendInvitation_InvalidEmail_ShouldReturn400` | POST /studies/{id}/invitations | 400 | Email inválido |
| 4 | `SendInvitation_AlreadyMember_ShouldReturn409` | POST /studies/{id}/invitations | 409 | Ya es miembro |
| 5 | `CancelInvitation_ShouldReturn204` | DELETE /studies/{id}/invitations/{invId} | 204 | Cancelar |
| 6 | `ResendInvitation_ShouldReturn200` | POST /studies/{id}/invitations/{invId}/resend | 200 | Reenviar |
| 7 | `ResendInvitation_MaxResends_ShouldReturn400` | POST /studies/{id}/invitations/{invId}/resend | 400 | Máximo alcanzado |
| 8 | `GetMyInvitations_ShouldReturn200` | GET /invitations | 200 | Mis invitaciones |
| 9 | `AcceptInvitation_Valid_ShouldReturn200` | POST /invitations/{invId}/accept | 200 | Aceptar |
| 10 | `AcceptInvitation_Expired_ShouldReturn400` | POST /invitations/{invId}/accept | 400 | Expirada |
| 11 | `DeclineInvitation_ShouldReturn200` | POST /invitations/{invId}/decline | 200 | Rechazar |
| 12 | `AcceptInvitation_ByToken_ShouldReturn200` | POST /invitations/accept?token={token} | 200 | Por token |
| 13 | `AcceptInvitation_InvalidToken_ShouldReturn400` | POST /invitations/accept?token={token} | 400 | Token inválido |
| 14 | `GetInvitationByToken_ShouldReturn200` | GET /invitations/token/{token} | 200 | Info por token |

### 5.4 StudyPaperEndpointsTests.cs (10 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetPapers_ShouldReturn200` | GET /studies/{id}/papers | 200 | Listar papers |
| 2 | `AddPaper_ValidDoi_ShouldReturn201` | POST /studies/{id}/papers | 201 | Agregar paper |
| 3 | `AddPaper_InvalidDoi_ShouldReturn400` | POST /studies/{id}/papers | 400 | DOI inválido |
| 4 | `AddPaper_DuplicateDoi_ShouldReturn409` | POST /studies/{id}/papers | 409 | DOI duplicado |
| 5 | `AddPaper_ByViewer_ShouldReturn403` | POST /studies/{id}/papers | 403 | Sin permisos |
| 6 | `RemovePaper_ByEditor_ShouldReturn204` | DELETE /studies/{id}/papers/{paperId} | 204 | Remover |
| 7 | `UpdatePaper_ShouldReturn200` | PUT /studies/{id}/papers/{paperId} | 200 | Actualizar |
| 8 | `GetPaper_ById_ShouldReturn200` | GET /studies/{id}/papers/{paperId} | 200 | Obtener |
| 9 | `ImportPapersFromDoi_ShouldReturn201` | POST /studies/{id}/papers/import | 201 | Importar |
| 10 | `SearchPapers_ShouldReturn200` | GET /studies/{id}/papers/search | 200 | Buscar |

---

## 6. TESTS E2E

### 6.1 StudyLifecycleE2ETests.cs (10 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `CreateStudy_AddMembers_ChangeStatus` | Ciclo de vida básico |
| 2 | `SendInvitation_Accept_BecomesMember` | Flujo de invitación |
| 3 | `SendInvitation_ExpiREDACTED` | Invitación expirada |
| 4 | `TransferOwnership_OldOwnerBecomesAdmin` | Transferencia exitosa |
| 5 | `ChangeMemberRoles_Permissions` | Cambio de roles |
| 6 | `LeaveStudy_LastAdmin_ShouldFail` | No puede dejar si es último admin |
| 7 | `DuplicateStudy_NewOwnerOnly` | Duplicar sin miembros |
| 8 | `StarStudy_UpdatesMetrics` | Estrella actualiza métricas |
| 9 | `PublishStudy_MakesPublic` | Publicar hace público |
| 10 | `ArchiveStudy_PreventEdits` | Archivar previene ediciones |

---

## 7. RESUMEN DE TESTS

| Categoría | Existentes | Nuevos | Total |
|-----------|------------|--------|-------|
| Domain - Study | 45 | 15 | 60 |
| Domain - Entities | 27 | 18 | 45 |
| Domain - VOs & Enums | 41 | 10 | 51 |
| **Subtotal Domain** | **113** | **43** | **156** |
| Handlers - Commands | 25 | 95 | 120 |
| Handlers - Queries | 10 | 27 | 37 |
| **Subtotal Handlers** | **35** | **122** | **157** |
| API - Endpoints | 0 | 54 | 54 |
| **Subtotal API** | **0** | **54** | **54** |
| E2E | 0 | 10 | 10 |
| **Subtotal E2E** | **0** | **10** | **10** |
| **TOTAL** | **~148** | **~229** | **~377** |

---

## 8. ARCHIVOS A CREAR

```
GeneFlow.ApiNet2.Tests/
├── Domain/
│   └── Studies/
│       └── (archivos existentes, agregar tests adicionales)
├── Application/
│   └── Studies/
│       ├── Commands/
│       │   ├── AcceptInvitationCommandHandlerTests.cs
│       │   ├── SendInvitationCommandHandlerTests.cs
│       │   ├── CancelInvitationCommandHandlerTests.cs
│       │   ├── ResendInvitationCommandHandlerTests.cs
│       │   ├── ChangeMemberRoleCommandHandlerTests.cs
│       │   ├── RemoveStudyMemberCommandHandlerTests.cs
│       │   ├── LeaveStudyCommandHandlerTests.cs
│       │   ├── TransferOwnershipCommandHandlerTests.cs
│       │   ├── ChangeStudyStatusCommandHandlerTests.cs
│       │   ├── UpdateStudySettingsCommandHandlerTests.cs
│       │   ├── FeatureStudyCommandHandlerTests.cs
│       │   ├── UnstarStudyCommandHandlerTests.cs
│       │   ├── AddStudyPaperCommandHandlerTests.cs
│       │   ├── RemoveStudyPaperCommandHandlerTests.cs
│       │   └── DuplicateStudyCommandHandlerTests.cs
│       └── Queries/
│           ├── GetStudyMembersQueryHandlerTests.cs
│           ├── GetStudyInvitationsQueryHandlerTests.cs
│           ├── GetMyInvitationsQueryHandlerTests.cs
│           ├── GetStudyPapersQueryHandlerTests.cs
│           ├── GetStudyStatsQueryHandlerTests.cs
│           └── IsStudyStarredQueryHandlerTests.cs
├── API/
│   └── Studies/
│       ├── StudyEndpointsTests.cs
│       ├── StudyMemberEndpointsTests.cs
│       ├── StudyInvitationEndpointsTests.cs
│       └── StudyPaperEndpointsTests.cs
└── E2E/
    └── StudyLifecycleE2ETests.cs
```

---

## 9. ORDEN DE IMPLEMENTACIÓN

1. **Día 1:** Tests adicionales de StudyTests.cs
2. **Día 2:** Tests adicionales de StudyInvitationTests.cs, StudyPaperTests.cs
3. **Día 3-4:** Command handlers de invitaciones
4. **Día 5-6:** Command handlers de miembros
5. **Día 7:** Command handlers restantes
6. **Día 8:** Query handlers
7. **Día 9-10:** API endpoint tests (Studies, Members)
8. **Día 11:** API endpoint tests (Invitations, Papers)
9. **Día 12:** E2E tests

**Tiempo estimado:** 2 semanas
