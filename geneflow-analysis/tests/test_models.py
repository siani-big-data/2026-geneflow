"""Tests for domain models."""

from src.models import (
    ORF,
    AlignmentJob,
    AlignmentResult,
    AlignmentType,
    AnalysisJob,
    AnalysisType,
    ChromatogramData,
    HeterozygoteCall,
    MotifMatch,
    ParsedTrace,
    QualityMetrics,
    RestrictionSite,
    Sequence,
    TraceFormat,
    TraceProcessingJob,
    TrimmingAlgorithm,
    TrimmingResult,
    Variant,
)


class TestSequence:
    """Tests for Sequence model."""

    def test_create_sequence(self, sample_sequence: str):
        seq = Sequence(id="test-1", sequence=sample_sequence, name="Test Sequence")
        assert seq.id == "test-1"
        assert seq.sequence == sample_sequence
        assert len(seq) == len(sample_sequence)

    def test_sequence_with_quality(self, sample_sequence: str, sample_quality: list[int]):
        seq = Sequence(
            id="test-1",
            sequence=sample_sequence[: len(sample_quality)],
            quality=sample_quality,
        )
        assert seq.quality == sample_quality

    def test_sequence_to_dict(self, sample_sequence: str):
        seq = Sequence(id="test-1", sequence=sample_sequence)
        data = seq.to_dict()
        assert data["id"] == "test-1"
        assert data["sequence"] == sample_sequence

    def test_sequence_from_dict(self, sample_sequence: str):
        data = {"id": "test-1", "sequence": sample_sequence, "name": "Test"}
        seq = Sequence.from_dict(data)
        assert seq.id == "test-1"
        assert seq.name == "Test"


class TestChromatogramData:
    """Tests for ChromatogramData model."""

    def test_create_chromatogram(self, sample_chromatogram_data: dict):
        chrom = ChromatogramData(**sample_chromatogram_data)
        assert len(chrom.traceA) == 40
        assert len(chrom.baseCalls) == 40

    def test_chromatogram_roundtrip(self, sample_chromatogram_data: dict):
        chrom = ChromatogramData(**sample_chromatogram_data)
        data = chrom.to_dict()
        chrom2 = ChromatogramData.from_dict(data)
        assert chrom.traceA == chrom2.traceA
        assert chrom.peakLocations == chrom2.peakLocations


class TestQualityMetrics:
    """Tests for QualityMetrics model."""

    def test_create_quality_metrics(self):
        metrics = QualityMetrics(
            meanQuality=35.5,
            q20Percentage=95.0,
            q30Percentage=85.0,
            gcContent=52.3,
            length=500,
        )
        assert metrics.meanQuality == 35.5
        assert metrics.q30Percentage == 85.0

    def test_quality_metrics_roundtrip(self):
        metrics = QualityMetrics(
            meanQuality=35.5,
            q20Percentage=95.0,
            q30Percentage=85.0,
            gcContent=52.3,
            length=500,
            ambiguousCount=3,
        )
        data = metrics.to_dict()
        metrics2 = QualityMetrics.from_dict(data)
        assert metrics.meanQuality == metrics2.meanQuality
        assert metrics.ambiguousCount == metrics2.ambiguousCount


class TestTrimmingResult:
    """Tests for TrimmingResult model."""

    def test_create_trimming_result(self):
        result = TrimmingResult(
            originalLength=500,
            trimmedLength=450,
            trimStart=25,
            trimEnd=475,
            trimmedSequence="ATCG" * 100,
            algorithm="modified_mott",
        )
        assert result.originalLength == 500
        assert result.trimmedLength == 450


class TestHeterozygoteCall:
    """Tests for HeterozygoteCall model."""

    def test_create_heterozygote(self):
        het = HeterozygoteCall(
            position=150,
            base1="A",
            base2="G",
            iupacCode="R",
            ratio=0.55,
            confidence=0.92,
        )
        assert het.iupacCode == "R"
        assert het.ratio == 0.55


class TestMotifMatch:
    """Tests for MotifMatch model."""

    def test_create_motif_match(self):
        match = MotifMatch(
            pattern="GAATTC",
            start=100,
            end=106,
            matchedSequence="GAATTC",
            strand="+",
        )
        assert match.pattern == "GAATTC"
        assert match.end - match.start == 6


class TestORF:
    """Tests for ORF model."""

    def test_create_orf(self):
        orf = ORF(
            start=0,
            end=300,
            frame=0,
            strand="+",
            length=300,
            sequence="ATG" + "GCT" * 98 + "TAA",
            proteinSequence="M" + "A" * 98 + "*",
        )
        assert orf.frame == 0
        assert orf.length == 300


class TestRestrictionSite:
    """Tests for RestrictionSite model."""

    def test_create_restriction_site(self):
        site = RestrictionSite(
            enzyme="EcoRI",
            position=100,
            cutPosition=101,
            recognitionSequence="GAATTC",
            overhang="5'",
        )
        assert site.enzyme == "EcoRI"


class TestAlignmentResult:
    """Tests for AlignmentResult model."""

    def test_create_alignment_result(self):
        result = AlignmentResult(
            alignmentId="align-1",
            type=AlignmentType.PAIRWISE,
            sequences=["ATCGATCG", "ATCGATCG"],
            alignedSequences=["ATCGATCG", "ATCGATCG"],
            score=100.0,
            identity=100.0,
            gaps=0,
            consensus="ATCGATCG",
        )
        assert result.identity == 100.0
        assert result.consensus == "ATCGATCG"


class TestVariant:
    """Tests for Variant model."""

    def test_create_snp(self):
        var = Variant(
            position=50,
            referenceBase="A",
            alternateBase="G",
            variantType="snp",
            quality=30.0,
        )
        assert var.variantType == "snp"


class TestParsedTrace:
    """Tests for ParsedTrace model."""

    def test_create_parsed_trace(self, sample_sequence: str):
        trace = ParsedTrace(
            traceId="trace-1",
            format=TraceFormat.AB1,
            sequence=Sequence(id="seq-1", sequence=sample_sequence),
        )
        assert trace.format == TraceFormat.AB1


class TestTraceProcessingJob:
    """Tests for TraceProcessingJob model."""

    def test_create_job(self):
        job = TraceProcessingJob(
            traceId="trace-1",
            studyId="study-1",
            fileName="sample.ab1",
            storagePath="traces/sample.ab1",
            format=TraceFormat.AB1,
        )
        assert job.format == TraceFormat.AB1

    def test_job_from_dict(self):
        data = {
            "traceId": "trace-1",
            "studyId": "study-1",
            "fileName": "sample.ab1",
            "storagePath": "traces/sample.ab1",
            "formatName": "AB1",
        }
        job = TraceProcessingJob.from_dict(data)
        assert job.format == TraceFormat.AB1


class TestAlignmentJob:
    """Tests for AlignmentJob model."""

    def test_create_alignment_job(self):
        job = AlignmentJob(
            alignmentId="align-1",
            type=AlignmentType.PAIRWISE,
            traceIds=["trace-1", "trace-2"],
        )
        assert len(job.traceIds) == 2

    def test_alignment_job_from_dict_with_type_id(self):
        data = {
            "alignmentId": "align-1",
            "typeId": 1,
            "traceIds": ["trace-1", "trace-2"],
        }
        job = AlignmentJob.from_dict(data)
        assert job.type == AlignmentType.PAIRWISE


class TestAnalysisJob:
    """Tests for AnalysisJob model."""

    def test_create_analysis_job(self):
        job = AnalysisJob(
            traceId="trace-1",
            analysisType=AnalysisType.TRIMMING,
            sequence="ATCGATCG",
            options={"algorithm": "modified_mott"},
        )
        assert job.analysisType == AnalysisType.TRIMMING


class TestEnums:
    """Tests for enum values."""

    def test_trace_format_values(self):
        assert TraceFormat.AB1.value == "ab1"
        assert TraceFormat.FASTQ.value == "fastq"

    def test_alignment_type_values(self):
        assert AlignmentType.PAIRWISE.value == "pairwise"
        assert AlignmentType.MULTIPLE.value == "multiple"

    def test_trimming_algorithm_values(self):
        assert TrimmingAlgorithm.MODIFIED_MOTT.value == "modified_mott"
        assert TrimmingAlgorithm.SLIDING_WINDOW.value == "sliding_window"

    def test_analysis_type_values(self):
        assert AnalysisType.QUALITY.value == "quality"
        assert AnalysisType.HETEROZYGOTE.value == "heterozygote"
