"""Tests for event serialization (to_dict + category for every event class)."""

import pytest

from src.events.events import (
    AlignmentCompleted,
    AlignmentFailed,
    AnalysisResultStored,
    BaseEvent,
    HeterozygoteDetectionCompleted,
    MotifSearchCompleted,
    ORFDetectionCompleted,
    PhylogenyCompleted,
    PhylogenyFailed,
    RestrictionAnalysisCompleted,
    TraceProcessed,
    TraceProcessingFailed,
    TranslationCompleted,
    TrimmingCompleted,
    WorkerStarted,
    WorkerStopped,
)


def _assert_envelope(payload: dict, event: BaseEvent):
    assert payload["eventId"] == event.eventId
    assert payload["type"]
    assert payload["category"]
    assert payload["source"] == "geneflow-analysis"
    assert payload["version"] == "1.0"
    assert "data" in payload


class TestEventSerialization:
    def test_base_event_category_raises(self):
        event = BaseEvent()
        with pytest.raises(NotImplementedError):
            _ = event.category

    def test_trace_processed(self):
        event = TraceProcessed(
            traceId="t-1",
            studyId="s-1",
            format="ab1",
            sequenceLength=10,
            meanQuality=33.5,
            hasChromatogram=True,
            hasQualityScores=True,
            chunkCount=2,
            parsedData={"chunks": []},
        )
        assert event.category == "traces"
        d = event.to_dict()
        _assert_envelope(d, event)

    def test_trace_processed_without_parsed_data(self):
        event = TraceProcessed(traceId="t", studyId="s", format="fasta")
        d = event.to_dict()
        assert d["category"] == "traces"

    def test_trace_processing_failed(self):
        event = TraceProcessingFailed(
            traceId="t", studyId="s", error="boom", errorType="ValueError"
        )
        assert event.category == "traces"
        _assert_envelope(event.to_dict(), event)

    def test_alignment_completed(self):
        event = AlignmentCompleted(
            alignmentId="a-1",
            type="pairwise",
            sequenceCount=2,
            score=99.5,
            identity=0.98,
            hasConsensus=True,
        )
        assert event.category == "alignments"
        _assert_envelope(event.to_dict(), event)

    def test_alignment_failed(self):
        event = AlignmentFailed(alignmentId="a-1", error="x", errorType="E")
        assert event.category == "alignments"
        _assert_envelope(event.to_dict(), event)

    def test_trimming_completed(self):
        event = TrimmingCompleted(
            traceId="t",
            algorithm="modified_mott",
            originalLength=100,
            trimmedLength=80,
            trimStart=5,
            trimEnd=85,
        )
        assert event.category == "analysis"
        _assert_envelope(event.to_dict(), event)

    def test_heterozygote_detection_completed(self):
        event = HeterozygoteDetectionCompleted(
            traceId="t", heterozygoteCount=2, positions=[10, 20]
        )
        assert event.category == "analysis"
        _assert_envelope(event.to_dict(), event)

    def test_motif_search_completed(self):
        event = MotifSearchCompleted(
            traceId="t", pattern="ACGT", matchCount=3, positions=[1, 5, 9]
        )
        assert event.category == "analysis"
        _assert_envelope(event.to_dict(), event)

    def test_translation_completed(self):
        event = TranslationCompleted(traceId="t", frame=1, proteinLength=12)
        assert event.category == "analysis"
        _assert_envelope(event.to_dict(), event)

    def test_orf_detection_completed(self):
        event = ORFDetectionCompleted(traceId="t", orfCount=3, longestOrfLength=120)
        assert event.category == "analysis"
        _assert_envelope(event.to_dict(), event)

    def test_restriction_analysis_completed(self):
        event = RestrictionAnalysisCompleted(
            traceId="t", enzymeCount=2, totalSites=4, enzymesWithSites=["EcoRI"]
        )
        assert event.category == "analysis"
        _assert_envelope(event.to_dict(), event)

    def test_analysis_result_stored(self):
        event = AnalysisResultStored(
            traceId="t", analysisType="quality", resultData={"score": 1}
        )
        assert event.category == "traces"
        _assert_envelope(event.to_dict(), event)

    def test_worker_started(self):
        event = WorkerStarted(workerName="w", workerId="id", enabledWorkers=["trace"])
        assert event.category == "system"
        _assert_envelope(event.to_dict(), event)

    def test_worker_stopped(self):
        event = WorkerStopped(workerName="w", workerId="id", reason="x", jobsProcessed=5)
        assert event.category == "system"
        _assert_envelope(event.to_dict(), event)

    def test_phylogeny_completed(self):
        event = PhylogenyCompleted(
            analysisId="p",
            alignmentId="a",
            sequenceCount=3,
            treeMethod="upgma",
            distanceMethod="jc",
            hasBootstrap=False,
            bootstrapReplicates=0,
        )
        assert event.category == "phylogeny"
        _assert_envelope(event.to_dict(), event)

    def test_phylogeny_failed(self):
        event = PhylogenyFailed(analysisId="p", error="boom", errorType="E")
        assert event.category == "phylogeny"
        _assert_envelope(event.to_dict(), event)
