"""Prompt templates for GeneFlow AI Copilot."""

from dataclasses import dataclass
from typing import Any, Optional


@dataclass
class PromptTemplates:
    """Collection of prompt templates for different analysis tasks."""

    # System prompts
    SYSTEM_ANALYST = """Eres un experto en bioinformática y análisis de secuencias de ADN.
Tu rol es analizar secuencias de Sanger y proporcionar interpretaciones claras y precisas.

Contexto del análisis:
- Organismo objetivo: {organism}
- Longitud de secuencia: {sequence_length} bp
- Calidad promedio: {avg_quality}

Directrices:
- Responde siempre en español
- Sé conciso pero completo
- Incluye recomendaciones prácticas cuando sea apropiado
- Si detectas problemas potenciales, menciónalos claramente
- Usa terminología técnica apropiada"""

    SYSTEM_REPORTER = """Eres un experto en bioinformática especializado en reportes.
Tu rol es crear reportes claros y estructurados sobre análisis de secuencias.

Directrices:
- Genera reportes en formato Markdown
- Incluye secciones claramente definidas
- Usa tablas cuando sea apropiado
- Proporciona conclusiones accionables
- Responde en español"""

    SYSTEM_QA = """Eres un asistente experto en bioinformática.
Tu rol es responder preguntas sobre análisis de secuencias de ADN de forma clara y precisa.

Directrices:
- Responde de forma directa y técnica
- Si no tienes información suficiente, indícalo
- Proporciona contexto relevante cuando sea útil
- Responde en español"""

    # User prompt templates
    INTERPRET_ANALYSIS = """Analiza los siguientes resultados y proporciona una interpretación:

## Datos del Análisis

**Trace ID:** {trace_id}
**Estudio:** {study_id}

### Calidad
{quality_section}

### Resultados BLAST
{blast_section}

### Variantes Detectadas
{variants_section}

### Anotaciones
{annotations_section}

---

Por favor proporciona:
1. Un resumen ejecutivo (2-3 oraciones)
2. Interpretación de los hallazgos principales
3. Posibles implicaciones
4. Recomendaciones para siguientes pasos"""

    GENERATE_REPORT = """Genera un reporte técnico basado en los siguientes datos:

{analysis_data}

El reporte debe incluir:
1. **Resumen Ejecutivo** - Breve descripción de los hallazgos principales
2. **Métricas de Calidad** - Evaluación de la calidad de la secuencia
3. **Identificación** - Resultados de BLAST y organismo identificado
4. **Variantes** - Lista de variantes detectadas y su significado
5. **Anotaciones** - Características identificadas en la secuencia
6. **Conclusiones** - Síntesis de los resultados
7. **Recomendaciones** - Próximos pasos sugeridos

Formato: Markdown estructurado con tablas donde sea apropiado."""

    ANSWER_QUESTION = """Contexto del análisis:
{context}

---

Pregunta del usuario:
{question}

---

Proporciona una respuesta clara y técnica basada en el contexto disponible."""

    EXPLAIN_VARIANT = """Explica la siguiente variante detectada:

**Posición:** {position}
**Referencia:** {reference_base}
**Alternativa:** {alternate_base}
**Tipo:** {variant_type}
**Significancia clínica:** {clinical_significance}

Proporciona:
1. Descripción de la variante
2. Posible impacto funcional
3. Relevancia clínica (si aplica)
4. Recomendaciones"""

    SUMMARIZE_BLAST = """Resume los siguientes resultados de BLAST:

{blast_hits}

Proporciona:
1. Identificación del organismo más probable
2. Nivel de confianza en la identificación
3. Genes o regiones identificadas
4. Observaciones relevantes"""

    @staticmethod
    def render(template: str, **kwargs: Any) -> str:
        """
        Render a prompt template with variables.

        Args:
            template: Template string with {placeholders}
            **kwargs: Values to substitute

        Returns:
            Rendered prompt string
        """
        # Replace missing keys with placeholder text
        import re

        def replace_missing(match):
            key = match.group(1)
            return kwargs.get(key, f"[{key} no disponible]")

        # First substitute known values
        result = template
        for key, value in kwargs.items():
            result = result.replace(f"{{{key}}}", str(value))

        # Then handle any remaining placeholders
        result = re.sub(r"\{(\w+)\}", replace_missing, result)

        return result

    @staticmethod
    def format_quality_section(quality: Optional[dict]) -> str:
        """Format quality_enhanced data for prompts."""
        if not quality:
            return "No hay datos de calidad disponibles."

        return f"""- Precisión predicha: {quality.get("predictedAccuracy", "N/A")}
- Probabilidad de error: {quality.get("errorProbability", "N/A")}
- Trim sugerido: {quality.get("suggestedTrimStart", 0)} - {quality.get("suggestedTrimEnd", "N/A")}
- Confianza: {quality.get("confidence", "N/A")}"""

    @staticmethod
    def format_blast_section(blast_hits: Optional[list]) -> str:
        """Format BLAST hits for prompts."""
        if not blast_hits:
            return "No se realizó búsqueda BLAST o no hay resultados."

        lines = []
        for i, hit in enumerate(blast_hits[:5], 1):  # Top 5 hits
            acc = hit.get("accession", "N/A")
            desc = hit.get("description", "N/A")
            lines.append(f"{i}. **{acc}** - {desc}")
            lines.append(f"   - Identidad: {hit.get('identity', 'N/A')}%")
            lines.append(f"   - E-value: {hit.get('eValue', 'N/A')}")
            if hit.get("organism"):
                lines.append(f"   - Organismo: {hit.get('organism')}")

        return "\n".join(lines)

    @staticmethod
    def format_variants_section(variants: Optional[list]) -> str:
        """Format variants for prompts."""
        if not variants:
            return "No se detectaron variantes."

        lines = []
        for v in variants:
            pos = v.get("position", "N/A")
            ref = v.get("referenceBase", "N/A")
            alt = v.get("alternateBase", "N/A")
            vtype = v.get("variantType", "N/A")
            sig = v.get("clinicalSignificance", "desconocida")
            lines.append(f"- Pos {pos}: {ref} → {alt} ({vtype}, significancia: {sig})")

        return "\n".join(lines)

    @staticmethod
    def format_annotations_section(annotations: Optional[list]) -> str:
        """Format annotations for prompts."""
        if not annotations:
            return "No se detectaron anotaciones."

        lines = []
        for a in annotations:
            start = a.get("start", "N/A")
            end = a.get("end", "N/A")
            ftype = a.get("featureType", "N/A")
            name = a.get("name", "")
            lines.append(f"- {ftype}: {start}-{end}" + (f" ({name})" if name else ""))

        return "\n".join(lines)
