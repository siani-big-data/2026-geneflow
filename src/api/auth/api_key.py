from typing import Optional

from fastapi import Header, HTTPException


class ApiKeyAuthenticator:
    """API Key authentication handler."""

    def __init__(self, api_key: str | None):
        self._api_key = api_key

    async def verify(self, x_api_key: Optional[str] = Header(None)) -> None:
        """Verify the API key from request header."""
        if not self._api_key:
            return

        if x_api_key != self._api_key:
            raise HTTPException(status_code=401, detail="Invalid or missing API key")
