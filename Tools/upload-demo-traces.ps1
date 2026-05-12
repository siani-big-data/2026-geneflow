#requires -Version 5.1
<#
    Sube todas las trazas .ab1 de un directorio (por defecto
    ../../geneflow-ai/datalake/ab1) y las distribuye en los 5 estudios
    demo. Cada estudio recibe una porcion contigua y es subida por su owner.

    Uso:
      ./upload-demo-traces.ps1
      ./upload-demo-traces.ps1 -Limit 25       # solo 25 por estudio (smoke)
#>
param(
    [string] $ApiBase = "http://localhost:5000",
    [string] $Ab1Dir  = "$PSScriptRoot/../../../geneflow-ai/datalake/ab1",
    [int]    $Limit   = 0,            # 0 = sin limite (todos)
    [int]    $ThrottleMs = 0          # pausa entre uploads
)

$ErrorActionPreference = "Stop"

# Credenciales demo
$users = @{
    "eduardo" = @{ Email = "eduardo.marrero@alu.ulpgc.es"; Password = "Eduardo.Marrero1" }
    "jose"    = @{ Email = "jose.galvez@ulpgc.es";         Password = "Jose.Galvez1"     }
    "mario"   = @{ Email = "mario.caballero@ulpgc.es";     Password = "Mario.Caballero1" }
}

# Reparto: studyId, owner, share (porcentaje)
$assignments = @(
    @{ StudyId = "S00000007"; Owner = "eduardo"; Share = 0.20 },
    @{ StudyId = "S00000008"; Owner = "eduardo"; Share = 0.20 },
    @{ StudyId = "S00000009"; Owner = "jose";    Share = 0.20 },
    @{ StudyId = "S00000010"; Owner = "jose";    Share = 0.20 },
    @{ StudyId = "S00000011"; Owner = "mario";   Share = 0.20 }
)

# 1) Login
Write-Host "Login demo users..." -ForegroundColor Green
$tokens = @{}
foreach ($k in $users.Keys) {
    $body = @{ identifier = $users[$k].Email; password = $users[$k].Password } | ConvertTo-Json -Compress
    $resp = Invoke-RestMethod -Method POST -Uri "$ApiBase/api/v1/auth/login" -ContentType "application/json" -Body $body
    $tokens[$k] = $resp.tokens.accessToken
    Write-Host "  - $k OK"
}

# 2) Listar AB1
$resolvedDir = (Resolve-Path $Ab1Dir).Path
Write-Host ""
Write-Host "Directorio: $resolvedDir" -ForegroundColor Cyan
$allFiles = @(Get-ChildItem -Path $resolvedDir -Filter "*.ab1" -File | Sort-Object Name)
$total = $allFiles.Count
Write-Host "Total .ab1: $total"

if ($total -eq 0) { throw "No hay .ab1 en $resolvedDir" }

# 3) Slice por estudio
$slices = @()
$idx = 0
for ($i = 0; $i -lt $assignments.Count; $i++) {
    $a = $assignments[$i]
    $count = if ($i -eq $assignments.Count - 1) {
        $total - $idx
    } else {
        [int][math]::Round($total * $a.Share)
    }
    if ($Limit -gt 0 -and $count -gt $Limit) { $count = $Limit }
    $end = [math]::Min($idx + $count, $total) - 1
    if ($end -ge $idx) {
        $slice = $allFiles[$idx..$end]
    } else {
        $slice = @()
    }
    $slices += @{ StudyId = $a.StudyId; Owner = $a.Owner; Files = $slice }
    Write-Host "  $($a.StudyId) ($($a.Owner)) -> $($slice.Count) files (idx $idx..$end)"
    $idx = $end + 1
}

# 4) Subir
foreach ($s in $slices) {
    Write-Host ""
    Write-Host "[Subiendo $($s.Files.Count) trazas a $($s.StudyId)]" -ForegroundColor Cyan
    $token = $tokens[$s.Owner]
    $studyId = $s.StudyId
    $url = "$ApiBase/api/v1/studies/$studyId/traces/upload"
    $ok = 0; $fail = 0
    foreach ($f in $s.Files) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
        # curl maneja multipart limpiamente. -s silencioso, -o /dev/null body, -w "%{http_code}"
        $code = & curl.exe --silent --show-error --output NUL --write-out "%{http_code}" `
            -X POST $url `
            -H "Authorization: Bearer $token" `
            -F "file=@$($f.FullName);type=application/octet-stream" `
            -F "name=$name" `
            -F "description=Demo upload"
        if ($code -eq "201" -or $code -eq "200") {
            $ok++
            if ($ok % 25 -eq 0) { Write-Host "    $ok OK..." }
        } else {
            $fail++
            if ($fail -le 3) { Write-Host "    FAIL ($code) $($f.Name)" -ForegroundColor Yellow }
        }
        if ($ThrottleMs -gt 0) { Start-Sleep -Milliseconds $ThrottleMs }
    }
    Write-Host "  -> $ok OK, $fail fallos"
}

Write-Host ""
Write-Host "Resumen final:" -ForegroundColor Cyan
docker exec geneflow-core-postgres psql -U geneflow -d geneflow -c "SELECT s.id, s.title, COUNT(t.id) AS traces FROM studies.studies s LEFT JOIN traces.traces t ON t.study_id = s.id AND NOT t.is_deleted WHERE s.id IN ('S00000007','S00000008','S00000009','S00000010','S00000011') GROUP BY s.id, s.title ORDER BY s.id;"
