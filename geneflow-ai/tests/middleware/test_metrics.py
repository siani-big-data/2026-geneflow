"""Tests for metrics middleware."""

import pytest

from src.middleware.metrics import MetricsCollector, RequestMetrics


class TestMetricsCollector:
    """Tests for Prometheus metrics collector."""

    @pytest.fixture
    def metrics(self):
        return MetricsCollector()

    def test_record_request(self, metrics):
        metrics.record_request(
            RequestMetrics(
                method="POST",
                path="/api/analyze",
                status_code=200,
                duration_ms=150.5,
            )
        )

        assert metrics.request_count == 1
        assert metrics.request_duration_sum == 150.5

    def test_record_multiple_requests(self, metrics):
        for i in range(10):
            metrics.record_request(
                RequestMetrics(
                    method="GET",
                    path="/api/test",
                    status_code=200,
                    duration_ms=100,
                )
            )

        assert metrics.request_count == 10
        assert metrics.request_duration_sum == 1000

    def test_record_error_status(self, metrics):
        metrics.record_request(
            RequestMetrics(
                method="POST",
                path="/api/fail",
                status_code=500,
                duration_ms=50,
            )
        )

        assert metrics.request_errors == 1
        assert metrics.status_counts[500] == 1

    def test_record_llm(self, metrics):
        metrics.record_llm(input_tokens=100, output_tokens=200)

        assert metrics.llm_requests == 1
        assert metrics.llm_tokens_input == 100
        assert metrics.llm_tokens_output == 200

    def test_record_llm_error(self, metrics):
        metrics.record_llm(input_tokens=50, output_tokens=0, error=True)

        assert metrics.llm_errors == 1

    def test_record_ml_prediction(self, metrics):
        metrics.record_ml_prediction(duration_ms=50.5)

        assert metrics.ml_predictions == 1
        assert metrics.ml_duration_sum == 50.5

    def test_record_external_api(self, metrics):
        metrics.record_external_api()
        metrics.record_external_api(error=True)

        assert metrics.external_requests == 2
        assert metrics.external_errors == 1

    def test_prometheus_format(self, metrics):
        metrics.record_request(
            RequestMetrics(
                method="GET",
                path="/test",
                status_code=200,
                duration_ms=100,
            )
        )
        prometheus_output = metrics.get_prometheus_metrics()

        assert isinstance(prometheus_output, str)
        assert "geneflow_requests_total" in prometheus_output
        assert "HELP" in prometheus_output
        assert "TYPE" in prometheus_output

    def test_json_export(self, metrics):
        metrics.record_request(
            RequestMetrics(
                method="GET",
                path="/test",
                status_code=200,
                duration_ms=100,
            )
        )
        json_output = metrics.get_json_metrics()

        assert isinstance(json_output, dict)
        assert "requests" in json_output
        assert json_output["requests"]["total"] == 1

    def test_path_aggregation(self, metrics):
        # Multiple requests to same path
        for _ in range(5):
            metrics.record_request(
                RequestMetrics(
                    method="POST",
                    path="/api/analyze",
                    status_code=200,
                    duration_ms=100,
                )
            )

        assert metrics.path_counts["/api/analyze"] == 5

    def test_status_code_distribution(self, metrics):
        metrics.record_request(
            RequestMetrics(method="GET", path="/test", status_code=200, duration_ms=100)
        )
        metrics.record_request(
            RequestMetrics(method="GET", path="/test", status_code=404, duration_ms=50)
        )
        metrics.record_request(
            RequestMetrics(method="GET", path="/test", status_code=500, duration_ms=30)
        )

        assert metrics.status_counts[200] == 1
        assert metrics.status_counts[404] == 1
        assert metrics.status_counts[500] == 1

    def test_path_normalization(self, metrics):
        path = metrics._normalize_path("/api/traces/12345/analyze?format=json")

        assert "{id}" in path
        assert "format" not in path

    def test_uuid_normalization(self, metrics):
        path = metrics._normalize_path("/api/traces/550e8400-e29b-41d4-a716-446655440000/analyze")

        assert "{id}" in path
        assert "550e8400" not in path
