/**
 * AI Service — talks to the GeneFlow AI Python backend (FastAPI, port 8090).
 *
 * This is a separate service from `api-client.ts` because the AI service is
 * a different process with a different base URL. It exposes the provider-
 * agnostic LLMAgent (Claude / DeepSeek / Ollama) and the molecular biology
 * tools (parsing, alignment, variants, phylogeny...).
 *
 * Base URL is configurable via `NEXT_PUBLIC_AI_API_URL`
 * (default: `http://localhost:8090`).
 */

const AI_BASE_URL =
  process.env.NEXT_PUBLIC_AI_API_URL || "http://localhost:8090";

// =============================================================================
// Types
// =============================================================================

export type LlmProvider = "claude" | "deepseek" | "ollama" | (string & {});

/** Parsed trace data passed to the agent as conversational context. */
export interface TraceContext {
  traceId: string;
  name?: string;
  format?: string;
  length: number;
  averageQuality?: number;
  gcContent?: number;
  bases: string;
  /** Optional Phred quality scores, one per base. */
  qualityScores?: number[];
  /** Optional active trims summary. */
  trims?: Array<{
    end: "5'" | "3'";
    start: number;
    stop: number;
    length: number;
    reason?: string;
  }>;
  /** Optional user annotations. */
  annotations?: Array<{
    label: string;
    start: number;
    end: number;
    type?: string;
    description?: string;
  }>;
}

/** Study-level data passed to the agent so it can answer questions about
 * the study as a whole (metadata, team, papers, trace counts...). */
export interface StudyContext {
  studyId: string;
  title: string;
  description?: string;
  researchField?: string;
  status?: string;
  institution?: string;
  principalInvestigator?: string;
  tags?: string[];
  memberCount?: number;
  members?: Array<{ name?: string; role?: string }>;
  paperCount?: number;
  papers?: Array<{ title: string; authors?: string; year?: number }>;
  traceCounts?: {
    total?: number;
    processed?: number;
    processing?: number;
    failed?: number;
  };
  viewsCount?: number;
  starsCount?: number;
  createdAt?: string;
  /** Optional README markdown so the agent can summarise the study's notes. */
  readmeMarkdown?: string;
}

export interface LlmAskRequest {
  question: string;
  contextId?: string;
  provider?: LlmProvider;
  /** When provided, the service prepends a structured preamble to the
   * question describing the active trace. The backend will see one user
   * message containing both the trace block and the user's question. */
  traceContext?: TraceContext | null;
  /** When provided, the service prepends a structured preamble describing
   * the study so the agent can answer questions about it in general. */
  studyContext?: StudyContext | null;
}

export interface LlmToolCallInfo {
  iteration: number;
  name: string;
  arguments?: unknown;
  result?: unknown;
}

export interface LlmAskResponse {
  answer: string;
  contextId: string;
  provider: string;
  model: string;
  iterations: number;
  toolCalls: LlmToolCallInfo[];
  metrics: Record<string, unknown>;
  error: boolean;
}

export interface LlmProviderInfo {
  provider: string;
  model: string;
  available: boolean;
  isDefault: boolean;
  metrics: Record<string, unknown>;
}

export interface LlmProvidersResponse {
  providers: LlmProviderInfo[];
  default: string;
}

// =============================================================================
// Low-level fetch helper (no auth — the AI service runs internal-only for now)
// =============================================================================

class AiServiceError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = "AiServiceError";
  }
}

type AiFetchInit = Omit<RequestInit, "body"> & { body?: unknown };

async function aiFetch<T>(endpoint: string, init?: AiFetchInit): Promise<T> {
  const { body, headers, ...rest } = init || {};
  const config: RequestInit = {
    ...rest,
    headers: {
      "Content-Type": "application/json",
      ...(headers || {}),
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  };

  const resp = await fetch(`${AI_BASE_URL}${endpoint}`, config);
  if (!resp.ok) {
    let detail = `AI service error ${resp.status}`;
    try {
      const j = await resp.json();
      if (j?.detail) detail = `${detail}: ${j.detail}`;
    } catch {
      // ignore
    }
    throw new AiServiceError(resp.status, detail);
  }
  if (resp.status === 204) return undefined as T;
  return resp.json();
}

// =============================================================================
// Trace preamble builder
// =============================================================================

const MAX_BASES_INLINE = 4000;
const MAX_QUALITY_INLINE = 2000;

/**
 * Build the structured trace preamble that goes in front of the user's
 * first question. The model reads this as a tool-input prelude and can
 * use the contained sequence with the registered tools (translate,
 * align_pairwise, detect_variants_from_alignment, etc.).
 */
export function buildTracePreamble(ctx: TraceContext): string {
  const lines: string[] = [];
  lines.push("=== TRACE CONTEXT (active chromatogram in the viewer) ===");
  lines.push(`traceId: ${ctx.traceId}`);
  if (ctx.name) lines.push(`name: ${ctx.name}`);
  if (ctx.format) lines.push(`format: ${ctx.format}`);
  lines.push(`length: ${ctx.length} bp`);
  if (typeof ctx.averageQuality === "number") {
    lines.push(`averageQuality: Q${ctx.averageQuality.toFixed(1)}`);
  }
  if (typeof ctx.gcContent === "number") {
    lines.push(`gcContent: ${(ctx.gcContent * 100).toFixed(1)}%`);
  }

  if (ctx.trims && ctx.trims.length > 0) {
    lines.push(`activeTrims: ${ctx.trims.length}`);
    for (const t of ctx.trims.slice(0, 6)) {
      lines.push(
        `  - ${t.end} [${t.start}..${t.stop}] len=${t.length}` +
          (t.reason ? ` (${t.reason})` : "")
      );
    }
  }

  if (ctx.annotations && ctx.annotations.length > 0) {
    lines.push(`annotations: ${ctx.annotations.length}`);
    for (const a of ctx.annotations.slice(0, 10)) {
      lines.push(`  - "${a.label}" [${a.start}..${a.end}]`);
    }
  }

  // Bases — truncate ultra-long sequences to keep prompts manageable
  if (ctx.bases) {
    const seq = ctx.bases;
    if (seq.length <= MAX_BASES_INLINE) {
      lines.push("bases:");
      lines.push(seq);
    } else {
      lines.push(
        `bases (TRUNCATED, full length ${seq.length} bp — first ${MAX_BASES_INLINE} bases shown):`
      );
      lines.push(seq.slice(0, MAX_BASES_INLINE));
    }
  }

  // Quality scores summary
  if (ctx.qualityScores && ctx.qualityScores.length > 0) {
    const q = ctx.qualityScores;
    if (q.length <= MAX_QUALITY_INLINE) {
      lines.push(`qualityScores (${q.length} values): [${q.join(",")}]`);
    } else {
      const head = q.slice(0, 50).join(",");
      const tail = q.slice(-50).join(",");
      const avg = q.reduce((a, b) => a + b, 0) / q.length;
      const q20pct = (q.filter((s) => s >= 20).length / q.length) * 100;
      lines.push(
        `qualityScores (${q.length} values, avg=${avg.toFixed(
          1
        )}, %Q20=${q20pct.toFixed(1)}%, head/tail shown):`
      );
      lines.push(`  head: [${head}]`);
      lines.push(`  tail: [${tail}]`);
    }
  }

  lines.push("=== END TRACE CONTEXT ===");
  return lines.join("\n");
}

/**
 * Build the structured study preamble that goes in front of the user's
 * first question so the agent can answer questions about the study in
 * general (metadata, team, papers, sequencing progress...).
 */
export function buildStudyPreamble(ctx: StudyContext): string {
  const lines: string[] = [];
  lines.push("=== STUDY CONTEXT (the study the user is viewing) ===");
  lines.push(`studyId: ${ctx.studyId}`);
  lines.push(`title: ${ctx.title}`);
  if (ctx.description) lines.push(`description: ${ctx.description}`);
  if (ctx.researchField) lines.push(`researchField: ${ctx.researchField}`);
  if (ctx.status) lines.push(`status: ${ctx.status}`);
  if (ctx.institution) lines.push(`institution: ${ctx.institution}`);
  if (ctx.principalInvestigator)
    lines.push(`principalInvestigator: ${ctx.principalInvestigator}`);
  if (ctx.tags && ctx.tags.length > 0)
    lines.push(`tags: ${ctx.tags.join(", ")}`);
  if (ctx.createdAt) lines.push(`createdAt: ${ctx.createdAt}`);
  if (typeof ctx.viewsCount === "number") lines.push(`views: ${ctx.viewsCount}`);
  if (typeof ctx.starsCount === "number") lines.push(`stars: ${ctx.starsCount}`);

  if (typeof ctx.memberCount === "number")
    lines.push(`memberCount: ${ctx.memberCount}`);
  if (ctx.members && ctx.members.length > 0) {
    lines.push("members:");
    for (const m of ctx.members.slice(0, 25)) {
      lines.push(`  - ${m.name ?? "(unknown)"}${m.role ? ` (${m.role})` : ""}`);
    }
  }

  if (ctx.traceCounts) {
    const tc = ctx.traceCounts;
    const parts: string[] = [];
    if (typeof tc.total === "number") parts.push(`total=${tc.total}`);
    if (typeof tc.processed === "number") parts.push(`processed=${tc.processed}`);
    if (typeof tc.processing === "number")
      parts.push(`processing=${tc.processing}`);
    if (typeof tc.failed === "number") parts.push(`failed=${tc.failed}`);
    if (parts.length > 0) lines.push(`traces: ${parts.join(", ")}`);
  }

  if (typeof ctx.paperCount === "number")
    lines.push(`paperCount: ${ctx.paperCount}`);
  if (ctx.papers && ctx.papers.length > 0) {
    lines.push("papers:");
    for (const p of ctx.papers.slice(0, 15)) {
      lines.push(
        `  - "${p.title}"${p.authors ? ` — ${p.authors}` : ""}` +
          (p.year ? ` (${p.year})` : "")
      );
    }
  }

  if (ctx.readmeMarkdown) {
    const readme =
      ctx.readmeMarkdown.length > 4000
        ? `${ctx.readmeMarkdown.slice(0, 4000)}\n…(truncated)`
        : ctx.readmeMarkdown;
    lines.push("readme:");
    lines.push(readme);
  }

  lines.push("=== END STUDY CONTEXT ===");
  return lines.join("\n");
}

// =============================================================================
// Public service
// =============================================================================

export const aiService = {
  /**
   * Ask the molecular biology agent a question. When `traceContext` is
   * provided, it is prepended to the question as a structured preamble.
   *
   * The server returns a stable `contextId` — pass it back on every
   * subsequent call to keep the conversation alive.
   */
  async ask(req: LlmAskRequest): Promise<LlmAskResponse> {
    const preambles: string[] = [];
    if (req.studyContext) preambles.push(buildStudyPreamble(req.studyContext));
    if (req.traceContext) preambles.push(buildTracePreamble(req.traceContext));

    const userMessage =
      preambles.length > 0
        ? `${preambles.join("\n\n")}\n\nUSER QUESTION:\n${req.question}`
        : req.question;

    return aiFetch<LlmAskResponse>("/llm/ask", {
      method: "POST",
      body: {
        question: userMessage,
        contextId: req.contextId,
        provider: req.provider,
      },
    });
  },

  /** Return the providers configured on the backend at startup. */
  async listProviders(): Promise<LlmProvidersResponse> {
    return aiFetch<LlmProvidersResponse>("/llm/providers", { method: "GET" });
  },

  /** Discard a conversation context on the server. */
  async deleteContext(contextId: string): Promise<void> {
    await aiFetch<void>(`/llm/contexts/${encodeURIComponent(contextId)}`, {
      method: "DELETE",
    });
  },

  /** Backend agent status snapshot. */
  async status(): Promise<{
    available: boolean;
    default: string;
    providers: string[];
    activeContexts: number;
  }> {
    return aiFetch("/llm/status", { method: "GET" });
  },
};

export { AiServiceError };
