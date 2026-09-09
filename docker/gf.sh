#!/bin/bash
# =============================================================================
# GeneFlow Docker Helper Script
# =============================================================================

set -e
cd "$(dirname "$0")"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_header() {
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}  GeneFlow Docker Manager${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

usage() {
    print_header
    echo ""
    echo "Usage: ./gf.sh <command> [options]"
    echo ""
    echo -e "${GREEN}Commands:${NC}"
    echo "  up [modules...]     Start services"
    echo "  down                Stop all services"
    echo "  logs [service]      View logs"
    echo "  ps                  List running services"
    echo "  restart [service]   Restart service(s)"
    echo "  clean               Stop and remove volumes"
    echo ""
    echo -e "${GREEN}Modules:${NC}"
    echo "  core                Redis, PostgreSQL, MinIO"
    echo "  admin               pgAdmin, Redis Commander, Mailpit"
    echo "  apinet              API .NET"
    echo "  datalake            Datalake consumer"
    echo "  ai                  AI workers"
    echo "  analysis            Analysis workers"
    echo "  all                 All modules"
    echo ""
    echo -e "${GREEN}Examples:${NC}"
    echo "  ./gf.sh up core              # Start core infrastructure"
    echo "  ./gf.sh up core admin        # Core + admin tools"
    echo "  ./gf.sh up core datalake     # Core + datalake"
    echo "  ./gf.sh up all               # Everything"
    echo "  ./gf.sh logs datalake        # View datalake logs"
    echo "  ./gf.sh down                 # Stop everything"
    echo ""
}

compose_files() {
    local files="-f docker-compose.yml"

    for module in "$@"; do
        case $module in
            core)
                # Already included in main compose
                ;;
            admin)
                files="$files --profile admin"
                ;;
            apinet)
                files="$files -f apinet/docker-compose.apinet.yml"
                ;;
            datalake)
                files="$files -f datalake/docker-compose.datalake.yml"
                ;;
            ai)
                files="$files -f ai/docker-compose.ai.yml"
                ;;
            analysis)
                files="$files -f analysis/docker-compose.analysis.yml"
                ;;
            all)
                files="-f docker-compose.yml --profile admin"
                files="$files -f apinet/docker-compose.apinet.yml"
                files="$files -f datalake/docker-compose.datalake.yml"
                files="$files -f ai/docker-compose.ai.yml"
                files="$files -f analysis/docker-compose.analysis.yml"
                ;;
        esac
    done

    echo "$files"
}

cmd_up() {
    if [ $# -eq 0 ]; then
        modules="core"
    else
        modules="$@"
    fi

    echo -e "${GREEN}Starting:${NC} $modules"
    files=$(compose_files $modules)
    docker compose $files up -d
    echo -e "${GREEN}Done!${NC}"
}

cmd_down() {
    echo -e "${YELLOW}Stopping all services...${NC}"
    docker compose down
    echo -e "${GREEN}Done!${NC}"
}

cmd_logs() {
    if [ -z "$1" ]; then
        docker compose logs -f
    else
        docker compose logs -f "geneflow-$1"
    fi
}

cmd_ps() {
    docker compose ps
}

cmd_restart() {
    if [ -z "$1" ]; then
        docker compose restart
    else
        docker compose restart "geneflow-$1"
    fi
}

cmd_clean() {
    echo -e "${RED}WARNING: This will delete all data!${NC}"
    read -p "Are you sure? (y/N) " confirm
    if [ "$confirm" = "y" ] || [ "$confirm" = "Y" ]; then
        docker compose down -v
        echo -e "${GREEN}Cleaned!${NC}"
    else
        echo "Cancelled."
    fi
}

# Main
case "${1:-}" in
    up)
        shift
        cmd_up "$@"
        ;;
    down)
        cmd_down
        ;;
    logs)
        shift
        cmd_logs "$@"
        ;;
    ps)
        cmd_ps
        ;;
    restart)
        shift
        cmd_restart "$@"
        ;;
    clean)
        cmd_clean
        ;;
    *)
        usage
        ;;
esac
