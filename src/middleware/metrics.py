"""Prometheus metrics middleware."""

import time
from dataclasses import dataclass, field
from typing import Callable

import structlog
from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware

logger = structlog.get_logger()


@dataclass
class RequestMetrics:
    """Metrics for a single request."""

    method: str
    path: str
    status_code: int
    duration_ms: float


@dataclass
class MetricsCollector:
    """Collects application metrics."""

    # Request metrics
    request_count: int = 0
    request_errors: int = 0
    request_duration_sum: float = 0.0
    request_duration_count: int = 0

    # Per-path metrics
    path_counts: dict[str, int] = field(default_factory=dict)
    path_durations: dict[str, list[float]] = field(default_factory=dict)
    status_counts: dict[int, int] = field(default_factory=dict)

    # Model/LLM metrics
    llm_requests: int = 0
    llm_tokens_input: int = 0
    llm_tokens_output: int = 0
    llm_errors: int = 0

    # ML model metrics
    ml_predictions: int = 0
    ml_duration_sum: float = 0.0

    # External API metrics
    external_requests: int = 0
    external_errors: int = 0

    def record_request(self, metrics: RequestMetrics) -> None:
        """Record request metrics."""
        self.request_count += 1
        self.request_duration_sum += metrics.duration_ms
        self.request_duration_count += 1

        # Per-path
        path = self._normalize_path(metrics.path)
        self.path_counts[path] = self.path_counts.get(path, 0) + 1

        if path not in self.path_durations:
            self.path_durations[path] = []
        self.path_durations[path].append(metrics.duration_ms)
        # Keep only last 100 for memory
        if len(self.path_durations[path]) > 100:
            self.path_durations[path] = self.path_durations[path][-100:]

        # Status codes
        self.status_counts[metrics.status_code] = self.status_counts.get(metrics.status_code, 0) + 1

        if metrics.status_code >= 400:
            self.request_errors += 1

    def record_llm(
        self,
        input_tokens: int,
        output_tokens: int,
        error: bool = False,
    ) -> None:
        """Record LLM usage."""
        self.llm_requests += 1
        self.llm_tokens_input += input_tokens
        self.llm_tokens_output += output_tokens
        if error:
            self.llm_errors += 1

    def record_ml_prediction(self, duration_ms: float) -> None:
        """Record ML model prediction."""
        self.ml_predictions += 1
        self.ml_duration_sum += duration_ms

    def record_external_api(self, error: bool = False) -> None:
        """Record external API call."""
        self.external_requests += 1
        if error:
            self.external_errors += 1

    def _normalize_path(self, path: str) -> str:
        """Normalize path for grouping."""
        # Remove query string
        path = path.split("?")[0]
        # Replace UUIDs and IDs with placeholder
        import re

        path = re.sub(
            r"/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
            "/{id}",
            path,
        )
        path = re.sub(r"/\d+", "/{id}", path)
        return path

    def get_prometheus_metrics(self) -> str:
        """Export metrics in Prometheus format."""
        lines = []

        # Request metrics
        lines.append("# HELP geneflow_requests_total Total HTTP requests")
        lines.append("# TYPE geneflow_requests_total counter")
        lines.append(f"geneflow_requests_total {self.request_count}")

        lines.append("# HELP geneflow_request_errors_total Total HTTP errors")
        lines.append("# TYPE geneflow_request_errors_total counter")
        lines.append(f"geneflow_request_errors_total {self.request_errors}")

        # Request duration
        lines.append("# HELP geneflow_request_duration_seconds Request duration")
        lines.append("# TYPE geneflow_request_duration_seconds summary")
        avg_duration = (
            self.request_duration_sum / self.request_duration_count / 1000
            if self.request_duration_count > 0
            else 0
        )
        lines.append(f'geneflow_request_duration_seconds{{quantile="0.5"}} {avg_duration:.6f}')

        # Status codes
        lines.append("# HELP geneflow_responses_total Responses by status code")
        lines.append("# TYPE geneflow_responses_total counter")
        for status, count in sorted(self.status_counts.items()):
            lines.append(f'geneflow_responses_total{{status="{status}"}} {count}')

        # Per-path metrics
        lines.append("# HELP geneflow_path_requests_total Requests by path")
        lines.append("# TYPE geneflow_path_requests_total counter")
        for path, count in sorted(self.path_counts.items()):
            safe_path = path.replace('"', '\\"')
            lines.append(f'geneflow_path_requests_total{{path="{safe_path}"}} {count}')

        # LLM metrics
        lines.append("# HELP geneflow_llm_requests_total LLM API requests")
        lines.append("# TYPE geneflow_llm_requests_total counter")
        lines.append(f"geneflow_llm_requests_total {self.llm_requests}")

        lines.append("# HELP geneflow_llm_tokens_total LLM tokens used")
        lines.append("# TYPE geneflow_llm_tokens_total counter")
        lines.append(f'geneflow_llm_tokens_total{{type="input"}} {self.llm_tokens_input}')
        lines.append(f'geneflow_llm_tokens_total{{type="output"}} {self.llm_tokens_output}')

        # ML metrics
        lines.append("# HELP geneflow_ml_predictions_total ML predictions")
        lines.append("# TYPE geneflow_ml_predictions_total counter")
        lines.append(f"geneflow_ml_predictions_total {self.ml_predictions}")

        # External API metrics
        lines.append("# HELP geneflow_external_requests_total External API requests")
        lines.append("# TYPE geneflow_external_requests_total counter")
        lines.append(f"geneflow_external_requests_total {self.external_requests}")
        lines.append(f"geneflow_external_errors_total {self.external_errors}")

        return "\n".join(lines)

    def get_json_metrics(self) -> dict:
        """Export metrics as JSON."""
        avg_duration = (
            self.request_duration_sum / self.request_duration_count
            if self.request_duration_count > 0
            else 0
        )

        return {
            "requests": {
                "total": self.request_count,
                "errors": self.request_errors,
                "avgDurationMs": round(avg_duration, 2),
            },
            "statusCodes": self.status_counts,
            "paths": self.path_counts,
            "llm": {
                "requests": self.llm_requests,
                "tokensInput": self.llm_tokens_input,
                "tokensOutput": self.llm_tokens_output,
                "errors": self.llm_errors,
            },
            "ml": {
                "predictions": self.ml_predictions,
                "avgDurationMs": round(self.ml_duration_sum / self.ml_predictions, 2)
                if self.ml_predictions > 0
                else 0,
            },
            "externalApis": {
                "requests": self.external_requests,
                "errors": self.external_errors,
            },
        }


# Global metrics collector
_metrics = MetricsCollector()


def get_metrics() -> MetricsCollector:
    """Get global metrics collector."""
    return _metrics


class MetricsMiddleware(BaseHTTPMiddleware):
    """FastAPI middleware for collecting metrics."""

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        start_time = time.perf_counter()

        response = await call_next(request)

        duration_ms = (time.perf_counter() - start_time) * 1000

        _metrics.record_request(
            RequestMetrics(
                method=request.method,
                path=request.url.path,
                status_code=response.status_code,
                duration_ms=duration_ms,
            )
        )

        return response
