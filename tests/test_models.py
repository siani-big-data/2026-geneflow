"""Tests for domain models."""


from src.models import (
    AnalysisResult,
    AnalysisStatus,
    BlastHit,
    QualityPrediction,
    SequenceAnnotation,
    VariantCall,
)


class TestBlastHit:
    """Test BlastHit model."""

    def test_to_dict(self):
        """Test serialization to dict."""
        hit = BlastHit(
            accession="NM_001234.5",
            description="Test gene",
            score=100.0,
            eValue=1e-50,
            identity=99.5,
            queryStart=1,
            queryEnd=100,
            subjectStart=1,
            subjectEnd=100,
            organism="Homo sapiens",
        )

        data = hit.to_dict()

        assert data["accession"] == "NM_001234.5"
        assert data["eValue"] == 1e-50
        assert data["organism"] == "Homo sapiens"

    def test_from_dict(self):
        """Test deserialization from dict."""
        data = {
            "accession": "NM_001234.5",
            "description": "Test gene",
            "score": 100.0,
            "eValue": 1e-50,
            "identity": 99.5,
            "queryStart": 1,
            "queryEnd": 100,
            "subjectStart": 1,
            "subjectEnd": 100,
        }

        hit = BlastHit.from_dict(data)

        assert hit.accession == "NM_001234.5"
        assert hit.organism is None


class TestQualityPrediction:
    """Test QualityPrediction model."""

    def test_to_dict(self):
        """Test serialization."""
        prediction = QualityPrediction(
            predictedAccuracy=0.95,
            errorProbability=0.05,
            lowQualityRegions=[(0, 10), (90, 100)],
            suggestedTrimStart=10,
            suggestedTrimEnd=90,
            confidence=0.88,
        )

        data = prediction.to_dict()

        assert data["predictedAccuracy"] == 0.95
        assert len(data["lowQualityRegions"]) == 2

    def test_from_dict(self):
        """Test deserialization."""
        data = {
            "predictedAccuracy": 0.95,
            "errorProbability": 0.05,
            "lowQualityRegions": [[0, 10], [90, 100]],
            "suggestedTrimStart": 10,
            "suggestedTrimEnd": 90,
            "confidence": 0.88,
        }

        prediction = QualityPrediction.from_dict(data)

        assert prediction.predictedAccuracy == 0.95
        assert prediction.lowQualityRegions == [(0, 10), (90, 100)]


class TestVariantCall:
    """Test VariantCall model."""

    def test_snp_variant(self):
        """Test SNP variant."""
        variant = VariantCall(
            position=100,
            referenceBase="A",
            alternateBase="G",
            variantType="snp",
            clinicalSignificance="benign",
            confidence=0.95,
        )

        data = variant.to_dict()

        assert data["variantType"] == "snp"
        assert data["clinicalSignificance"] == "benign"


class TestSequenceAnnotation:
    """Test SequenceAnnotation model."""

    def test_annotation(self):
        """Test sequence annotation."""
        annotation = SequenceAnnotation(
            start=100,
            end=500,
            featureType="ORF",
            strand="+",
            name="ORF_1",
            description="Predicted ORF",
            confidence=0.85,
            source="blast",
        )

        data = annotation.to_dict()

        assert data["featureType"] == "ORF"
        assert data["source"] == "blast"


class TestAnalysisResult:
    """Test AnalysisResult model."""

    def test_empty_result(self):
        """Test empty analysis result."""
        result = AnalysisResult(
            traceId="tr-123",
            studyId="st-456",
        )

        assert result.status == AnalysisStatus.PENDING
        assert result.blastHits == []
        assert result.variants == []

    def test_to_dict_with_results(self):
        """Test serialization with results."""
        result = AnalysisResult(
            traceId="tr-123",
            studyId="st-456",
            status=AnalysisStatus.COMPLETED,
            summary="Test summary",
            recommendations=["Recommendation 1"],
        )

        data = result.to_dict()

        assert data["status"] == "completed"
        assert data["summary"] == "Test summary"
        assert "recommendations" in data

    def test_from_dict(self):
        """Test deserialization."""
        data = {
            "analysisId": "ana-123",
            "traceId": "tr-123",
            "studyId": "st-456",
            "status": "completed",
            "processingTimeMs": 1500,
            "overallConfidence": 0.9,
        }

        result = AnalysisResult.from_dict(data)

        assert result.analysisId == "ana-123"
        assert result.status == AnalysisStatus.COMPLETED
