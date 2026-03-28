"""Tests for BLAST client."""

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from src.blast.client import BlastClient, BlastError
from src.blast.models import BlastJob, BlastSearchResult
from src.config import Settings


@pytest.fixture
def settings_without_email():
    """Settings without BLAST email."""
    return Settings(blast_email="")


@pytest.fixture
def settings_with_email():
    """Settings with BLAST email."""
    return Settings(blast_email="test@example.com")


@pytest.fixture
def settings_with_api_key():
    """Settings with BLAST email and API key."""
    return Settings(blast_email="test@example.com", blast_api_key="test-key")


@pytest.fixture
def sample_blast_response():
    """Sample BLAST submission response."""
    return """
    <!--
    QBlastInfoBegin
        RID = ABC123
        RTOE = 10
    QBlastInfoEnd
    -->
    """


@pytest.fixture
def sample_status_ready():
    """Sample BLAST status response (ready)."""
    return """
    Status=READY
    ThereAreHits=yes
    """


@pytest.fixture
def sample_status_waiting():
    """Sample BLAST status response (waiting)."""
    return """
    Status=WAITING
    """


@pytest.fixture
def sample_xml_results():
    """Sample BLAST XML results."""
    return """<?xml version="1.0"?>
    <BlastOutput>
        <BlastOutput_db>nt</BlastOutput_db>
        <BlastOutput_program>blastn</BlastOutput_program>
        <BlastOutput_iterations>
            <Iteration>
                <Iteration_query-len>500</Iteration_query-len>
                <Iteration_hits>
                    <Hit>
                        <Hit_accession>NC_001234</Hit_accession>
                        <Hit_def>Homo sapiens chromosome 1 [Homo sapiens]</Hit_def>
                        <Hit_hsps>
                            <Hsp>
                                <Hsp_bit-score>450.5</Hsp_bit-score>
                                <Hsp_evalue>1e-120</Hsp_evalue>
                                <Hsp_identity>245</Hsp_identity>
                                <Hsp_align-len>250</Hsp_align-len>
                                <Hsp_query-from>1</Hsp_query-from>
                                <Hsp_query-to>250</Hsp_query-to>
                                <Hsp_hit-from>1000</Hsp_hit-from>
                                <Hsp_hit-to>1250</Hsp_hit-to>
                            </Hsp>
                        </Hit_hsps>
                    </Hit>
                </Iteration_hits>
            </Iteration>
        </BlastOutput_iterations>
    </BlastOutput>
    """


class TestBlastClient:
    """Tests for BlastClient."""

    def test_not_configured_without_email(self, settings_without_email):
        """Client is not configured without email."""
        client = BlastClient(settings_without_email)
        assert client.is_configured is False

    def test_configured_with_email(self, settings_with_email):
        """Client is configured with email."""
        client = BlastClient(settings_with_email)
        assert client.is_configured is True

    def test_metrics_initial(self, settings_without_email):
        """Initial metrics are zero."""
        client = BlastClient(settings_without_email)
        metrics = client.metrics

        assert metrics["jobsSubmitted"] == 0
        assert metrics["jobsCompleted"] == 0
        assert metrics["errors"] == 0
        assert metrics["configured"] is False

    @pytest.mark.asyncio
    async def test_submit_search_not_configured(self, settings_without_email):
        """submit_search raises when not configured."""
        client = BlastClient(settings_without_email)

        with pytest.raises(BlastError, match="not configured"):
            await client.submit_search("ATCG")

    @pytest.mark.asyncio
    async def test_submit_search_success(
        self, settings_with_email, sample_blast_response
    ):
        """submit_search returns job with RID."""
        client = BlastClient(settings_with_email)

        with patch.object(client, "_get_client") as mock_get:
            mock_http = AsyncMock()
            mock_get.return_value = mock_http

            mock_response = MagicMock()
            mock_response.text = sample_blast_response
            mock_response.raise_for_status = MagicMock()
            mock_http.post = AsyncMock(return_value=mock_response)

            job = await client.submit_search("ATCGATCG")

            assert job.rid == "ABC123"
            assert job.status == "WAITING"
            assert job.program == "blastn"
            assert job.database == "nt"
            assert client.metrics["jobsSubmitted"] == 1

    @pytest.mark.asyncio
    async def test_check_status_ready(self, settings_with_email, sample_status_ready):
        """check_status returns READY when job is done."""
        client = BlastClient(settings_with_email)

        with patch.object(client, "_get_client") as mock_get:
            mock_http = AsyncMock()
            mock_get.return_value = mock_http

            mock_response = MagicMock()
            mock_response.text = sample_status_ready
            mock_response.raise_for_status = MagicMock()
            mock_http.get = AsyncMock(return_value=mock_response)

            status = await client.check_status("ABC123")

            assert status == "READY"

    @pytest.mark.asyncio
    async def test_check_status_waiting(
        self, settings_with_email, sample_status_waiting
    ):
        """check_status returns WAITING when job is pending."""
        client = BlastClient(settings_with_email)

        with patch.object(client, "_get_client") as mock_get:
            mock_http = AsyncMock()
            mock_get.return_value = mock_http

            mock_response = MagicMock()
            mock_response.text = sample_status_waiting
            mock_response.raise_for_status = MagicMock()
            mock_http.get = AsyncMock(return_value=mock_response)

            status = await client.check_status("ABC123")

            assert status == "WAITING"

    @pytest.mark.asyncio
    async def test_get_results_parses_xml(
        self, settings_with_email, sample_xml_results
    ):
        """get_results parses XML and returns BlastSearchResult."""
        client = BlastClient(settings_with_email)

        with patch.object(client, "_get_client") as mock_get:
            mock_http = AsyncMock()
            mock_get.return_value = mock_http

            mock_response = MagicMock()
            mock_response.text = sample_xml_results
            mock_response.raise_for_status = MagicMock()
            mock_http.get = AsyncMock(return_value=mock_response)

            result = await client.get_results("ABC123")

            assert result.rid == "ABC123"
            assert result.queryLength == 500
            assert result.database == "nt"
            assert result.program == "blastn"
            assert len(result.hits) == 1

            hit = result.hits[0]
            assert hit.accession == "NC_001234"
            assert hit.score == 450.5
            assert hit.identity == 98.0  # 245/250 * 100
            assert hit.organism == "Homo sapiens"

    def test_parse_rid_success(self, settings_with_email, sample_blast_response):
        """_parse_rid extracts RID from response."""
        client = BlastClient(settings_with_email)

        rid = client._parse_rid(sample_blast_response)

        assert rid == "ABC123"

    def test_parse_rid_failure(self, settings_with_email):
        """_parse_rid raises on missing RID."""
        client = BlastClient(settings_with_email)

        with pytest.raises(BlastError, match="Could not parse RID"):
            client._parse_rid("No RID here")

    def test_parse_status_ready(self, settings_with_email, sample_status_ready):
        """_parse_status returns READY."""
        client = BlastClient(settings_with_email)

        status = client._parse_status(sample_status_ready)

        assert status == "READY"

    def test_parse_status_waiting(self, settings_with_email, sample_status_waiting):
        """_parse_status returns WAITING."""
        client = BlastClient(settings_with_email)

        status = client._parse_status(sample_status_waiting)

        assert status == "WAITING"

    def test_parse_status_with_hits(self, settings_with_email):
        """_parse_status returns READY when ThereAreHits=yes."""
        client = BlastClient(settings_with_email)

        status = client._parse_status("ThereAreHits=yes")

        assert status == "READY"

    def test_extract_organism_from_brackets(self, settings_with_email):
        """_extract_organism extracts organism from brackets."""
        client = BlastClient(settings_with_email)

        organism = client._extract_organism("Some gene [Homo sapiens]")

        assert organism == "Homo sapiens"

    def test_extract_organism_from_words(self, settings_with_email):
        """_extract_organism extracts genus species from words."""
        client = BlastClient(settings_with_email)

        organism = client._extract_organism("Escherichia coli strain K12")

        assert organism == "Escherichia coli"

    @pytest.mark.asyncio
    async def test_close(self, settings_with_email):
        """close closes HTTP client."""
        client = BlastClient(settings_with_email)

        # Create a mock client
        mock_http = AsyncMock()
        client._client = mock_http

        await client.close()

        mock_http.aclose.assert_called_once()
        assert client._client is None


class TestBlastJob:
    """Tests for BlastJob model."""

    def test_blast_job_creation(self):
        """BlastJob is created with defaults."""
        job = BlastJob(rid="TEST123")

        assert job.rid == "TEST123"
        assert job.status == "WAITING"
        assert job.program == "blastn"
        assert job.database == "nt"

    def test_blast_job_to_dict(self):
        """BlastJob serializes to dict."""
        job = BlastJob(rid="TEST123", program="blastp", database="nr")

        data = job.to_dict()

        assert data["rid"] == "TEST123"
        assert data["program"] == "blastp"
        assert data["database"] == "nr"
        assert "submittedAt" in data


class TestBlastSearchResult:
    """Tests for BlastSearchResult model."""

    def test_blast_result_creation(self):
        """BlastSearchResult is created with defaults."""
        result = BlastSearchResult(rid="TEST123")

        assert result.rid == "TEST123"
        assert result.hits == []
        assert result.queryLength == 0

    def test_hit_count_property(self):
        """hitCount returns number of hits."""
        from src.models import BlastHit

        result = BlastSearchResult(rid="TEST")
        result.hits = [
            BlastHit(
                accession="A", description="", score=0, eValue=0, identity=0,
                queryStart=1, queryEnd=100, subjectStart=1, subjectEnd=100
            ),
            BlastHit(
                accession="B", description="", score=0, eValue=0, identity=0,
                queryStart=1, queryEnd=100, subjectStart=1, subjectEnd=100
            ),
        ]

        assert result.hitCount == 2

    def test_top_hit_property(self):
        """topHit returns first hit."""
        from src.models import BlastHit

        hit = BlastHit(
            accession="A", description="", score=0, eValue=0, identity=0,
            queryStart=1, queryEnd=100, subjectStart=1, subjectEnd=100
        )
        result = BlastSearchResult(rid="TEST")
        result.hits = [hit]

        assert result.topHit is hit

    def test_top_hit_none(self):
        """topHit returns None when no hits."""
        result = BlastSearchResult(rid="TEST")

        assert result.topHit is None

    def test_to_dict(self):
        """BlastSearchResult serializes to dict."""
        result = BlastSearchResult(
            rid="TEST",
            queryLength=500,
            database="nt",
            program="blastn",
        )

        data = result.to_dict()

        assert data["rid"] == "TEST"
        assert data["queryLength"] == 500
        assert data["hitCount"] == 0
        assert "completedAt" in data
