"""Report generator for GeneFlow AI Copilot."""

from datetime import datetime, timezone
from typing import Any, Optional

import structlog

from src.config import Settings
from src.models import AnalysisResult

from .client import ClaudeClient
from .prompts import PromptTemplates

logger = structlog.get_logger()


class ReportGenerator:
    """Generates analysis reports using Claude API."""

    def __init__(self, client: ClaudeClient, settings: Settings):
        self._client = client
        self._settings = settings

    @property
    def is_available(self) -> bool:
        """Check if report generation is available."""
        return self._client.is_configured

    async def generate_report(
        self,
        analysis_result: AnalysisResult,
        format: str = "markdown",
        language: str = "es",
    ) -> dict[str, Any]:
        """
        Generate a comprehensive analysis report.

        Args:
            analysis_result: The analysis result to report on
            format: Output format (currently only 'markdown' supported)
            language: Report language ('es' or 'en')

        Returns:
            Dict with report content and metadata
        """
        if not self._client.is_configured:
            return self._generate_fallback_report(analysis_result)

        # Build analysis data summary
        analysis_data = self._format_analysis_data(analysis_result)

        # Build prompt
        prompt = PromptTemplates.render(
            PromptTemplates.GENERATE_REPORT,
            analysis_data=analysis_data,
        )

        try:
            report_content = await self._client.create_message(
                system_prompt=PromptTemplates.SYSTEM_REPORTER,
                user_message=prompt,
            )

            logger.info(
                "report_generated",
                trace_id=analysis_result.traceId,
                analysis_id=analysis_result.analysisId,
                report_length=len(report_content),
            )

            return {
                "content": report_content,
                "format": format,
                "generatedAt": datetime.now(timezone.utc).isoformat(),
                "analysisId": analysis_result.analysisId,
                "traceId": analysis_result.traceId,
            }

        except Exception as e:
            logger.error("report_generation_error", error=str(e))
            return self._generate_fallback_report(analysis_result, error=str(e))

    async def generate_summary(
        self,
        analysis_result: AnalysisResult,
    ) -> str:
        """
        Generate a brief summary of the analysis.

        Args:
            analysis_result: The analysis result to summarize

        Returns:
            Summary text
        """
        if not self._client.is_configured:
            return self._generate_fallback_summary(analysis_result)

        # Build compact prompt
        prompt = f"""Resume en 2-3 oraciones los siguientes resultados de análisis:

Trace ID: {analysis_result.traceId}
Estado: {analysis_result.status.value}
Confianza: {analysis_result.overallConfidence}
BLAST hits: {len(analysis_result.blastHits)}
Variantes: {len(analysis_result.variants)}
Anotaciones: {len(analysis_result.annotations)}
Warnings: {len(analysis_result.warnings)}

Proporciona un resumen ejecutivo conciso."""

        try:
            summary = await self._client.create_message(
                system_prompt="Eres un asistente de bioinformática. Responde de forma muy concisa.",
                user_message=prompt,
                max_tokens=256,
            )

            return summary

        except Exception as e:
            logger.error("summary_generation_error", error=str(e))
            return self._generate_fallback_summary(analysis_result)

    def _format_analysis_data(self, result: AnalysisResult) -> str:
        """Format analysis result for report generation."""
        lines = [
            "## Información General",
            f"- **Analysis ID:** {result.analysisId}",
            f"- **Trace ID:** {result.traceId}",
            f"- **Study ID:** {result.studyId}",
            f"- **Estado:** {result.status.value}",
            f"- **Tiempo de procesamiento:** {result.processingTimeMs} ms",
            f"- **Confianza general:** {result.overallConfidence}",
            "",
        ]

        # Quality
        lines.append("## Calidad")
        if result.quality:
            q = result.quality
            lines.extend([
                f"- Precisión predicha: {q.predictedAccuracy}",
                f"- Probabilidad de error: {q.errorProbability}",
                f"- Trim sugerido: {q.suggestedTrimStart} - {q.suggestedTrimEnd}",
                f"- Regiones de baja calidad: {len(q.lowQualityRegions)}",
            ])
        else:
            lines.append("No hay datos de calidad disponibles.")
        lines.append("")

        # BLAST
        lines.append("## Resultados BLAST")
        if result.blastHits:
            for i, hit in enumerate(result.blastHits[:5], 1):
                lines.append(f"{i}. {hit.accession} - {hit.description}")
                lines.append(f"   - Identidad: {hit.identity}%, E-value: {hit.eValue}")
                if hit.organism:
                    lines.append(f"   - Organismo: {hit.organism}")
        else:
            lines.append("No hay resultados BLAST.")
        lines.append("")

        # Variants
        lines.append("## Variantes")
        if result.variants:
            for v in result.variants:
                lines.append(
                    f"- Pos {v.position}: {v.referenceBase}→{v.alternateBase} "
                    f"({v.variantType}, {v.clinicalSignificance or 'N/A'})"
                )
        else:
            lines.append("No se detectaron variantes.")
        lines.append("")

        # Annotations
        lines.append("## Anotaciones")
        if result.annotations:
            for a in result.annotations:
                lines.append(f"- {a.featureType}: {a.start}-{a.end}")
        else:
            lines.append("No hay anotaciones.")
        lines.append("")

        # Warnings
        if result.warnings:
            lines.append("## Advertencias")
            for w in result.warnings:
                lines.append(f"- {w}")
            lines.append("")

        return "\n".join(lines)

    def _generate_fallback_report(
        self,
        result: AnalysisResult,
        error: Optional[str] = None,
    ) -> dict[str, Any]:
        """Generate a basic report without AI."""
        content = self._format_analysis_data(result)

        if error:
            content = f"*Nota: Reporte generado sin asistencia de IA ({error})*\n\n" + content

        return {
            "content": content,
            "format": "markdown",
            "generatedAt": datetime.now(timezone.utc).isoformat(),
            "analysisId": result.analysisId,
            "traceId": result.traceId,
            "fallback": True,
        }

    def _generate_fallback_summary(self, result: AnalysisResult) -> str:
        """Generate a basic summary without AI."""
        parts = [f"Análisis {result.analysisId} completado."]

        if result.blastHits:
            top = result.blastHits[0]
            parts.append(f"Top hit: {top.accession} ({top.identity}% identidad).")

        if result.variants:
            parts.append(f"{len(result.variants)} variante(s) detectada(s).")

        if result.warnings:
            parts.append(f"{len(result.warnings)} advertencia(s).")

        return " ".join(parts)
