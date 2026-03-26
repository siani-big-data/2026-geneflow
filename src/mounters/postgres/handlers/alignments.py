"""Alignments event handler for PostgreSQL."""

from src.mounters.postgres.handlers.base import BaseHandler


class AlignmentsHandler(BaseHandler):
    """Handler for alignment-related events."""

    def __init__(self, connection):
        super().__init__(connection)
        self._event_mappings = {
            "AlignmentCreated": "insert_alignment",
            "AlignmentTraceAdded": "insert_alignment_trace",
            "AlignmentCompleted": "update_alignment",
        }

    async def insert_alignment(self, payload: dict) -> None:
        """Insert a new alignment."""
        await self._connection.execute(
            """INSERT INTO alignments.alignments (id, name, study_id, created_by, type_id)
            VALUES ($1, $2, $3, $4, $5)""",
            payload.get("id"),
            payload.get("name"),
            payload.get("study_id"),
            payload.get("created_by"),
            payload.get("type_id"),
        )

    async def insert_alignment_trace(self, payload: dict) -> None:
        """Add a trace to an alignment."""
        await self._connection.execute(
            """INSERT INTO alignments.alignment_traces (alignment_id, trace_id, sequence_order)
            VALUES ($1, $2, $3)""",
            payload.get("alignment_id"),
            payload.get("trace_id"),
            payload.get("sequence_order"),
        )

    async def update_alignment(self, payload: dict) -> None:
        """Update alignment with results."""
        updates = []
        values = []
        idx = 1

        for key in ["status_id", "alignment_length", "identity_percentage", "consensus_sequence", "completed_at"]:
            if key in payload:
                updates.append(f"{key} = ${idx}")
                values.append(payload[key])
                idx += 1

        if updates:
            values.append(payload.get("id"))
            query = f"UPDATE alignments.alignments SET {', '.join(updates)} WHERE id = ${idx}"
            await self._connection.execute(query, *values)
