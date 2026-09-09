"""GeneFlow Analysis - Entry Point."""

import asyncio
import sys

import structlog

from src.bootstrap import bootstrap
from src.lifecycle import ApplicationLifecycle

structlog.configure(
    processors=[
        structlog.stdlib.filter_by_level,
        structlog.stdlib.add_logger_name,
        structlog.stdlib.add_log_level,
        structlog.stdlib.PositionalArgumentsFormatter(),
        structlog.processors.TimeStamper(fmt="iso"),
        structlog.processors.StackInfoRenderer(),
        structlog.processors.format_exc_info,
        structlog.processors.UnicodeDecoder(),
        structlog.processors.JSONRenderer(),
    ],
    wrapper_class=structlog.stdlib.BoundLogger,
    context_class=dict,
    logger_factory=structlog.stdlib.LoggerFactory(),
    cache_logger_on_first_use=True,
)

logger = structlog.get_logger()


async def main() -> None:
    """Main entry point."""
    logger.info("geneflow_analysis_starting")

    components = bootstrap()
    lifecycle = ApplicationLifecycle(components)

    await lifecycle.run()

    logger.info("geneflow_analysis_stopped")


def run() -> None:
    """Run the service (entry point for pyproject.toml)."""
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        logger.info("keyboard_interrupt")
    except Exception as e:
        logger.error("fatal_error", error=str(e))
        sys.exit(1)


if __name__ == "__main__":
    run()
