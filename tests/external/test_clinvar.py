"""Tests for ClinVar API client."""

import pytest

from src.external.clinvar import ClinVarClient


class TestClinVarClient:
    """Tests for ClinVar/NCBI API client."""

    @pytest.fixture
    def client(self):
        return ClinVarClient()

    def test_init(self, client):
        assert "ncbi.nlm.nih.gov" in client.base_url

    def test_name(self, client):
        assert client.name == "clinvar"

    def test_default_headers(self, client):
        headers = client._default_headers()
        assert "Accept" in headers
        assert "User-Agent" in headers

    def test_get_metrics(self, client):
        metrics = client.get_metrics()

        assert metrics["api"] == "clinvar"
        assert "requestsMade" in metrics
        assert "errors" in metrics

    def test_client_has_required_methods(self, client):
        # Verify the client has the expected interface
        assert hasattr(client, "search_variant")
        assert hasattr(client, "get_variant_details")
        assert hasattr(client, "search_by_rsid")
        assert hasattr(client, "search_by_gene")
        assert hasattr(client, "get_clinical_significance")
        assert callable(client.search_variant)
