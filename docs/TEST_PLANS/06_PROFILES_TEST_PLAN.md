# Plan de Tests: Módulo Profiles

## Cobertura Actual: ~50%
## Tests Estimados: ~139 tests

### Objetivos de Cobertura por Capa

| Capa | Archivos | Line | Branch | Method |
|------|----------|------|--------|--------|
| **Domain** | Profile.cs, ValueObjects/*, Enumerations/* | **≥95%** | **≥90%** | **≥98%** |
| **Application** | Commands/*, Queries/* | **≥90%** | **≥85%** | **≥95%** |
| **Infrastructure** | Repositories/*, ImageProcessingService | **≥75%** | **≥70%** | **≥85%** |
| **API** | Endpoints/* | **≥85%** | **≥80%** | **≥90%** |

### Objetivos Específicos por Archivo

| Archivo | Line | Branch | Prioridad |
|---------|------|--------|-----------|
| `Profile.cs` (~200 LOC) | 95% | 92% | P0 |
| `Bio.cs` (~40 LOC) | 100% | 100% | P1 |
| `PersonName.cs` (~50 LOC) | 100% | 100% | P1 |
| `ResearchIdentifiers.cs` (~60 LOC) | 100% | 100% | P1 |
| `Institution.cs` (~40 LOC) | 100% | 100% | P1 |
| `Location.cs` (~40 LOC) | 100% | 100% | P1 |
| `ProfessionalRole.cs` (~40 LOC) | 100% | 100% | P1 |
| `ResearchField.cs` (~30 LOC) | 100% | 100% | P1 |
| Command Handlers | 90% | 85% | P0 |
| Query Handlers | 85% | 80% | P1 |
| `ProfileEndpoints.cs` | 85% | 80% | P0 |

---

## 1. ANÁLISIS DEL MÓDULO

### 1.1 Archivos de Dominio (~500 LOC)

| Archivo | LOC | Tests Existentes | Faltantes |
|---------|-----|------------------|-----------|
| `Profile.cs` | ~200 | ~10 | ~10 |
| `ProfileId.cs` | ~40 | ~5 | 0 |
| `ProfileErrors.cs` | ~40 | 0 | 0 |
| `ValueObjects/Bio.cs` | ~40 | ~5 | 3 |
| `ValueObjects/PersonName.cs` | ~50 | ~5 | 3 |
| `ValueObjects/ResearchIdentifiers.cs` | ~60 | ~5 | 5 |
| `ValueObjects/Institution.cs` | ~40 | 0 | 6 |
| `ValueObjects/Location.cs` | ~40 | 0 | 6 |
| `ValueObjects/ProfessionalRole.cs` | ~40 | 0 | 6 |
| `Enumerations/ResearchField.cs` | ~30 | ~4 | 0 |

### 1.2 Archivos de Aplicación (~400 LOC)

**Commands con Tests:**
- CreateProfileCommandHandler ✓
- UpdateProfileCommandHandler ✓
- UpdateProfilePhotoCommandHandler ✓
- DeleteProfilePhotoCommandHandler ✓
- UpdateResearchIdentifiersCommandHandler ✓

**Commands SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `UpdateSocialLinksCommandHandler` | ~40 | P2 |
| `UpdatePrivacySettingsCommandHandler` | ~40 | P2 |

**Queries con Tests:**
- GetCurrentUserProfileQueryHandler ✓
- GetProfileByUserIdQueryHandler ✓
- GetProfilesByUserIdsQueryHandler ✓

**Queries SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `GetProfileStatsQueryHandler` | ~50 | P1 |
| `SearchProfilesQueryHandler` | ~60 | P2 |

---

## 2. TESTS EXISTENTES

### Domain Tests
- `ProfileTests.cs` - ~10 tests
- `ProfileIdTests.cs` - ~5 tests
- `BioTests.cs` - ~5 tests
- `PersonNameTests.cs` - ~5 tests
- `ResearchIdentifiersTests.cs` - ~5 tests
- `ResearchFieldTests.cs` - ~4 tests

### Handler Tests
- `CreateProfileCommandHandlerTests.cs` - ~5 tests
- `UpdateProfileCommandHandlerTests.cs` - ~5 tests
- `UpdateProfilePhotoCommandHandlerTests.cs` - ~4 tests
- `DeleteProfilePhotoCommandHandlerTests.cs` - ~3 tests
- `UpdateResearchIdentifiersCommandHandlerTests.cs` - ~4 tests
- `GetCurrentUserProfileQueryHandlerTests.cs` - ~4 tests
- `GetProfileByUserIdQueryHandlerTests.cs` - ~4 tests
- `GetProfilesByUserIdsQueryHandlerTests.cs` - ~4 tests

**Total Existentes: ~67 tests**

---

## 3. TESTS UNITARIOS DE DOMINIO FALTANTES

### 3.1 ProfileTests.cs - Tests Adicionales (10 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `SetInstitution_ValidValue_ShouldSet` | Establecer institución |
| 2 | `SetLocation_ValidValue_ShouldSet` | Establecer ubicación |
| 3 | `SetProfessionalRole_ValidValue_ShouldSet` | Establecer rol |
| 4 | `AddSocialLink_ValidLink_ShouldAdd` | Agregar red social |
| 5 | `RemoveSocialLink_ExistingLink_ShouldRemove` | Remover red social |
| 6 | `UpdatePrivacySettings_ShouldUpdate` | Actualizar privacidad |
| 7 | `IsProfileComplete_AllFieldsSet_ShouldReturnTrue` | Perfil completo |
| 8 | `GetCompletionPercentage_ShouldCalculate` | Porcentaje completado |
| 9 | `SetWebsite_ValidUrl_ShouldSet` | Establecer website |
| 10 | `SetWebsite_InvalidUrl_ShouldReturnError` | URL inválida |

### 3.2 Value Objects Faltantes

#### InstitutionTests.cs (6 tests)
```
Tests/Domain/Profiles/ValueObjects/InstitutionTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidName_ShouldCreate` | Crear institución |
| 2 | `Create_TooLong_ShouldReturnError` | Nombre muy largo |
| 3 | `Create_WithDepartment_ShouldSetDepartment` | Con departamento |
| 4 | `Create_ShouldTrimWhitespace` | Trimear espacios |
| 5 | `Empty_ShouldReturnNullValue` | Valor vacío |
| 6 | `Equality_SameName_ShouldBeEqual` | Igualdad |

#### LocationTests.cs (6 tests)
```
Tests/Domain/Profiles/ValueObjects/LocationTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidData_ShouldCreate` | Crear ubicación |
| 2 | `Create_WithCityAndCountry_ShouldCreate` | Ciudad y país |
| 3 | `Create_TooLong_ShouldReturnError` | Muy largo |
| 4 | `GetDisplayString_ShouldFormat` | Formato display |
| 5 | `Empty_ShouldReturnNullValue` | Valor vacío |
| 6 | `Equality_SameLocation_ShouldBeEqual` | Igualdad |

#### ProfessionalRoleTests.cs (6 tests)
```
Tests/Domain/Profiles/ValueObjects/ProfessionalRoleTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidTitle_ShouldCreate` | Crear rol |
| 2 | `Create_TooLong_ShouldReturnError` | Título muy largo |
| 3 | `Create_ShouldTrimWhitespace` | Trimear espacios |
| 4 | `Empty_ShouldReturnNullValue` | Valor vacío |
| 5 | `Common_ResearchAssociate_ShouldBeValid` | Rol común |
| 6 | `Equality_SameTitle_ShouldBeEqual` | Igualdad |

### 3.3 Existing VOs - Tests Adicionales

#### BioTests.cs - (3 tests adicionales)
#### PersonNameTests.cs - (3 tests adicionales)
#### ResearchIdentifiersTests.cs - (5 tests adicionales)

---

## 4. TESTS DE HANDLERS FALTANTES

### 4.1 Command Handler Tests

#### UpdateSocialLinksCommandHandlerTests.cs (5 tests)
#### UpdatePrivacySettingsCommandHandlerTests.cs (5 tests)

### 4.2 Query Handler Tests

#### GetProfileStatsQueryHandlerTests.cs (5 tests)
#### SearchProfilesQueryHandlerTests.cs (6 tests)

---

## 5. TESTS DE INTEGRACIÓN API

### 5.1 ProfileEndpointsTests.cs (12 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetCurrentProfile_ShouldReturn200` | GET /profiles/me | 200 | Mi perfil |
| 2 | `GetProfile_ById_ShouldReturn200` | GET /profiles/{id} | 200 | Por ID |
| 3 | `GetProfile_NotFound_ShouldReturn404` | GET /profiles/{id} | 404 | No encontrado |
| 4 | `CreateProfile_ShouldReturn201` | POST /profiles | 201 | Crear |
| 5 | `UpdateProfile_ShouldReturn200` | PUT /profiles/me | 200 | Actualizar |
| 6 | `UpdatePhoto_ValidImage_ShouldReturn200` | PUT /profiles/me/photo | 200 | Actualizar foto |
| 7 | `UpdatePhoto_InvalidFormat_ShouldReturn400` | PUT /profiles/me/photo | 400 | Formato inválido |
| 8 | `DeletePhoto_ShouldReturn204` | DELETE /profiles/me/photo | 204 | Eliminar foto |
| 9 | `UpdateResearchIds_ShouldReturn200` | PUT /profiles/me/research-ids | 200 | IDs investigación |
| 10 | `UpdateSocialLinks_ShouldReturn200` | PUT /profiles/me/social-links | 200 | Redes sociales |
| 11 | `GetProfileStats_ShouldReturn200` | GET /profiles/me/stats | 200 | Estadísticas |
| 12 | `SearchProfiles_ShouldReturn200` | GET /profiles/search | 200 | Buscar |

---

## 6. RESUMEN DE TESTS

| Categoría | Existentes | Nuevos | Total |
|-----------|------------|--------|-------|
| Domain | 34 | 39 | 73 |
| Handlers | 33 | 21 | 54 |
| API | 0 | 12 | 12 |
| **TOTAL** | **~67** | **~72** | **~139** |

---

## 7. ORDEN DE IMPLEMENTACIÓN

1. **Día 1:** Value Objects faltantes (Institution, Location, ProfessionalRole)
2. **Día 2:** ProfileTests adicionales, VOs adicionales
3. **Día 3:** Command/Query handlers
4. **Día 4:** API tests

**Tiempo estimado:** 4 días
