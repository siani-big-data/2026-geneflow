# Módulo de Perfiles de Usuario

## Descripción General

El módulo **Profiles** gestiona la información personal y profesional de los usuarios de GeneFlow. Es un bounded context separado de **Identity** (autenticación), permitiendo una clara separación de responsabilidades.

---

## Arquitectura

### Relación con Identity

```
┌─────────────────┐         ┌─────────────────┐
│    Identity     │         │    Profiles     │
│  (Autenticación)│         │  (Info Personal)│
├─────────────────┤         ├─────────────────┤
│ - User          │◄───────►│ - Profile       │
│ - Credentials   │  UserId │ - Name          │
│ - 2FA           │         │ - Bio           │
│ - OAuth         │         │ - Institution   │
└─────────────────┘         └─────────────────┘
```

- **Profile** referencia a **User** mediante `UserId` (relación 1:1)
- Un Profile se crea automáticamente al registrar un usuario
- ProfileId usa el formato `P00000001` (prefijo P + 8 dígitos)

---

## Modelo de Dominio

### Aggregate Root: Profile

```csharp
public sealed class Profile : FullAuditableAggregateRoot<ProfileId>
{
    public ProfileId Id { get; }
    public UserId UserId { get; }
    public PersonName Name { get; }
    public Bio? Bio { get; }
    public Location? Location { get; }
    public ProfessionalRole? ProfessionalRole { get; }
    public Institution? Institution { get; }
    public ResearchField? ResearchField { get; }
    public ResearchIdentifiers? ResearchIdentifiers { get; }
    public ProfilePhoto? Photo { get; }

    // Computed
    public string FullName { get; }
    public string Initials { get; }
    public bool IsComplete { get; }
}
```

### Value Objects

| Value Object | Campos | Validaciones |
|--------------|--------|--------------|
| `PersonName` | FirstName, LastName | FirstName: 1-100 chars, requerido. LastName: 0-100 chars |
| `Bio` | Value | Máx 500 caracteres |
| `Location` | Value | Máx 200 caracteres |
| `ProfessionalRole` | Value | Máx 100 caracteres |
| `Institution` | Name, Department | Name: 200 chars, Department: 200 chars |
| `ResearchIdentifiers` | OrcidId, Website | ORCID: formato `0000-0000-0000-000X`, Website: URL válida |
| `ProfilePhoto` | Url, ThumbnailUrl, SizeBytes | URLs: 1000 chars, Size: máx 10MB |

### Enumerations

**ResearchField** (Smart Enumeration):
- Genomics, Proteomics, Transcriptomics, Bioinformatics
- MolecularBiology, CellBiology, Genetics, Biochemistry
- Microbiology, Immunology, Neuroscience, PlantBiology
- MarineBiology, Ecology, EvolutionaryBiology
- ComputationalBiology, SyntheticBiology, Other

---

## Base de Datos

### Schema: `profiles`

```sql
CREATE TABLE profiles.profiles (
    id                     VARCHAR(10) PRIMARY KEY,
    user_id                VARCHAR(10) NOT NULL UNIQUE,
    first_name             VARCHAR(100) NOT NULL,
    last_name              VARCHAR(100),
    bio                    VARCHAR(500),
    location               VARCHAR(200),
    professional_role      VARCHAR(100),
    institution_name       VARCHAR(200),
    institution_department VARCHAR(200),
    research_field         VARCHAR(50),
    orcid_id               VARCHAR(19),
    website                VARCHAR(500),
    photo_url              VARCHAR(1000),
    photo_thumbnail_url    VARCHAR(1000),
    photo_size_bytes       BIGINT,
    created_at             TIMESTAMP WITH TIME ZONE NOT NULL,
    created_by             VARCHAR(100),
    modified_at            TIMESTAMP WITH TIME ZONE,
    modified_by            VARCHAR(100),
    is_deleted             BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at             TIMESTAMP WITH TIME ZONE,
    deleted_by             VARCHAR(100)
);

CREATE INDEX IX_profiles_research_field ON profiles.profiles(research_field);
CREATE UNIQUE INDEX IX_profiles_user_id ON profiles.profiles(user_id);
```

---

## API Endpoints

### Base URL: `/api/v1/profiles`

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/me` | ✓ | Obtener perfil del usuario actual |
| GET | `/me/stats` | ✓ | Obtener estadísticas del perfil |
| PUT | `/me` | ✓ | Actualizar información básica |
| PUT | `/me/research-identifiers` | ✓ | Actualizar ORCID y website |
| PUT | `/me/photo` | ✓ | Actualizar URL de foto |
| POST | `/me/photo/upload` | ✓ | Subir foto (multipart/form-data) |
| DELETE | `/me/photo` | ✓ | Eliminar foto |
| GET | `/{userId}` | - | Obtener perfil por userId |
| POST | `/batch` | - | Obtener múltiples perfiles |

### Ejemplos de Request/Response

#### GET /api/v1/profiles/me

**Response 200:**
```json
{
  "id": "P00000001",
  "userId": "U00000001",
  "firstName": "Sarah",
  "lastName": "Martinez",
  "fullName": "Sarah Martinez",
  "initials": "SM",
  "bio": "Investigadora en genómica computacional",
  "location": "Madrid, España",
  "professionalRole": "Senior Researcher",
  "institutionName": "Universidad Complutense",
  "institutionDepartment": "Departamento de Biología",
  "institutionDisplayName": "Universidad Complutense - Departamento de Biología",
  "researchField": "Genomics",
  "orcidId": "0000-0002-1234-5678",
  "orcidUrl": "https://orcid.org/0000-0002-1234-5678",
  "website": "https://sarahmartinez.com",
  "photoUrl": "/storage/profiles/1/photo.jpg",
  "photoThumbnailUrl": "/storage/profiles/1/thumbnail.jpg",
  "isComplete": true,
  "createdAt": "2024-01-15T10:00:00Z",
  "modifiedAt": "2024-03-20T14:30:00Z"
}
```

#### PUT /api/v1/profiles/me

**Request:**
```json
{
  "firstName": "Sarah",
  "lastName": "Martinez",
  "bio": "Investigadora en genómica computacional con 10 años de experiencia",
  "location": "Madrid, España",
  "professionalRole": "Principal Investigator",
  "institutionName": "Universidad Complutense",
  "institutionDepartment": "Departamento de Biología Molecular",
  "researchField": "Genomics"
}
```

#### PUT /api/v1/profiles/me/research-identifiers

**Request:**
```json
{
  "orcidId": "0000-0002-1234-5678",
  "website": "https://sarahmartinez.com"
}
```

#### POST /api/v1/profiles/me/photo/upload

**Request:** `multipart/form-data`
- `file`: Archivo de imagen (JPEG, PNG, GIF, WebP)
- Tamaño máximo: 10MB

**Response 200:**
```json
{
  "id": "P00000001",
  "photoUrl": "/storage/profiles/1/photo.jpg",
  "photoThumbnailUrl": "/storage/profiles/1/thumbnail.jpg"
  // ... resto del perfil
}
```

#### GET /api/v1/profiles/me/stats

**Response 200:**
```json
{
  "totalStudies": 5,
  "ownedStudies": 3,
  "totalTraces": 150,
  "totalAlignments": 25,
  "completedAlignments": 20,
  "lastActivityAt": "2024-03-20T14:30:00Z",
  "memberSince": "2024-01-15T10:00:00Z"
}
```

#### POST /api/v1/profiles/batch

**Request:**
```json
{
  "userIds": ["U00000001", "U00000002", "U00000003"]
}
```

**Response 200:**
```json
[
  {
    "userId": "U00000001",
    "firstName": "Sarah",
    "lastName": "Martinez",
    "fullName": "Sarah Martinez",
    "initials": "SM",
    "photoThumbnailUrl": "/storage/profiles/1/thumbnail.jpg"
  },
  // ...
]
```

---

## Eventos de Dominio

| Evento | Trigger | Payload |
|--------|---------|---------|
| `ProfileCreatedEvent` | Crear perfil | ProfileId |
| `ProfileUpdatedEvent` | Actualizar perfil | ProfileId |
| `ProfilePhotoUpdatedEvent` | Cambiar foto | ProfileId |
| `ProfilePhotoUploadedEvent` | Subir foto | ProfileId, PhotoData, Extension, ContentType, SizeBytes |
| `ProfilePhotoDeletedEvent` | Eliminar foto | ProfileId |

### Flujo de Foto de Perfil

```
┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐
│ Frontend│───►│   API   │───►│  Redis  │───►│Datalake │
│         │    │         │    │ Stream  │    │ Storage │
└─────────┘    └─────────┘    └─────────┘    └─────────┘
     │              │              │              │
     │  1. Upload   │              │              │
     │  (base64)    │              │              │
     │─────────────►│              │              │
     │              │ 2. Publish   │              │
     │              │    Event     │              │
     │              │─────────────►│              │
     │              │              │ 3. Store     │
     │              │              │    Binary    │
     │              │              │─────────────►│
     │              │              │              │
     │  4. Return   │              │              │
     │  URLs        │              │              │
     │◄─────────────│              │              │
```

---

## Comandos y Queries

### Commands

| Command | Descripción | Retorna |
|---------|-------------|---------|
| `CreateProfileCommand` | Crear perfil (interno) | `Result<ProfileDto>` |
| `UpdateProfileCommand` | Actualizar info básica | `Result<ProfileDto>` |
| `UpdateResearchIdentifiersCommand` | Actualizar ORCID/Website | `Result<ProfileDto>` |
| `UpdateProfilePhotoCommand` | Actualizar URLs de foto | `Result<ProfileDto>` |
| `UploadProfilePhotoCommand` | Subir foto (base64) | `Result<ProfileDto>` |
| `DeleteProfilePhotoCommand` | Eliminar foto | `Result` |

### Queries

| Query | Descripción | Retorna |
|-------|-------------|---------|
| `GetCurrentUserProfileQuery` | Perfil del usuario actual | `Result<ProfileDto>` |
| `GetProfileByUserIdQuery` | Perfil por userId | `Result<ProfileDto>` |
| `GetProfileStatsQuery` | Estadísticas del perfil | `Result<ProfileStatsDto>` |
| `GetProfilesByUserIdsQuery` | Múltiples perfiles | `Result<IReadOnlyList<ProfileSummaryDto>>` |

---

## DTOs

### ProfileDto
```csharp
public sealed record ProfileDto(
    string Id,
    string UserId,
    string FirstName,
    string? LastName,
    string FullName,
    string Initials,
    string? Bio,
    string? Location,
    string? ProfessionalRole,
    string? InstitutionName,
    string? InstitutionDepartment,
    string? InstitutionDisplayName,
    string? ResearchField,
    string? OrcidId,
    string? OrcidUrl,
    string? Website,
    string? PhotoUrl,
    string? PhotoThumbnailUrl,
    bool IsComplete,
    DateTime CreatedAt,
    DateTime? ModifiedAt);
```

### ProfileSummaryDto
```csharp
public sealed record ProfileSummaryDto(
    string UserId,
    string FirstName,
    string? LastName,
    string FullName,
    string Initials,
    string? PhotoThumbnailUrl);
```

### ProfileStatsDto
```csharp
public sealed record ProfileStatsDto(
    int TotalStudies,
    int OwnedStudies,
    int TotalTraces,
    int TotalAlignments,
    int CompletedAlignments,
    DateTime? LastActivityAt,
    DateTime MemberSince);
```

---

## Errores de Dominio

| Código | Mensaje |
|--------|---------|
| `Profile.NotFound` | The specified profile was not found |
| `Profile.AlreadyExists` | A profile already exists for this user |
| `Profile.FirstNameRequired` | First name is required |
| `Profile.FirstNameTooShort` | First name must be at least {min} characters |
| `Profile.FirstNameTooLong` | First name cannot exceed {max} characters |
| `Profile.FirstNameInvalidFormat` | First name can only contain letters, spaces, hyphens, and apostrophes |
| `Profile.LastNameTooLong` | Last name cannot exceed {max} characters |
| `Profile.BioTooLong` | Bio cannot exceed {max} characters |
| `Profile.LocationTooLong` | Location cannot exceed {max} characters |
| `Profile.OrcidIdInvalidFormat` | ORCID ID must be in format 0000-0000-0000-0000 |
| `Profile.WebsiteInvalidFormat` | Website must be a valid URL |
| `Profile.PhotoUrlInvalidFormat` | Photo URL must be a valid URL |
| `Profile.PhotoTooLarge` | Photo size cannot exceed 10 MB |
| `Profile.InvalidPhotoFormat` | Allowed formats: JPEG, PNG, GIF, WebP |
| `Profile.InvalidPhotoData` | Photo data is invalid or empty |

---

## Event Handlers

### CreateProfileOnUserRegisteredHandler

Escucha `UserRegisteredEvent` y crea automáticamente un perfil:

```csharp
public async Task Handle(UserRegisteredEvent notification, CancellationToken ct)
{
    var profile = Profile.Create(
        profileId: ProfileId.From(notification.UserId),
        userId: notification.UserId,
        name: PersonName.Create(notification.Username).Value
    );

    await _profileRepository.AddAsync(profile, ct);
    await _unitOfWork.SaveChangesAsync(ct);
}
```

---

## Tests

El módulo incluye tests unitarios para:

- **Domain**: Profile, ProfileId, Value Objects, Enumerations
- **Application**: Todos los Command y Query handlers
- **Total**: 199 tests

```bash
dotnet test --filter "FullyQualifiedName~Profiles"
```

---

## Integración con Datalake

El módulo publica eventos al datalake para:

1. **Storage de fotos**: `ProfilePhotoUploadedEvent` → StorageMounter
   - Almacena en: `profiles/{profile_id}/photo.{ext}`
   - Genera thumbnail: `profiles/{profile_id}/thumbnail.{ext}`

2. **Proyección a PostgreSQL**: Eventos de perfil → PostgresMounter
   - Tabla: `profiles` en schema `datalake`

---

## Dependencias

```
GeneFlow.ApiNet2.Domain.Profiles
    └── GeneFlow.ApiNet2.SharedKernel

GeneFlow.ApiNet2.Application.Profiles
    ├── GeneFlow.ApiNet2.Domain.Profiles
    └── GeneFlow.ApiNet2.SharedKernel

GeneFlow.ApiNet2.Infrastructure.Profiles
    ├── GeneFlow.ApiNet2.Domain.Profiles
    └── Microsoft.EntityFrameworkCore

GeneFlow.ApiNet2.API.Profiles
    ├── GeneFlow.ApiNet2.Application.Profiles
    └── MediatR
```
