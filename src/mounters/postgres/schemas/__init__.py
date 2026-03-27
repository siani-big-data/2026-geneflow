"""PostgreSQL schema definitions."""

from src.mounters.postgres.schemas.alignments import ALIGNMENTS_SCHEMA
from src.mounters.postgres.schemas.billing import BILLING_SCHEMA
from src.mounters.postgres.schemas.studies import STUDIES_SCHEMA
from src.mounters.postgres.schemas.traces import TRACES_SCHEMA
from src.mounters.postgres.schemas.users import USERS_SCHEMA

__all__ = [
    "USERS_SCHEMA",
    "STUDIES_SCHEMA",
    "TRACES_SCHEMA",
    "ALIGNMENTS_SCHEMA",
    "BILLING_SCHEMA",
]
