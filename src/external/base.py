"""Base client for external APIs."""

from abc import ABC, abstractmethod
from dataclasses import dataclass
from typing import Any

import httpx
import structlog

logger = structlog.get_logger()


@dataclass
class APIResponse:
    """Response from external API."""

    success: bool
    data: Any = None
    error: str | None = None
    status_code: int = 0
    source: str = ""

    def to_dict(self) -> dict:
        result = {
            "success": self.success,
            "source": self.source,
        }
        if self.success:
            result["data"] = self.data
        else:
            result["error"] = self.error
            result["statusCode"] = self.status_code
        return result


class ExternalAPIClient(ABC):
    """Abstract base class for external API clients."""

    def __init__(
        self,
        base_url: str,
        timeout: float = 30.0,
        max_retries: int = 3,
    ):
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        self.max_retries = max_retries
        self._client = httpx.AsyncClient(
            base_url=self.base_url,
            timeout=timeout,
            headers=self._default_headers(),
        )
        self._requests_made = 0
        self._errors = 0

    @property
    @abstractmethod
    def name(self) -> str:
        """API name."""
        pass

    def _default_headers(self) -> dict[str, str]:
        """Default headers for requests."""
        return {
            "Accept": "application/json",
            "User-Agent": "GeneFlow-AI/1.0",
        }

    async def _get(self, endpoint: str, params: dict | None = None) -> APIResponse:
        """Make GET request with retries."""
        url = f"{endpoint}"

        for attempt in range(self.max_retries):
            try:
                response = await self._client.get(url, params=params)
                self._requests_made += 1

                if response.status_code == 200:
                    return APIResponse(
                        success=True,
                        data=response.json(),
                        status_code=200,
                        source=self.name,
                    )
                elif response.status_code == 404:
                    return APIResponse(
                        success=False,
                        error="Not found",
                        status_code=404,
                        source=self.name,
                    )
                elif response.status_code == 429:
                    # Rate limited, wait and retry
                    import asyncio

                    await asyncio.sleep(2**attempt)
                    continue
                else:
                    return APIResponse(
                        success=False,
                        error=f"HTTP {response.status_code}",
                        status_code=response.status_code,
                        source=self.name,
                    )

            except httpx.TimeoutException:
                logger.warning(
                    "api_timeout",
                    api=self.name,
                    endpoint=endpoint,
                    attempt=attempt + 1,
                )
                if attempt == self.max_retries - 1:
                    self._errors += 1
                    return APIResponse(
                        success=False,
                        error="Request timeout",
                        source=self.name,
                    )

            except Exception as e:
                logger.error("api_error", api=self.name, error=str(e))
                self._errors += 1
                return APIResponse(
                    success=False,
                    error=str(e),
                    source=self.name,
                )

        return APIResponse(
            success=False,
            error="Max retries exceeded",
            source=self.name,
        )

    async def _post(
        self,
        endpoint: str,
        data: dict | None = None,
        json_data: dict | None = None,
    ) -> APIResponse:
        """Make POST request."""
        try:
            response = await self._client.post(
                endpoint,
                data=data,
                json=json_data,
            )
            self._requests_made += 1

            if response.status_code in (200, 201):
                return APIResponse(
                    success=True,
                    data=response.json(),
                    status_code=response.status_code,
                    source=self.name,
                )
            else:
                return APIResponse(
                    success=False,
                    error=f"HTTP {response.status_code}",
                    status_code=response.status_code,
                    source=self.name,
                )

        except Exception as e:
            self._errors += 1
            return APIResponse(
                success=False,
                error=str(e),
                source=self.name,
            )

    def get_metrics(self) -> dict:
        """Return API usage metrics."""
        return {
            "api": self.name,
            "requestsMade": self._requests_made,
            "errors": self._errors,
        }

    async def close(self) -> None:
        """Close the HTTP client."""
        await self._client.aclose()
