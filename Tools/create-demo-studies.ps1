#requires -Version 5.1
<#
    Crea N estudios demo con los 3 usuarios de prueba como miembros.
    Por cada estudio:
      1) Owner crea el estudio   (POST /api/v1/studies)
      2) Owner invita a los otros 2 (POST /api/v1/studies/{id}/invitations RoleId=2 Admin)
      3) Cada invitado lista sus pending y acepta (POST /api/v1/invitations/{token}/accept)
#>
param(
    [string] $ApiBase = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

# Credenciales (deben coincidir con create-demo-user.ps1)
$users = @{
    "eduardo" = @{ Email = "eduardo.marrero@alu.ulpgc.es"; Password = "Eduardo.Marrero1" }
    "jose"    = @{ Email = "jose.galvez@ulpgc.es";         Password = "Jose.Galvez1"     }
    "mario"   = @{ Email = "mario.caballero@ulpgc.es";     Password = "Mario.Caballero1" }
}

# 5 estudios. ResearchFieldId valido (Studies enum): 1=Genomics, 2=Proteomics,
# 3=Transcriptomics, 4=Metagenomics, 5=Phylogenetics, 6=MolecularBiology,
# 7=Genetics, 8=Bioinformatics, 9=Other
$studies = @(
    @{
        Owner = "eduardo"
        Title = "Sanger trace processing pipeline benchmark"
        Description = "Estudio comparativo de algoritmos de basecalling y trimming sobre cromatogramas Sanger de la coleccion ULPGC."
        ResearchFieldId = 8
        Institution = "Universidad de Las Palmas de Gran Canaria"
        PrincipalInvestigator = "Eduardo Marrero"
        Tags = @("sanger","pipeline","benchmark")
    },
    @{
        Owner = "eduardo"
        Title = "GeneFlow demo - reproducible bioinformatics workflows"
        Description = "Showcase study para validar workflows reproducibles en GeneFlow con datasets sinteticos."
        ResearchFieldId = 8
        Institution = "Universidad de Las Palmas de Gran Canaria"
        PrincipalInvestigator = "Eduardo Marrero"
        Tags = @("demo","reproducibility")
    },
    @{
        Owner = "jose"
        Title = "Comparative genomics of Canarian endemic plants"
        Description = "Analisis comparativo del genoma de especies endemicas vegetales de las Islas Canarias."
        ResearchFieldId = 1
        Institution = "Universidad de Las Palmas de Gran Canaria"
        PrincipalInvestigator = "Jose Galvez"
        Tags = @("genomics","plants","canarias")
    },
    @{
        Owner = "jose"
        Title = "Molecular biology lab Sanger sequencing archive"
        Description = "Archivo curado de cromatogramas Sanger del laboratorio de biologia molecular para fines docentes y de investigacion."
        ResearchFieldId = 6
        Institution = "Universidad de Las Palmas de Gran Canaria"
        PrincipalInvestigator = "Jose Galvez"
        Tags = @("sanger","teaching","archive")
    },
    @{
        Owner = "mario"
        Title = "Marine biodiversity sequencing - Atlantic coast"
        Description = "Secuenciacion y analisis de especies marinas endemicas de la costa atlantica para estudio de biodiversidad."
        ResearchFieldId = 9
        Institution = "Universidad de Las Palmas de Gran Canaria"
        PrincipalInvestigator = "Mario Caballero"
        Tags = @("marine","biodiversity","atlantic")
    }
)

function Invoke-Json {
    param(
        [string] $Method,
        [string] $Url,
        $Body = $null,
        [string] $Token = $null
    )
    $headers = @{ "Accept" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    $params = @{
        Method      = $Method
        Uri         = $Url
        Headers     = $headers
        ContentType = "application/json"
    }
    if ($null -ne $Body) {
        $params["Body"] = ($Body | ConvertTo-Json -Depth 10 -Compress)
    }
    return Invoke-RestMethod @params
}

function Get-Token {
    param([hashtable] $User)
    $resp = Invoke-Json -Method "POST" -Url "$ApiBase/api/v1/auth/login" -Body @{
        identifier = $User.Email
        password   = $User.Password
    }
    return $resp.tokens.accessToken
}

# 1) Pre-login todos los tokens
Write-Host "Logging in users..." -ForegroundColor Green
$tokens = @{}
foreach ($k in $users.Keys) {
    $tokens[$k] = Get-Token -User $users[$k]
    Write-Host "  - $k (len=$($tokens[$k].Length))"
}

# 2) Crear estudios e invitar
foreach ($s in $studies) {
    $owner = $s.Owner
    Write-Host ""
    Write-Host "[Estudio] $($s.Title)" -ForegroundColor Cyan
    Write-Host "  owner: $owner ($($users[$owner].Email))"

    # Crear estudio
    $body = @{
        title                 = $s.Title
        description           = $s.Description
        researchFieldId       = $s.ResearchFieldId
        institution           = $s.Institution
        principalInvestigator = $s.PrincipalInvestigator
        tags                  = $s.Tags
    }
    try {
        $created = Invoke-Json -Method "POST" -Url "$ApiBase/api/v1/studies/" -Body $body -Token $tokens[$owner]
    } catch {
        $resp = $_.Exception.Response
        $code = if ($resp) { $resp.StatusCode.value__ } else { 0 }
        $errBody = ""
        try {
            $stream = $resp.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $errBody = $reader.ReadToEnd()
        } catch {}
        Write-Host "  - create study fallo HTTP=$code body=$errBody" -ForegroundColor Red
        continue
    }
    $studyId = $created.id
    Write-Host "  - creado id=$studyId"

    # Anadir a los otros dos como Admin via SQL (workaround del bug en study_invitations.id uuid vs varchar)
    $ownerEmail = $users[$owner].Email
    foreach ($k in $users.Keys) {
        if ($k -eq $owner) { continue }
        $memberEmail = $users[$k].Email
        $memberSql = @"
INSERT INTO studies.study_members (id, study_id, user_id, role, joined_at, invited_by)
SELECT gen_random_uuid(),
       '$studyId',
       u.id,
       'Admin',
       NOW(),
       o.id
FROM identity.users u
CROSS JOIN identity.users o
WHERE u.email = '$memberEmail'
  AND o.email = '$ownerEmail'
ON CONFLICT (study_id, user_id) DO NOTHING;
"@
        docker exec geneflow-core-postgres psql -U geneflow -d geneflow -v ON_ERROR_STOP=1 -c $memberSql | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  - $k anadido como Admin (sql)"
        } else {
            Write-Host "  - $k INSERT fallo" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Resultado final (estudios + miembros):" -ForegroundColor Cyan
docker exec geneflow-core-postgres psql -U geneflow -d geneflow -c "SELECT s.id, s.title, s.owner_id, COUNT(m.user_id) AS members FROM studies.studies s LEFT JOIN studies.study_members m ON m.study_id = s.id GROUP BY s.id, s.title, s.owner_id ORDER BY s.id;"
