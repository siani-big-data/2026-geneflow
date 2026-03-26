"""Traces event handler for PostgreSQL."""

from src.mounters.postgres.handlers.base import BaseHandler


class TracesHandler(BaseHandler):
    """Handler for trace-related events."""

    def __init__(self, connection):
        super().__init__(connection)
        self._event_mappings = {
            "TraceUploaded": "insert_trace",
            "TraceProcessed": "update_trace",
            "TraceDeleted": "soft_delete_trace",
            "AnnotationCreated": "insert_annotation",
            "AnnotationDeleted": "delete_annotation",
        }

    async def insert_trace(self, payload: dict) -> None:
        """Insert trace metadata."""
        await self._connection.execute(
            """INSERT INTO traces.traces (id, name, study_id, uploaded_by, file_name, format_id, size_bytes)
            VALUES ($1, $2, $3, $4, $5, $6, $7)""",
            payload.get("id"),
            payload.get("name"),
            payload.get("study_id"),
            payload.get("uploaded_by"),
            payload.get("file_name"),
            payload.get("format_id"),
            payload.get("size_bytes"),
        )

    async def update_trace(self, payload: dict) -> None:
        """Update trace with processing results."""
        updates = []
        values = []
        idx = 1

        for key in ["status_id", "total_bases", "average_quality_score", "processed_at"]:
            if key in payload:
                updates.append(f"{key} = ${idx}")
                values.append(payload[key])
                idx += 1

        if updates:
            values.append(payload.get("id"))
            query = f"UPDATE traces.traces SET {', '.join(updates)} WHERE id = ${idx}"
            await self._connection.execute(query, *values)

    async def soft_delete_trace(self, payload: dict) -> None:
        """Soft delete a trace."""
        pass

    async def insert_annotation(self, payload: dict) -> None:
        """Insert an annotation."""
        await self._connection.execute(
            """INSERT INTO traces.annotations (id, trace_id, type_id, label, start_position, end_position, created_by)
            VALUES ($1, $2, $3, $4, $5, $6, $7)""",
            payload.get("id"),
            payload.get("trace_id"),
            payload.get("type_id"),
            payload.get("label"),
            payload.get("start_position"),
            payload.get("end_position"),
            payload.get("created_by"),
        )

    async def delete_annotation(self, payload: dict) -> None:
        """Delete an annotation."""
        pass
