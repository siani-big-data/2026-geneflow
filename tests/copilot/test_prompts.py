"""Tests for prompt templates."""


from src.copilot.prompts import PromptTemplates


class TestPromptTemplates:
    """Tests for PromptTemplates."""

    def test_render_substitutes_values(self):
        """render substitutes placeholders with values."""
        template = "Hello {name}, your score is {score}."
        result = PromptTemplates.render(template, name="Alice", score=100)

        assert result == "Hello Alice, your score is 100."

    def test_render_handles_missing_keys(self):
        """render handles missing keys gracefully."""
        template = "Hello {name}, your {missing} is ready."
        result = PromptTemplates.render(template, name="Bob")

        assert result == "Hello Bob, your [missing no disponible] is ready."

    def test_format_quality_section_with_data(self):
        """format_quality_section formats quality data."""
        quality = {
            "predictedAccuracy": 0.95,
            "errorProbability": 0.05,
            "suggestedTrimStart": 10,
            "suggestedTrimEnd": 500,
            "confidence": "high",
        }

        result = PromptTemplates.format_quality_section(quality)

        assert "0.95" in result
        assert "0.05" in result
        assert "10" in result
        assert "500" in result
        assert "high" in result

    def test_format_quality_section_without_data(self):
        """format_quality_section handles None."""
        result = PromptTemplates.format_quality_section(None)

        assert "No hay datos" in result

    def test_format_blast_section_with_hits(self):
        """format_blast_section formats BLAST hits."""
        hits = [
            {
                "accession": "NC_001234",
                "description": "Test gene",
                "identity": 99.5,
                "eValue": 0.001,
                "organism": "Homo sapiens",
            }
        ]

        result = PromptTemplates.format_blast_section(hits)

        assert "NC_001234" in result
        assert "Test gene" in result
        assert "99.5" in result
        assert "0.001" in result
        assert "Homo sapiens" in result

    def test_format_blast_section_limits_to_5_hits(self):
        """format_blast_section shows only top 5 hits."""
        hits = [{"accession": f"ACC_{i}", "description": f"Hit {i}"} for i in range(10)]

        result = PromptTemplates.format_blast_section(hits)

        assert "ACC_0" in result
        assert "ACC_4" in result
        assert "ACC_5" not in result

    def test_format_blast_section_without_hits(self):
        """format_blast_section handles None."""
        result = PromptTemplates.format_blast_section(None)

        assert "No se realizó" in result or "no hay" in result.lower()

    def test_format_variants_section_with_variants(self):
        """format_variants_section formats variants."""
        variants = [
            {
                "position": 100,
                "referenceBase": "A",
                "alternateBase": "G",
                "variantType": "SNP",
                "clinicalSignificance": "pathogenic",
            }
        ]

        result = PromptTemplates.format_variants_section(variants)

        assert "100" in result
        assert "A" in result
        assert "G" in result
        assert "SNP" in result
        assert "pathogenic" in result

    def test_format_variants_section_without_variants(self):
        """format_variants_section handles None."""
        result = PromptTemplates.format_variants_section(None)

        assert "No se detectaron" in result

    def test_format_annotations_section_with_annotations(self):
        """format_annotations_section formats annotations."""
        annotations = [
            {
                "start": 1,
                "end": 100,
                "featureType": "exon",
                "name": "Exon 1",
            }
        ]

        result = PromptTemplates.format_annotations_section(annotations)

        assert "1-100" in result
        assert "exon" in result
        assert "Exon 1" in result

    def test_format_annotations_section_without_annotations(self):
        """format_annotations_section handles None."""
        result = PromptTemplates.format_annotations_section(None)

        assert "No se detectaron" in result

    def test_system_prompts_exist(self):
        """System prompts are defined."""
        assert PromptTemplates.SYSTEM_ANALYST
        assert PromptTemplates.SYSTEM_REPORTER
        assert PromptTemplates.SYSTEM_QA

    def test_user_prompts_exist(self):
        """User prompts are defined."""
        assert PromptTemplates.INTERPRET_ANALYSIS
        assert PromptTemplates.GENERATE_REPORT
        assert PromptTemplates.ANSWER_QUESTION
        assert PromptTemplates.EXPLAIN_VARIANT
        assert PromptTemplates.SUMMARIZE_BLAST
