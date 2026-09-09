"""Tests for InterPro API client."""

import pytest

from src.external.interpro import InterProClient


class TestInterProClient:
    """Tests for InterPro REST API client."""

    @pytest.fixture
    def client(self):
        return InterProClient()

    def test_init(self, client):
        assert "ebi.ac.uk" in client.base_url

    def test_name(self, client):
        assert client.name == "interpro"

    def test_default_headers(self, client):
        headers = client._default_headers()
        assert "Accept" in headers
        assert "User-Agent" in headers

    def test_get_metrics(self, client):
        metrics = client.get_metrics()

        assert metrics["api"] == "interpro"
        assert "requestsMade" in metrics
        assert "errors" in metrics

    def test_client_has_required_methods(self, client):
        # Verify the client has the expected interface
        assert hasattr(client, "lookup_entry")
        assert hasattr(client, "search_protein")
        assert hasattr(client, "get_pfam_domains")
        assert hasattr(client, "search_by_name")
        assert hasattr(client, "get_go_terms")
        assert callable(client.lookup_entry)
