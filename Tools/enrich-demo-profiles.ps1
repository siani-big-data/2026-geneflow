#requires -Version 5.1
<#
    Rellena los perfiles demo (bio, location, institution, research field, ...).
    Trabaja directamente contra la DB via docker exec psql.
#>
param(
    [string] $PgContainer = "geneflow-core-postgres",
    [string] $PgUser      = "geneflow",
    [string] $PgDatabase  = "geneflow"
)

$ErrorActionPreference = "Stop"

# email -> { Bio, Location, ProfessionalRole, InstitutionName, InstitutionDepartment, ResearchField, OrcidId, Website }
$profiles = @(
    @{
        Email = "eduardo.marrero@alu.ulpgc.es"
        Bio = "Estudiante de Ingenieria Informatica en la ULPGC. Investigacion en analisis de cromatogramas Sanger y pipelines bioinformaticos."
        Location = "Las Palmas de Gran Canaria, Espana"
        ProfessionalRole = "Estudiante de grado"
        InstitutionName = "Universidad de Las Palmas de Gran Canaria"
        InstitutionDepartment = "Escuela de Ingenieria Informatica"
        ResearchField = "Bioinformatics"
        OrcidId = "0000-0001-0000-0001"
        Website = "https://www.ulpgc.es"
    },
    @{
        Email = "jose.galvez@ulpgc.es"
        Bio = "Profesor e investigador en biologia molecular y genomica aplicada."
        Location = "Las Palmas de Gran Canaria, Espana"
        ProfessionalRole = "Profesor titular"
        InstitutionName = "Universidad de Las Palmas de Gran Canaria"
        InstitutionDepartment = "Departamento de Bioquimica y Biologia Molecular"
        ResearchField = "MolecularBiology"
        OrcidId = "0000-0002-0000-0002"
        Website = "https://www.ulpgc.es"
    },
    @{
        Email = "mario.caballero@ulpgc.es"
        Bio = "Investigador en biologia marina y secuenciacion de especies endemicas de Canarias."
        Location = "Las Palmas de Gran Canaria, Espana"
        ProfessionalRole = "Investigador postdoctoral"
        InstitutionName = "Universidad de Las Palmas de Gran Canaria"
        InstitutionDepartment = "Instituto Universitario ECOAQUA"
        ResearchField = "MarineBiology"
        OrcidId = "0000-0003-0000-0003"
        Website = "https://www.ulpgc.es"
    }
)

function Esc { param([string]$s) if ($null -eq $s) { return "" } return $s.Replace("'", "''") }

foreach ($p in $profiles) {
    Write-Host "Enriqueciendo perfil de $($p.Email)" -ForegroundColor Green

    $sql = @"
UPDATE profiles.profiles pr
SET bio                    = '$(Esc $p.Bio)',
    location               = '$(Esc $p.Location)',
    professional_role      = '$(Esc $p.ProfessionalRole)',
    institution_name       = '$(Esc $p.InstitutionName)',
    institution_department = '$(Esc $p.InstitutionDepartment)',
    research_field         = '$(Esc $p.ResearchField)',
    orcid_id               = '$(Esc $p.OrcidId)',
    website                = '$(Esc $p.Website)',
    modified_at            = NOW()
FROM identity.users u
WHERE u.email = '$(Esc $p.Email)'
  AND pr.user_id = u.id;
"@
    docker exec $PgContainer psql -U $PgUser -d $PgDatabase -v ON_ERROR_STOP=1 -c $sql | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Update fallo para $($p.Email)" }
}

Write-Host ""
Write-Host "Resultado final:" -ForegroundColor Cyan
docker exec $PgContainer psql -U $PgUser -d $PgDatabase -c "SELECT u.email, pr.first_name, pr.last_name, pr.professional_role, pr.institution_name, pr.research_field FROM profiles.profiles pr JOIN identity.users u ON u.id = pr.user_id ORDER BY u.email;"
