#requires -Version 5.1
<#
    Reset script for GeneFlow demo environment (Docker-based stack).

    DESTRUCTIVE: drops the geneflow Postgres database, flushes Redis,
    wipes local uploads and re-runs all EF migrations.

    Steps:
      1. Stop docker consumers (analysis, datalake-consumer) so they do
         not hold open connections during the drop.
      2. Backup current Postgres DB via `docker exec pg_dump`.
      3. Drop & recreate the geneflow database via `docker exec psql`.
      4. Apply EF migrations for the 8 DbContexts (host -> 127.0.0.1:5432).
      5. FLUSHALL on Redis via `docker exec redis-cli`.
      6. Wipe the local uploads folder.
      7. Restart the docker consumers.

    Pre-requisites:
      - Docker Desktop running.
      - The .NET API process (dotnet run) stopped before invocation.
      - dotnet-ef installed globally (verified with `dotnet ef --version`).
#>
param(
    [switch] $WipeDatalake,
    [switch] $SkipBackup,
    [switch] $Force,
    [string] $PgContainer    = "geneflow-core-postgres",
    [string] $RedisContainer = "geneflow-core-redis",
    [string] $MinioContainer = "geneflow-core-minio",
    [string] $PgDatabase     = "geneflow",
    [string] $PgUser         = "geneflow",
    [string] $PgHostFromHost = "127.0.0.1",
    [int]    $PgPortFromHost = 5432,
    [string[]] $ConsumerContainers = @("geneflow-analysis", "geneflow-datalake-consumer")
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

Write-Host ""
Write-Host "GeneFlow demo reset (docker stack)" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Repo root        : $repoRoot"
Write-Host "Postgres container: $PgContainer"
Write-Host "Redis container   : $RedisContainer"
Write-Host "Consumers stopped : $($ConsumerContainers -join ', ')"
Write-Host "Uploads           : $repoRoot\GeneFlow.ApiNet2.API\uploads"
Write-Host "Datalake          : $(if ($WipeDatalake) { 'WILL be wiped' } else { 'kept' })"
Write-Host ""

if (-not $Force) {
    $confirm = Read-Host "Type 'RESET' to proceed (anything else aborts)"
    if ($confirm -ne "RESET") {
        Write-Host "Aborted." -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "Force mode: skipping confirmation prompt." -ForegroundColor Yellow
}

# Helper: invoke docker exec, throw if it fails.
function Invoke-DockerExec {
    param([string]$Container, [string[]]$DockerArgs, [string[]]$Cmd, [string]$Stdin = $null)
    if ($Stdin) {
        $Stdin | docker exec -i $Container @Cmd
    } else {
        docker exec @DockerArgs $Container @Cmd
    }
    if ($LASTEXITCODE -ne 0) {
        throw "docker exec $Container $($Cmd -join ' ') failed (exit $LASTEXITCODE)"
    }
}

# --- 1. Stop consumers --------------------------------------------------
Write-Host "[1/7] Stop consumer containers" -ForegroundColor Green
foreach ($c in $ConsumerContainers) {
    $running = docker ps --filter "name=^$c$" --format "{{.Names}}"
    if ($running) {
        Write-Host "  - stopping $c"
        docker stop $c | Out-Null
    } else {
        Write-Host "  - $c not running, skip"
    }
}

# --- 2. Backup ----------------------------------------------------------
if (-not $SkipBackup) {
    $stamp     = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupDir = Join-Path $repoRoot "Tools\backups"
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    $backupFile = Join-Path $backupDir "geneflow-pre-demo-$stamp.sql"
    Write-Host "[2/7] Backup -> $backupFile" -ForegroundColor Green
    docker exec $PgContainer pg_dump -U $PgUser -d $PgDatabase --no-owner --no-acl |
        Out-File -FilePath $backupFile -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw "pg_dump failed" }
} else {
    Write-Host "[2/7] Backup skipped (-SkipBackup)" -ForegroundColor Yellow
}

# --- 3. Drop & recreate Postgres ---------------------------------------
Write-Host "[3/7] Drop & recreate database $PgDatabase" -ForegroundColor Green
$dropSql = @"
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = '$PgDatabase' AND pid <> pg_backend_pid();
"@
docker exec $PgContainer psql -U $PgUser -d postgres -v ON_ERROR_STOP=1 -c $dropSql | Out-Null
docker exec $PgContainer psql -U $PgUser -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE IF EXISTS $PgDatabase;" | Out-Null
docker exec $PgContainer psql -U $PgUser -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE $PgDatabase OWNER $PgUser;" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Postgres recreate failed" }

# --- 4. Apply migrations -----------------------------------------------
Write-Host "[4/7] Apply EF migrations (8 contexts)" -ForegroundColor Green
$startupProject = Join-Path $repoRoot "GeneFlow.ApiNet2.API\GeneFlow.ApiNet2.API.csproj"
$infraProject   = Join-Path $repoRoot "GeneFlow.ApiNet2.Infrastructure\GeneFlow.ApiNet2.Infrastructure.csproj"

$contexts = @(
    "UserContext",
    "PlanContext",
    "SubscriptionContext",
    "ProfileContext",
    "PaymentMethodContext",
    "StudyContext",
    "TraceContext",
    "PipelineContext"
)

foreach ($ctx in $contexts) {
    Write-Host "  - $ctx"
    & dotnet ef database update --context $ctx `
        --project $infraProject `
        --startup-project $startupProject
    if ($LASTEXITCODE -ne 0) {
        throw "EF migration failed for $ctx"
    }
}

# --- 5. Flush Redis ----------------------------------------------------
Write-Host "[5/7] FLUSHALL on Redis" -ForegroundColor Green
docker exec $RedisContainer redis-cli FLUSHALL | Out-Null
if ($LASTEXITCODE -ne 0) { throw "redis-cli FLUSHALL failed" }

# --- 6. Wipe uploads ---------------------------------------------------
$uploadsPath = Join-Path $repoRoot "GeneFlow.ApiNet2.API\uploads"
Write-Host "[6/7] Wipe uploads at $uploadsPath" -ForegroundColor Green
if (Test-Path $uploadsPath) {
    Get-ChildItem -Path $uploadsPath -Force -Recurse |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

# --- 7. (Optional) wipe MinIO + restart consumers ---------------------
if ($WipeDatalake) {
    Write-Host "[7/7] Wipe MinIO bucket geneflow" -ForegroundColor Green
    docker exec $MinioContainer sh -c "mc alias set local http://localhost:9000 geneflow geneflow123 >/dev/null 2>&1 || true; mc rb --force local/geneflow >/dev/null 2>&1 || true; mc mb local/geneflow >/dev/null 2>&1 || true"
} else {
    Write-Host "[7/7] Skipping datalake (pass -WipeDatalake to wipe MinIO)" -ForegroundColor Yellow
}

# Restart consumers
Write-Host "Restart consumers" -ForegroundColor Green
foreach ($c in $ConsumerContainers) {
    $exists = docker ps -a --filter "name=^$c$" --format "{{.Names}}"
    if ($exists) {
        Write-Host "  - starting $c"
        docker start $c | Out-Null
    }
}

Write-Host ""
Write-Host "Done. Platform is empty. Restart the .NET API now (dotnet run)." -ForegroundColor Cyan
