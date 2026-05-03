# Plan de Tests: E2E e Infraestructura

## Tests Estimados: ~170 tests

### Objetivos de Cobertura - Infrastructure Services

| Servicio | Line | Branch | Prioridad |
|----------|------|--------|-----------|
| `TwoFactorAuthenticator` | 80% | 75% | P0 |
| `StripeService` | 75% | 70% | P0 |
| `MinIOFileStorageService` | 75% | 70% | P0 |
| `TraceAnalysisService` | 80% | 75% | P0 |
| `RedisCacheService` | 75% | 70% | P1 |
| `EmailService` | 70% | 65% | P1 |
| `OAuthTokenValidator` | 75% | 70% | P0 |
| `ImageProcessingService` | 70% | 65% | P2 |

### Objetivos de Cobertura - Repositories

| Repository | Line | Branch | Prioridad |
|------------|------|--------|-----------|
| `UserRepository` | 80% | 75% | P0 |
| `StudyRepository` | 80% | 75% | P0 |
| `TraceRepository` | 80% | 75% | P0 |
| `PipelineRepository` | 80% | 75% | P0 |

### E2E Coverage Goals

Los tests E2E no se miden por cobertura de código, sino por:

| Métrica | Objetivo |
|---------|----------|
| **Flujos críticos cubiertos** | 100% |
| **Happy paths** | 100% |
| **Error paths principales** | ≥80% |
| **Edge cases** | ≥60% |

---

# PARTE 1: TESTS E2E GLOBALES

## 1. FLUJOS DE USUARIO COMPLETOS

### 1.1 AuthenticationFlowE2ETests.cs (12 tests)
*Documentado en 02_IDENTITY_TEST_PLAN.md*

### 1.2 StudyLifecycleE2ETests.cs (10 tests)
*Documentado en 03_STUDIES_TEST_PLAN.md*

### 1.3 TraceProcessingE2ETests.cs (12 tests)
*Documentado en 04_TRACES_TEST_PLAN.md*

### 1.4 PipelineLifecycleE2ETests.cs (8 tests)
*Documentado en 01_PIPELINES_TEST_PLAN.md*

### 1.5 SubscriptionFlowE2ETests.cs (6 tests)
*Documentado en 05_SUBSCRIPTIONS_TEST_PLAN.md*

### 1.6 CollaborationFlowE2ETests.cs (8 tests)

```
Tests/E2E/CollaborationFlowE2ETests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `InviteUser_AcceptInvitation_ViewStudy` | Flujo completo de invitación |
| 2 | `InviteUser_DeclineInvitation` | Rechazar invitación |
| 3 | `InviteUser_ExpireInvitation_CannotAccept` | Invitación expirada |
| 4 | `ChangeRole_UpdatesPermissions` | Cambio de rol afecta permisos |
| 5 | `RemoveMember_LosesAccess` | Miembro removido pierde acceso |
| 6 | `TransferOwnership_PreviousOwnerBecomesAdmin` | Transferir propiedad |
| 7 | `MultipleCollaborators_EditSameStudy` | Edición concurrente |
| 8 | `LeaveStudy_NotLastAdmin` | Abandonar estudio |

### 1.7 DataExportE2ETests.cs (6 tests)

```
Tests/E2E/DataExportE2ETests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `ExportStudy_AsZip_ShouldIncludeAllTraces` | Exportar estudio completo |
| 2 | `ExportTrace_AsFasta_ShouldExportSequence` | Exportar como FASTA |
| 3 | `ExportTrace_AsGenBank_ShouldIncludeAnnotations` | Exportar como GenBank |
| 4 | `ExportAnnotations_AsGFF3_ShouldExportAll` | Exportar anotaciones |
| 5 | `ExportMultipleTraces_Batch_ShouldWork` | Exportación por lotes |
| 6 | `ExportWithEdits_ShouldIncludeEditedSequence` | Exportar con ediciones |

---

# PARTE 2: TESTS DE INFRAESTRUCTURA

## 2. SERVICIOS DE INFRAESTRUCTURA

### 2.1 TwoFactorAuthenticatorTests.cs (10 tests)

```
Tests/Infrastructure/Identity/Services/TwoFactorAuthenticatorTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `GenerateSecret_ShouldReturnBase32Secret` | Generar secreto |
| 2 | `GenerateQrCodeUri_ShouldReturnValidUri` | Generar URI QR |
| 3 | `ValidateCode_ValidCode_ShouldReturnTrue` | Validar código TOTP |
| 4 | `ValidateCode_InvalidCode_ShouldReturnFalse` | Código inválido |
| 5 | `ValidateCode_ExpiredCode_ShouldReturnFalse` | Código expirado |
| 6 | `ValidateCode_ShouldAllowTimeSkew` | Permitir desfase tiempo |
| 7 | `GenerateRecoveryCodes_ShouldGenerate8Codes` | Generar códigos recuperación |
| 8 | `ValidateRecoveryCode_ValidCode_ShouldReturnTrue` | Validar código recuperación |
| 9 | `ValidateRecoveryCode_UsedCode_ShouldReturnFalse` | Código ya usado |
| 10 | `HashRecoveryCode_ShouldHashSecurely` | Hash seguro |

### 2.2 StripeServiceTests.cs (12 tests)

```
Tests/Infrastructure/Payments/Services/StripeServiceTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `CreateCustomer_ShouldReturnCustomerId` | Crear cliente Stripe |
| 2 | `CreatePaymentMethod_ValidCard_ShouldReturn` | Crear método de pago |
| 3 | `CreateSubscription_ShouldReturnSubscriptionId` | Crear suscripción |
| 4 | `CancelSubscription_ShouldCancel` | Cancelar suscripción |
| 5 | `UpdateSubscription_ChangePlan_ShouldUpdate` | Cambiar plan |
| 6 | `CreateInvoice_ShouldReturnInvoiceId` | Crear factura |
| 7 | `HandleWebhook_PaymentSucceeded_ShouldProcess` | Webhook pago exitoso |
| 8 | `HandleWebhook_PaymentFailed_ShouldProcess` | Webhook pago fallido |
| 9 | `HandleWebhook_SubscriptionCancelled_ShouldProcess` | Webhook cancelación |
| 10 | `GetUpcomingInvoice_ShouldReturnInvoice` | Próxima factura |
| 11 | `RefundPayment_ShouldRefund` | Reembolsar pago |
| 12 | `ValidateWebhookSignatuREDACTED` | Validar firma webhook |

### 2.3 MinIOFileStorageServiceTests.cs (10 tests)

```
Tests/Infrastructure/Storage/Services/MinIOFileStorageServiceTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `UploadFile_ShouldStoreFile` | Subir archivo |
| 2 | `DownloadFile_ExistingFile_ShouldReturn` | Descargar archivo |
| 3 | `DownloadFile_NotFound_ShouldReturnNull` | Archivo no existe |
| 4 | `DeleteFile_ExistingFile_ShouldDelete` | Eliminar archivo |
| 5 | `FileExists_ExistingFile_ShouldReturnTrue` | Verificar existencia |
| 6 | `GetFileMetadata_ShouldReturnMetadata` | Obtener metadata |
| 7 | `GeneratePresignedUrl_ShouldReturnValidUrl` | URL prefirmada |
| 8 | `ListFiles_ShouldReturnFileList` | Listar archivos |
| 9 | `CopyFile_ShouldCopyFile` | Copiar archivo |
| 10 | `GetStorageUsed_ShouldCalculateSize` | Calcular tamaño |

### 2.4 TraceAnalysisServiceTests.cs (10 tests)

```
Tests/Infrastructure/Traces/Services/TraceAnalysisServiceTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `ParseAb1File_ValidFile_ShouldParseSequence` | Parsear AB1 |
| 2 | `ParseAb1File_InvalidFile_ShouldReturnError` | AB1 inválido |
| 3 | `ParseScfFile_ValidFile_ShouldParseSequence` | Parsear SCF |
| 4 | `CalculateQualityMetrics_ShouldCalculate` | Calcular métricas |
| 5 | `CalculateAutoTrimPositions_ShouldReturn` | Calcular posiciones trim |
| 6 | `ExtractChromatogramData_ShouldExtract` | Extraer cromatograma |
| 7 | `FindMotifs_ShouldFindMatches` | Buscar motivos |
| 8 | `FindRestrictionSites_ShouldFindSites` | Buscar sitios restricción |
| 9 | `TranslateSequence_ShouldTranslate` | Traducir secuencia |
| 10 | `GetReverseComplement_ShouldReturn` | Complemento reverso |

### 2.5 RedisCacheServiceTests.cs (8 tests)

```
Tests/Infrastructure/Redis/RedisCacheServiceTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `SetAsync_ShouldStoreValue` | Almacenar valor |
| 2 | `GetAsync_ExistingKey_ShouldReturnValue` | Obtener valor |
| 3 | `GetAsync_NonExistingKey_ShouldReturnNull` | Clave no existe |
| 4 | `RemoveAsync_ShouldRemoveValue` | Eliminar valor |
| 5 | `ExistsAsync_ExistingKey_ShouldReturnTrue` | Verificar existencia |
| 6 | `SetAsync_WithExpiration_ShouldExpire` | Con expiración |
| 7 | `GetOrSetAsync_NotCached_ShouldCallFactory` | Cache miss |
| 8 | `GetOrSetAsync_Cached_ShouldReturnCached` | Cache hit |

### 2.6 EmailServiceTests.cs (8 tests)

```
Tests/Infrastructure/Email/EmailServiceTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `SendVerificationEmail_ShouldSend` | Email verificación |
| 2 | `SendPasswordResetEmail_ShouldSend` | Email reset password |
| 3 | `SendInvitationEmail_ShouldSend` | Email invitación |
| 4 | `SendWelcomeEmail_ShouldSend` | Email bienvenida |
| 5 | `SendSubscriptionConfirmation_ShouldSend` | Email confirmación sub |
| 6 | `SendEmail_InvalidAddress_ShouldReturnError` | Email inválido |
| 7 | `SendEmail_ShouldUseTemplate` | Usar plantilla |
| 8 | `SendEmail_ShouldRetryOnFailure` | Reintentar en fallo |

### 2.7 OAuthTokenValidatorTests.cs (8 tests)

```
Tests/Infrastructure/Identity/Services/OAuthTokenValidatorTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `ValidateGitHubToken_ValidToken_ShouldReturnUserInfo` | GitHub válido |
| 2 | `ValidateGitHubToken_InvalidToken_ShouldReturnError` | GitHub inválido |
| 3 | `ValidateGoogleToken_ValidToken_ShouldReturnUserInfo` | Google válido |
| 4 | `ValidateGoogleToken_InvalidToken_ShouldReturnError` | Google inválido |
| 5 | `ExchangeCodeForToken_GitHub_ShouldReturnToken` | Exchange code GitHub |
| 6 | `ExchangeCodeForToken_Google_ShouldReturnToken` | Exchange code Google |
| 7 | `RefreshOAuthToken_ShouldRefresh` | Refrescar token |
| 8 | `RevokeOAuthToken_ShouldRevoke` | Revocar token |

### 2.8 ImageProcessingServiceTests.cs (6 tests)

```
Tests/Infrastructure/Images/ImageProcessingServiceTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `ResizeImage_ShouldResizeToTarget` | Redimensionar |
| 2 | `CropImage_ShouldCropCorrectly` | Recortar |
| 3 | `ConvertToWebP_ShouldConvert` | Convertir a WebP |
| 4 | `ValidateImage_ValidFormat_ShouldReturnTrue` | Formato válido |
| 5 | `ValidateImage_InvalidFormat_ShouldReturnFalse` | Formato inválido |
| 6 | `GenerateThumbnail_ShouldGenerate` | Generar thumbnail |

---

# PARTE 3: TESTS DE REPOSITORIOS

## 3. REPOSITORY INTEGRATION TESTS

### 3.1 UserRepositoryTests.cs (8 tests)

```
Tests/Infrastructure/Identity/Repositories/UserRepositoryTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `GetByIdAsync_ExistingUser_ShouldReturn` | Obtener por ID |
| 2 | `GetByIdAsync_NonExisting_ShouldReturnNull` | No existe |
| 3 | `GetByEmailAsync_ShouldReturn` | Obtener por email |
| 4 | `GetByUsernameAsync_ShouldReturn` | Obtener por username |
| 5 | `AddAsync_ShouldPersist` | Agregar |
| 6 | `UpdateAsync_ShouldUpdate` | Actualizar |
| 7 | `ExistsByEmailAsync_ShouldReturnTrue` | Email existe |
| 8 | `ExistsByUsernameAsync_ShouldReturnTrue` | Username existe |

### 3.2 StudyRepositoryTests.cs (10 tests)

```
Tests/Infrastructure/Studies/Repositories/StudyRepositoryTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `GetByIdAsync_WithMembers_ShouldIncludeMembers` | Incluir miembros |
| 2 | `GetByIdAsync_WithTraces_ShouldIncludeTraces` | Incluir traces |
| 3 | `GetUserStudiesAsync_ShouldReturnUserStudies` | Estudios del usuario |
| 4 | `GetUserStudiesAsync_WithPagination_ShouldPaginate` | Con paginación |
| 5 | `GetPublicStudiesAsync_ShouldReturnPublicOnly` | Solo públicos |
| 6 | `GetFeaturedStudiesAsync_ShouldReturnFeatured` | Solo destacados |
| 7 | `SearchStudiesAsync_ShouldSearchByTitle` | Buscar por título |
| 8 | `GetStudyStatsAsync_ShouldReturnStats` | Estadísticas |
| 9 | `IsUserMemberAsync_ShouldReturnCorrectly` | Es miembro |
| 10 | `GetUserRoleAsync_ShouldReturnRole` | Obtener rol |

### 3.3 TraceRepositoryTests.cs (10 tests)

```
Tests/Infrastructure/Traces/Repositories/TraceRepositoryTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `GetByIdAsync_WithAnnotations_ShouldInclude` | Incluir anotaciones |
| 2 | `GetByIdAsync_WithEdits_ShouldInclude` | Incluir ediciones |
| 3 | `GetStudyTracesAsync_ShouldReturnStudyTraces` | Traces del estudio |
| 4 | `GetStudyTracesAsync_FilterByStatus_ShouldFilter` | Filtrar por estado |
| 5 | `GetTraceCountsByStatusAsync_ShouldReturnCounts` | Contar por estado |
| 6 | `GetPendingProcessingAsync_ShouldReturnPending` | Pendientes procesamiento |
| 7 | `SearchTracesAsync_ShouldSearchByName` | Buscar por nombre |
| 8 | `GetAnnotationsAsync_ShouldReturnAnnotations` | Obtener anotaciones |
| 9 | `GetSequenceEditsAsync_ShouldReturnEdits` | Obtener ediciones |
| 10 | `GetTotalStorageUsedAsync_ShouldCalculate` | Calcular almacenamiento |

### 3.4 PipelineRepositoryTests.cs (8 tests)

```
Tests/Infrastructure/Pipelines/Repositories/PipelineRepositoryTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `GetByIdAsync_WithSteps_ShouldIncludeSteps` | Incluir steps |
| 2 | `GetStudyPipelinesAsync_ShouldReturn` | Pipelines del estudio |
| 3 | `GetStudyPipelinesAsync_FilterByStatus_ShouldFilter` | Filtrar por estado |
| 4 | `GetActivePipelinesAsync_ShouldReturnActive` | Solo activos |
| 5 | `GetByIdWithExecutionsAsync_ShouldInclude` | Incluir ejecuciones |
| 6 | `GetExecutionByIdAsync_ShouldReturn` | Obtener ejecución |
| 7 | `GetTraceExecutionsAsync_ShouldReturn` | Ejecuciones del trace |
| 8 | `GetRunningExecutionsAsync_ShouldReturn` | Ejecuciones corriendo |

---

## 4. RESUMEN DE TESTS

| Categoría | Tests |
|-----------|-------|
| E2E - Collaboration | 8 |
| E2E - DataExport | 6 |
| E2E - (otros módulos) | 48 |
| **Subtotal E2E** | **62** |
| Infrastructure - TwoFactorAuth | 10 |
| Infrastructure - Stripe | 12 |
| Infrastructure - MinIO | 10 |
| Infrastructure - TraceAnalysis | 10 |
| Infrastructure - Redis | 8 |
| Infrastructure - Email | 8 |
| Infrastructure - OAuth | 8 |
| Infrastructure - Image | 6 |
| **Subtotal Infrastructure** | **72** |
| Repository - User | 8 |
| Repository - Study | 10 |
| Repository - Trace | 10 |
| Repository - Pipeline | 8 |
| **Subtotal Repository** | **36** |
| **TOTAL** | **~170** |

---

## 5. ORDEN DE IMPLEMENTACIÓN

### Semana 1: E2E Tests
1. **Día 1:** CollaborationFlowE2ETests
2. **Día 2:** DataExportE2ETests

### Semana 2: Infrastructure Services
3. **Día 3:** TwoFactorAuthenticatorTests
4. **Día 4:** StripeServiceTests
5. **Día 5:** MinIOFileStorageServiceTests
6. **Día 6:** TraceAnalysisServiceTests
7. **Día 7:** RedisCacheServiceTests, EmailServiceTests

### Semana 3: Repository Tests
8. **Día 8:** UserRepositoryTests, StudyRepositoryTests
9. **Día 9:** TraceRepositoryTests, PipelineRepositoryTests
10. **Día 10:** OAuthTokenValidatorTests, ImageProcessingServiceTests

**Tiempo estimado:** 2 semanas
