@echo off
REM =============================================================================
REM GeneFlow Docker Helper Script (Windows)
REM =============================================================================

setlocal enabledelayedexpansion
cd /d "%~dp0"

if "%1"=="" goto :usage
if "%1"=="up" goto :up
if "%1"=="down" goto :down
if "%1"=="logs" goto :logs
if "%1"=="ps" goto :ps
if "%1"=="restart" goto :restart
if "%1"=="clean" goto :clean
goto :usage

:usage
echo.
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
echo   GeneFlow Docker Manager
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
echo.
echo Usage: gf ^<command^> [options]
echo.
echo Commands:
echo   up [modules...]     Start services
echo   down                Stop all services
echo   logs [service]      View logs
echo   ps                  List running services
echo   restart [service]   Restart service(s)
echo   clean               Stop and remove volumes
echo.
echo Modules:
echo   core                Redis, PostgreSQL, MinIO
echo   admin               pgAdmin, Redis Commander, Mailpit
echo   apinet              API .NET
echo   datalake            Datalake consumer
echo   ai                  AI workers
echo   analysis            Analysis workers
echo   all                 All modules
echo.
echo Examples:
echo   gf up core              - Start core infrastructure
echo   gf up core admin        - Core + admin tools
echo   gf up core datalake     - Core + datalake
echo   gf up all               - Everything
echo   gf logs datalake        - View datalake logs
echo   gf down                 - Stop everything
echo.
goto :eof

:up
set "MODULES="
set "FILES=-f docker-compose.yml"

shift
:parse_modules
if "%1"=="" goto :do_up

if "%1"=="core" (
    set "MODULES=!MODULES! core"
)
if "%1"=="admin" (
    set "MODULES=!MODULES! admin"
    set "FILES=!FILES! --profile admin"
)
if "%1"=="apinet" (
    set "MODULES=!MODULES! apinet"
    set "FILES=!FILES! -f apinet/docker-compose.apinet.yml"
)
if "%1"=="datalake" (
    set "MODULES=!MODULES! datalake"
    set "FILES=!FILES! -f datalake/docker-compose.datalake.yml"
)
if "%1"=="ai" (
    set "MODULES=!MODULES! ai"
    set "FILES=!FILES! -f ai/docker-compose.ai.yml"
)
if "%1"=="analysis" (
    set "MODULES=!MODULES! analysis"
    set "FILES=!FILES! -f analysis/docker-compose.analysis.yml"
)
if "%1"=="all" (
    set "MODULES=all"
    set "FILES=-f docker-compose.yml --profile admin -f apinet/docker-compose.apinet.yml -f datalake/docker-compose.datalake.yml -f ai/docker-compose.ai.yml -f analysis/docker-compose.analysis.yml"
)

shift
goto :parse_modules

:do_up
if "!MODULES!"=="" set "MODULES=core"
echo Starting:!MODULES!
docker compose !FILES! up -d
echo Done!
goto :eof

:down
echo Stopping all services...
docker compose down
echo Done!
goto :eof

:logs
if "%2"=="" (
    docker compose logs -f
) else (
    docker compose logs -f geneflow-%2
)
goto :eof

:ps
docker compose ps
goto :eof

:restart
if "%2"=="" (
    docker compose restart
) else (
    docker compose restart geneflow-%2
)
goto :eof

:clean
echo WARNING: This will delete all data!
set /p confirm="Are you sure? (y/N) "
if /i "%confirm%"=="y" (
    docker compose down -v
    echo Cleaned!
) else (
    echo Cancelled.
)
goto :eof
