import json
from datetime import datetime


class MetadataService:
    """Creates and parses metadata for stored objects."""

    def create_photo_metadata(
        self,
        profile_id: str,
        extension: str,
        content_type: str,
        photo_size: int,
        thumbnail_size: int,
    ) -> bytes:
        """Create photo metadata JSON."""
        metadata = {
            "profile_id": profile_id,
            "extension": extension,
            "content_type": content_type,
            "size_bytes": photo_size,
            "thumbnail_size_bytes": thumbnail_size,
            "uploaded_at": datetime.utcnow().isoformat() + "Z",
        }
        return json.dumps(metadata).encode()

    def parse_photo_metadata(self, data: bytes) -> dict:
        """Parse photo metadata from JSON."""
        return json.loads(data.decode())
