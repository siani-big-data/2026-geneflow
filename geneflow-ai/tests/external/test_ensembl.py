"""Tests for Ensembl API client."""

import pytest

from src.external.ensembl import EnsemblClient


class TestEnsemblClient:
    """Tests for Ensembl REST API client."""

    @pytest.fixture
    def client(self):
        return EnsemblClient()

    def test_init(self, client):
        assert "ensembl.org" in client.base_url

    def test_name(self, client):
        assert client.name == "ensembl"

    def test_default_headers(self, client):
        headers = client._default_headers()
        assert "Accept" in headers
        assert "User-Agent" in headers

    def test_get_metrics(self, client):
        metrics = client.get_metrics()

        assert "api" in metrics
        assert metrics["api"] == "ensembl"
        assert "requestsMade" in metrics
        assert "errors" in metrics

    def test_client_has_required_methods(self, client):
        # Verify the client has the expected interface
        assert hasattr(client, "lookup_gene")
        assert hasattr(client, "lookup_symbol")
        assert hasattr(client, "get_sequence")
        assert hasattr(client, "get_variant_consequences")
        assert hasattr(client, "search_genes")
        assert hasattr(client, "get_homologs")
        assert callable(client.lookup_gene)
