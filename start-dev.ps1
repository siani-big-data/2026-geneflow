# =============================================================================
# GeneFlow - Development Startup Script (Full Stack)
# =============================================================================
# Usage:
#   .\start-dev.ps1                    # Start all services
#   .\start-dev.ps1 -Minimal           # Only Redis + PostgreSQL + API
#   .\start-dev.ps1 -Backend           # Infrastructure + API + Workers (no frontend)
#   .\start-dev.ps1 -Stop              # Stop all services
#   .\start-dev.ps1 -Status            # Check status of all services
# =============================================================================

param(
    [switch]$Minimal,
    [switch]$Backend,
    [switch]$Stop,
    [switch]$Status
)

$ErrorActionPreference = "Continue"

# Colors
function Write-Header { param($msg) Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-Step { param($msg) Write-Host "[+] $msg" -ForegroundColor Green }
function Write-Info { param($msg) Write-Host "    $msg" -ForegroundColor Gray }
function Write-Warn { param($msg) Write-Host "[!] $msg" -ForegroundColor Yellow }
function Write-Err { param($msg) Write-Host "[X] $msg" -ForegroundColor Red }

# Paths
$ROOT = "C:\develop\geneflow"
$API_PATH = "$ROOT\geneflow-backend\GeneFlow.ApiNet2\GeneFlow.ApiNet2.API"
$DATALAKE_PATH = "$ROOT\geneflow-datalake"
$ANALYSIS_PATH = "$ROOT\geneflow-analysis"
$AI_PATH = "$ROOT\geneflow-ai"
$FRONTEND_PATH = "$ROOT\geneflow-frontend"

# =============================================================================
# STATUS CHECK
# =============================================================================
if ($Status) {
    Write-Header "GeneFlow Services Status"

    Write-Host "`n  Docker Containers:" -ForegroundColor White
    $containers = @("geneflow_redis", "geneflow_postgres", "geneflow_mailpit")
    foreach ($c in $containers) {
        $running = docker ps --filter "name=$c" --format "{{.Status}}" 2>$null
        if ($running) {
            Write-Host "    [OK] $c - $running" -ForegroundColor Green
        } else {
            Write-Host "    [--] $c - not running" -ForegroundColor Gray
        }
    }

    Write-Host "`n  Ports:" -ForegroundColor White
    $ports = @(
        @{Port=5432; Name="PostgreSQL"},
        @{Port=6379; Name="Redis"},
        @{Port=5145; Name="API"},
        @{Port=8080; Name="Analysis Worker"},
        @{Port=8082; Name="Datalake"},
        @{Port=8090; Name="AI Service"},
        @{Port=3000; Name="Frontend"},
        @{Port=8025; Name="Mailpit UI"}
    )
    foreach ($p in $ports) {
        $listening = Get-NetTCPConnection -LocalPort $p.Port -State Listen -ErrorAction SilentlyContinue
        if ($listening) {
            Write-Host "    [OK] $($p.Name) - port $($p.Port)" -ForegroundColor Green
        } else {
            Write-Host "    [--] $($p.Name) - port $($p.Port) not listening" -ForegroundColor Gray
        }
    }
    exit 0
}

# =============================================================================
# STOP ALL SERVICES
# =============================================================================
if ($Stop) {
    Write-Header "Stopping GeneFlow Services"

    Write-Step "Stopping Docker containers..."
    docker stop geneflow_redis geneflow_postgres geneflow_mailpit 2>$null
    docker rm geneflow_redis geneflow_postgres geneflow_mailpit 2>$null

    Write-Step "Stopping .NET processes..."
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

    Write-Step "Stopping Node.js processes..."
    Get-Process -Name "node" -ErrorAction SilentlyContinue | Stop-Process -Force

    Write-Step "Stopping Python processes..."
    Get-Process -Name "python" -ErrorAction SilentlyContinue | Stop-Process -Force

    Write-Host "`nAll services stopped." -ForegroundColor Green
    exit 0
}

# =============================================================================
# START SERVICES
# =============================================================================
Write-Header "Starting GeneFlow Development Environment"

# -----------------------------------------------------------------------------
# 1. Docker Services (Redis + PostgreSQL + Mailpit)
# -----------------------------------------------------------------------------
Write-Step "Starting Redis..."
$redisRunning = docker ps --filter "name=geneflow_redis" --format "{{.Names}}" 2>$null
if (-not $redisRunning) {
    docker run -d `
        --name geneflow_redis `
        -p 6379:6379 `
        redis:7-alpine `
        redis-server --appendonly yes | Out-Null
    Write-Info "Redis started on port 6379"
} else {
    Write-Info "Redis already running"
}

Write-Step "Starting PostgreSQL..."
$pgRunning = docker ps --filter "name=geneflow_postgres" --format "{{.Names}}" 2>$null
if (-not $pgRunning) {
    docker run -d `
        --name geneflow_postgres `
        -p 5432:5432 `
        -e POSTGRES_USER=geneflow `
        -e POSTGRES_PASSWORD=geneflow `
        -e POSTGRES_DB=geneflow `
        postgres:16-alpine | Out-Null
    Write-Info "PostgreSQL started on port 5432"
    Write-Info "Waiting for PostgreSQL to be ready..."
    Start-Sleep -Seconds 3
} else {
    Write-Info "PostgreSQL already running"
}

Write-Step "Starting Mailpit (email testing)..."
$mailpitRunning = docker ps --filter "name=geneflow_mailpit" --format "{{.Names}}" 2>$null
if (-not $mailpitRunning) {
    docker run -d `
        --name geneflow_mailpit `
        -p 1025:1025 `
        -p 8025:8025 `
        axllent/mailpit:latest | Out-Null
    Write-Info "Mailpit started - SMTP:1025, UI:8025"
} else {
    Write-Info "Mailpit already running"
}

# -----------------------------------------------------------------------------
# 2. Datalake (Event Store + PostgreSQL Mounter)
# -----------------------------------------------------------------------------
if (-not $Minimal) {
    Write-Step "Starting Datalake (Event Store + Mounter)..."

    $datalakeScript = @"
cd /d "$DATALAKE_PATH"
set REDIS_URL=redis://localhost:6379
set REDIS_STREAM_PREFIX=geneflow:events
set REDIS_CONSUMER_GROUP=datalake-consumers
set REDIS_CONSUMER_NAME=datalake-dev
set POSTGRES_HOST=localhost
set POSTGRES_PORT=5432
set POSTGRES_USER=geneflow
set POSTGRES_PASSWORD=geneflow
set POSTGRES_DB=geneflow
set MOUNTER_ENABLED=true
set DATALAKE_STORAGE_PATH=$DATALAKE_PATH\data\events
set DATALAKE_API_PORT=8082
set DATALAKE_LOG_LEVEL=INFO
uv run python main.py
"@
    Start-Process -FilePath "cmd.exe" -ArgumentList "/k", $datalakeScript -WindowStyle Normal
    Write-Info "Datalake starting on port 8082"
}

# -----------------------------------------------------------------------------
# 3. .NET API
# -----------------------------------------------------------------------------
Write-Step "Starting .NET API..."

$apiScript = @"
cd /d "$API_PATH"
dotnet run --urls "http://localhost:5145"
"@
Start-Process -FilePath "cmd.exe" -ArgumentList "/k", $apiScript -WindowStyle Normal
Write-Info "API starting on port 5145"

# -----------------------------------------------------------------------------
# 4. Analysis Worker
# -----------------------------------------------------------------------------
if (-not $Minimal) {
    Write-Step "Starting Analysis Worker..."

    $analysisScript = @"
cd /d "$ANALYSIS_PATH"
set WORKER_REDIS_URL=redis://localhost:6379
set WORKER_REDIS_CONSUMER_GROUP=geneflow-analysis-consumers
set WORKER_REDIS_CONSUMER_NAME=analysis-dev
set WORKER_API_HOST=0.0.0.0
set WORKER_API_PORT=8080
set WORKER_STORAGE_PROVIDER=local
set WORKER_LOCAL_STORAGE_PATH=$ANALYSIS_PATH\data
set WORKER_EVENTBUS_ENABLED=true
set WORKER_EVENTBUS_STREAM_PREFIX=geneflow:events
set WORKER_LOG_LEVEL=INFO
uv run python -m src.main
"@
    Start-Process -FilePath "cmd.exe" -ArgumentList "/k", $analysisScript -WindowStyle Normal
    Write-Info "Analysis Worker starting on port 8080"
}

# -----------------------------------------------------------------------------
# 5. AI Service
# -----------------------------------------------------------------------------
if (-not $Minimal) {
    Write-Step "Starting AI Service..."

    $aiScript = @"
cd /d "$AI_PATH"
set AI_REDIS_URL=redis://localhost:6379
set AI_REDIS_CONSUMER_GROUP=ai-consumers
set AI_REDIS_CONSUMER_NAME=ai-dev
set AI_API_HOST=0.0.0.0
set AI_API_PORT=8090
set AI_EVENTBUS_ENABLED=true
set AI_EVENTBUS_STREAM_PREFIX=geneflow:events
set AI_LOG_LEVEL=INFO
REM Provider is taken from geneflow-ai\.env (deepseek, has a valid API key).
REM Do NOT force claude here: AI_CLAUDE_API_KEY is empty, which leaves the
REM agent without a usable provider ("claude_api_not_configured").
uv run python -m src.main
"@
    Start-Process -FilePath "cmd.exe" -ArgumentList "/k", $aiScript -WindowStyle Normal
    Write-Info "AI Service starting on port 8090"
}

# -----------------------------------------------------------------------------
# 6. Frontend (Next.js)
# -----------------------------------------------------------------------------
if (-not $Minimal -and -not $Backend) {
    Write-Step "Starting Frontend (Next.js)..."

    $frontendScript = @"
cd /d "$FRONTEND_PATH"
pnpm dev
"@
    Start-Process -FilePath "cmd.exe" -ArgumentList "/k", $frontendScript -WindowStyle Normal
    Write-Info "Frontend starting on port 3000"
}

# =============================================================================
# SUMMARY
# =============================================================================
Start-Sleep -Seconds 2
Write-Header "GeneFlow Development Environment"

Write-Host ""
Write-Host "  INFRASTRUCTURE" -ForegroundColor Yellow
Write-Host "  Service          Port        URL" -ForegroundColor White
Write-Host "  -------          ----        ---" -ForegroundColor DarkGray
Write-Host "  PostgreSQL       5432        localhost:5432"
Write-Host "  Redis            6379        localhost:6379"
Write-Host "  Mailpit SMTP     1025        localhost:1025"
Write-Host "  Mailpit UI       8025        http://localhost:8025"

Write-Host ""
Write-Host "  BACKEND" -ForegroundColor Yellow
Write-Host "  Service          Port        URL" -ForegroundColor White
Write-Host "  -------          ----        ---" -ForegroundColor DarkGray
Write-Host "  API              5145        http://localhost:5145" -ForegroundColor Cyan
Write-Host "  Swagger          5145        http://localhost:5145/swagger" -ForegroundColor Cyan

if (-not $Minimal) {
    Write-Host "  Datalake         8082        http://localhost:8082"
    Write-Host "  Analysis Worker  8080        http://localhost:8080"
    Write-Host "  AI Service       8090        http://localhost:8090"
}

if (-not $Minimal -and -not $Backend) {
    Write-Host ""
    Write-Host "  FRONTEND" -ForegroundColor Yellow
    Write-Host "  Service          Port        URL" -ForegroundColor White
    Write-Host "  -------          ----        ---" -ForegroundColor DarkGray
    Write-Host "  Next.js          3000        http://localhost:3000" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "  Commands:" -ForegroundColor Yellow
Write-Host "    .\start-dev.ps1 -Status    Check all services"
Write-Host "    .\start-dev.ps1 -Stop      Stop all services"
Write-Host ""
