"use client";

import { useState, useRef, useEffect } from "react";
import { Sparkles, X, Send, ChevronRight, Loader2, Wrench } from "lucide-react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { cn } from "@/lib/utils";
import { aiService, AiServiceError } from "@/services/ai.service";
import type {
  LlmProvider,
  LlmToolCallInfo,
  TraceContext,
  StudyContext,
} from "@/services/ai.service";
import {profileService} from "@/services";
import {useAuthStore} from "@/stores/auth-store";

/** localStorage key + bounds for the resizable panel width. */
const PANEL_WIDTH_KEY = "geneflow:ai-assistant:width";
const DEFAULT_WIDTH = 460;
const MIN_WIDTH = 320;
const MAX_WIDTH = 1100;

const clampWidth = (n: number): number =>
  Math.min(MAX_WIDTH, Math.max(MIN_WIDTH, Math.round(n)));

interface Message {
  id: string;
  role: "user" | "assistant";
  content: string;
  timestamp: Date;
  toolCalls?: LlmToolCallInfo[];
  iterations?: number;
  isError?: boolean;
}

interface AIAssistantProps {
  isOpen: boolean;
  onClose: () => void;
  contextType: "study" | "trace";
  contextTitle: string;
  contextId: string;
  permanent?: boolean;
  /**
   * When provided (typically for `contextType === "trace"`) the parsed
   * sequence + quality + metadata are injected as a structured preamble
   * in front of the first user question so the agent can analyse it
   * with the registered tools.
   */
  traceContext?: TraceContext | null;
  /**
   * When provided (typically for `contextType === "study"`) the study
   * metadata, team, papers and sequencing progress are injected as a
   * structured preamble in front of the first user question so the agent
   * can answer questions about the study in general.
   */
  studyContext?: StudyContext | null;
  /** Force a specific LLM provider (claude / deepseek / ollama). */
  provider?: LlmProvider;
}

export function AIAssistant({
  isOpen,
  onClose,
  contextType,
  contextTitle,
  contextId,
  permanent,
  traceContext,
  studyContext,
  provider,
}: AIAssistantProps) {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: "welcome",
      role: "assistant",
      content:
        contextType === "trace" && traceContext
          ? `Hola — soy tu asistente de biología molecular. Tengo cargada la traza "${
              traceContext.name || traceContext.traceId
            }" (${traceContext.length} bp). Puedo analizar calidad, traducir, alinear, detectar variantes, buscar motifs, construir filogenias y más. ¿Qué quieres saber?`
          : contextType === "study"
            ? `Hola — soy tu asistente de investigación. Conozco el estudio "${contextTitle}": su descripción, equipo, artículos y el progreso de secuenciación. Puedes preguntarme cualquier cosa sobre el estudio en general. ¿Qué quieres saber?`
            : `I'm your research assistant for this ${contextType}. I can help you understand your data, suggest quality improvements, interpret results, and guide you through analysis workflows. What would you like to know?`,
      timestamp: new Date(),
    },
  ]);
  const [input, setInput] = useState("");
  const [isThinking, setIsThinking] = useState(false);
  /** Conversation id minted by the backend on the first response. */
  const [serverContextId, setServerContextId] = useState<string | null>(null);
  /** Whether the trace preamble has already been sent for this conversation. */
  const traceContextSentRef = useRef(false);
  /** Whether the study preamble has already been sent for this conversation. */
  const studyContextSentRef = useRef(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  /** Panel width (resizable by dragging the left edge). Persisted in
   *  localStorage so it survives reloads. */
  const [panelWidth, setPanelWidth] = useState<number>(DEFAULT_WIDTH);
  const [isResizing, setIsResizing] = useState(false);

  const { user, profile } = useAuthStore();

  const displayName = profile?.fullName || user?.username || "User";
  const initials = profile?.initials || displayName.charAt(0).toUpperCase();
  const photoUrl = profileService.resolveStorageUrl(
      profile?.photoThumbnailUrl || profile?.photoUrl
  );

  useEffect(() => {
    if (typeof window === "undefined") return;
    const stored = window.localStorage.getItem(PANEL_WIDTH_KEY);
    const parsed = stored ? parseInt(stored, 10) : NaN;
    if (Number.isFinite(parsed)) {
      setPanelWidth(clampWidth(parsed));
    }
  }, []);

  const startResize = (e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizing(true);

    let lastWidth = panelWidth;
    const onMove = (ev: MouseEvent) => {
      // Panel is anchored to the right edge of the viewport, so width =
      // distance from the cursor to that edge.
      lastWidth = clampWidth(window.innerWidth - ev.clientX);
      setPanelWidth(lastWidth);
    };
    const onUp = () => {
      setIsResizing(false);
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("mouseup", onUp);
      try {
        window.localStorage.setItem(PANEL_WIDTH_KEY, String(lastWidth));
      } catch {
        // ignore storage failures (private mode, etc.)
      }
    };
    window.addEventListener("mousemove", onMove);
    window.addEventListener("mouseup", onUp);
  };

  // Persist width whenever it changes (handles double-click reset too)
  useEffect(() => {
    if (typeof window === "undefined") return;
    try {
      window.localStorage.setItem(PANEL_WIDTH_KEY, String(panelWidth));
    } catch {
      // ignore
    }
  }, [panelWidth]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus();
    }
  }, [isOpen]);

  const studySuggestions = [
    "Resume de qué trata este estudio",
    "¿Quién forma parte del equipo y con qué rol?",
    "¿Cuál es el progreso de secuenciación (trazas procesadas, pendientes, fallidas)?",
    "¿Qué artículos hay asociados al estudio?",
  ];

  const traceSuggestions = [
    "Analiza la calidad de esta traza y resume las métricas clave",
    "Tradúcela en el marco 1 y dime si hay codones de stop prematuros",
    "Saca el reverse-complement de los primeros 100 bp",
    "¿Qué taxonomía sugieren estas bases (16S)?",
  ];

  const suggestions = contextType === "study" ? studySuggestions : traceSuggestions;

  const handleSendMessage = async () => {
    if (!input.trim() || isThinking) return;

    const question = input;
    const userMessage: Message = {
      id: Date.now().toString(),
      role: "user",
      content: question,
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput("");
    setIsThinking(true);

    // Only inject the context the FIRST time we hit the backend for this
    // conversation. Subsequent calls reuse the server-side context via
    // contextId.
    const includeTrace =
      contextType === "trace" &&
      !!traceContext &&
      !traceContextSentRef.current;
    const includeStudy =
      contextType === "study" &&
      !!studyContext &&
      !studyContextSentRef.current;

    try {
      const resp = await aiService.ask({
        question,
        contextId: serverContextId ?? undefined,
        provider,
        traceContext: includeTrace ? traceContext : null,
        studyContext: includeStudy ? studyContext : null,
      });

      if (includeTrace) traceContextSentRef.current = true;
      if (includeStudy) studyContextSentRef.current = true;
      if (resp.contextId) setServerContextId(resp.contextId);

      const assistantMessage: Message = {
        id: (Date.now() + 1).toString(),
        role: "assistant",
        content: resp.answer || "(sin respuesta)",
        timestamp: new Date(),
        toolCalls: resp.toolCalls,
        iterations: resp.iterations,
        isError: !!resp.error,
      };
      setMessages((prev) => [...prev, assistantMessage]);
    } catch (err) {
      const detail =
        err instanceof AiServiceError
          ? err.message
          : err instanceof Error
            ? err.message
            : String(err);
      const errMessage: Message = {
        id: (Date.now() + 1).toString(),
        role: "assistant",
        content: `Error al consultar el agente: ${detail}`,
        timestamp: new Date(),
        isError: true,
      };
      setMessages((prev) => [...prev, errMessage]);
    } finally {
      setIsThinking(false);
    }
  };

  const handleSuggestionClick = (suggestion: string) => {
    setInput(suggestion);
    inputRef.current?.focus();
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  if (!isOpen) return null;

  return (
    <div
      className={cn(
        "fixed bottom-0 right-0 top-0 z-50 flex max-w-full transform flex-col border-l border-border bg-card shadow-2xl transition-transform duration-300 ease-in-out",
        isOpen ? "translate-x-0" : "translate-x-full",
        isResizing && "select-none transition-none"
      )}
      style={{ width: `${panelWidth}px` }}
      role="dialog"
      aria-labelledby="ai-assistant-title"
      aria-modal="true"
    >
      {/* Resize handle — drag the left edge to grow/shrink the panel */}
      <div
        onMouseDown={startResize}
        onDoubleClick={() => setPanelWidth(DEFAULT_WIDTH)}
        className={cn(
          "group absolute left-0 top-0 z-20 flex h-full w-2 -translate-x-1/2 cursor-col-resize items-center justify-center",
          "hover:bg-teal/20",
          isResizing && "bg-teal/30"
        )}
        title="Arrastra para redimensionar · Doble click para resetear"
        aria-label="Resize panel"
        role="separator"
        aria-orientation="vertical"
      >
        {/* Vertical guide line */}
        <div
          className={cn(
            "absolute inset-y-0 left-1/2 w-px -translate-x-1/2 bg-border transition-colors",
            "group-hover:bg-teal",
            isResizing && "bg-teal"
          )}
        />
        {/* Grip pill — always faintly visible, brighter on hover/drag */}
        <div
          className={cn(
            "relative flex h-14 w-1.5 flex-col items-center justify-center gap-0.5 rounded-full border border-border bg-card shadow-sm transition-all",
            "group-hover:h-16 group-hover:border-teal/60 group-hover:bg-teal/10 group-hover:shadow-md",
            isResizing && "h-16 border-teal bg-teal/20"
          )}
        >
          <span className="h-1 w-1 rounded-full bg-muted-foreground group-hover:bg-teal" />
          <span className="h-1 w-1 rounded-full bg-muted-foreground group-hover:bg-teal" />
          <span className="h-1 w-1 rounded-full bg-muted-foreground group-hover:bg-teal" />
        </div>
      </div>
      {/* Header */}
      <div className="flex flex-shrink-0 items-center justify-between border-b border-border bg-muted/30 px-5 py-4">
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-gradient-to-br from-teal/10 to-blue-deep/10">
            <Sparkles className="h-4.5 w-4.5 text-teal" />
          </div>
          <div className="flex flex-col">
            <h2
              id="ai-assistant-title"
              className="text-sm font-semibold text-foreground"
            >
              AI Research Assistant
            </h2>
            <span className="truncate text-xs text-muted-foreground">{contextTitle}</span>
          </div>
        </div>
        {!permanent && (
          <button
            onClick={onClose}
            className="min-h-[36px] min-w-[36px] rounded-lg p-2 text-muted-foreground transition-all hover:bg-muted/50 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label="Close AI assistant"
          >
            <X className="h-4.5 w-4.5" />
          </button>
        )}
      </div>

      {/* Messages Area */}
      <div className="flex-1 space-y-4 overflow-y-auto p-5">
        {messages.map((message) => (
          <div
            key={message.id}
            className={cn(
              "flex gap-3",
              message.role === "user" ? "justify-end" : "justify-start"
            )}
          >
            {message.role === "assistant" && (
              <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-teal/10 to-blue-deep/10">
                <Sparkles className="h-4 w-4 text-teal" />
              </div>
            )}
            <div
              className={cn(
                "max-w-[85%]",
                message.role === "user"
                  ? "rounded-2xl rounded-tr-md bg-teal px-4 py-2.5 text-white"
                  : message.isError
                    ? "rounded-2xl rounded-tl-md border border-destructive/40 bg-destructive/10 px-4 py-2.5 text-foreground"
                    : "rounded-2xl rounded-tl-md border border-border bg-muted/50 px-4 py-2.5 text-foreground"
              )}
            >
              {message.role === "user" ? (
                <p className="whitespace-pre-wrap text-sm leading-relaxed">
                  {message.content}
                </p>
              ) : (
                <div
                  className={cn(
                    "text-sm leading-relaxed",
                    // typographic styles: headings, lists, tables, code, links
                    "[&_h1]:mb-2 [&_h1]:mt-3 [&_h1]:text-base [&_h1]:font-semibold",
                    "[&_h2]:mb-1.5 [&_h2]:mt-2.5 [&_h2]:text-[15px] [&_h2]:font-semibold",
                    "[&_h3]:mb-1 [&_h3]:mt-2 [&_h3]:text-sm [&_h3]:font-semibold",
                    "[&_h4]:mb-1 [&_h4]:mt-2 [&_h4]:text-sm [&_h4]:font-semibold",
                    "[&_p]:my-1.5",
                    "[&_ul]:my-1.5 [&_ul]:list-disc [&_ul]:pl-5",
                    "[&_ol]:my-1.5 [&_ol]:list-decimal [&_ol]:pl-5",
                    "[&_li]:my-0.5",
                    "[&_strong]:font-semibold",
                    "[&_em]:italic",
                    "[&_hr]:my-3 [&_hr]:border-border",
                    "[&_blockquote]:my-2 [&_blockquote]:border-l-2 [&_blockquote]:border-teal [&_blockquote]:pl-3 [&_blockquote]:italic [&_blockquote]:text-muted-foreground",
                    "[&_a]:text-teal [&_a]:underline [&_a]:underline-offset-2 hover:[&_a]:opacity-80",
                    "[&_code]:rounded [&_code]:bg-muted [&_code]:px-1 [&_code]:py-0.5 [&_code]:font-mono [&_code]:text-[12px]",
                    "[&_pre]:my-2 [&_pre]:overflow-x-auto [&_pre]:rounded-md [&_pre]:border [&_pre]:border-border [&_pre]:bg-muted [&_pre]:p-2 [&_pre]:font-mono [&_pre]:text-[12px]",
                    "[&_pREDACTED]:bg-transparent [&_pREDACTED]:p-0",
                    // tables (GFM)
                    "[&_table]:my-2 [&_table]:w-full [&_table]:border-collapse [&_table]:overflow-hidden [&_table]:rounded-md [&_table]:border [&_table]:border-border [&_table]:text-[12px]",
                    "[&_thead]:bg-muted/60",
                    "[&_th]:border [&_th]:border-border [&_th]:px-2 [&_th]:py-1 [&_th]:text-left [&_th]:font-semibold",
                    "[&_td]:border [&_td]:border-border [&_td]:px-2 [&_td]:py-1 [&_td]:align-top",
                    "[&_tr:nth-child(even)]:bg-muted/30"
                  )}
                >
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>
                    {message.content}
                  </ReactMarkdown>
                </div>
              )}

              {message.toolCalls && message.toolCalls.length > 0 && (
                <details className="mt-2 text-[11px]">
                  <summary className="flex cursor-pointer items-center gap-1 text-muted-foreground hover:text-foreground">
                    <Wrench className="h-3 w-3" />
                    {message.toolCalls.length} tool call
                    {message.toolCalls.length === 1 ? "" : "s"}
                    {typeof message.iterations === "number"
                      ? ` · ${message.iterations} iter`
                      : ""}
                  </summary>
                  <ul className="mt-1 space-y-1 pl-4">
                    {message.toolCalls.map((tc, idx) => (
                      <li
                        key={`${tc.iteration}-${idx}-${tc.name}`}
                        className="font-mono text-muted-foreground"
                      >
                        <span className="text-teal">{tc.name}</span>
                        <span>(</span>
                        <span className="break-all">
                          {typeof tc.arguments === "string"
                            ? tc.arguments
                            : JSON.stringify(tc.arguments)}
                        </span>
                        <span>)</span>
                      </li>
                    ))}
                  </ul>
                </details>
              )}

              <span
                className={cn(
                  "mt-1.5 block text-[10px]",
                  message.role === "user" ? "text-white/70" : "text-muted-foreground"
                )}
              >
                {message.timestamp.toLocaleTimeString([], {
                  hour: "2-digit",
                  minute: "2-digit",
                })}
              </span>
            </div>
            {message.role === "user" && (
                photoUrl ? (
                    <img
                        src={photoUrl}
                        alt={displayName}
                        className="h-8 w-8 flex-shrink-0 rounded-full object-cover"
                    />
                ) : (
                    <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-blue-deep to-teal text-xs font-semibold text-white">
                      {initials}
                    </div>
                )
            )}
          </div>
        ))}

        {isThinking && (
          <div className="flex justify-start gap-3">
            <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-teal/10 to-blue-deep/10">
              <Sparkles className="h-4 w-4 text-teal" />
            </div>
            <div className="rounded-2xl rounded-tl-md border border-border bg-muted/50 px-4 py-3 text-foreground">
              <div className="flex items-center gap-2">
                <Loader2 className="h-3.5 w-3.5 animate-spin text-teal" />
                <span className="text-sm text-muted-foreground">Analyzing your request...</span>
              </div>
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Suggestions */}
      {messages.length === 1 && !isThinking && (
        <div className="flex-shrink-0 px-5 pb-4">
          <p className="mb-2.5 text-xs font-medium text-muted-foreground">
            Suggested questions:
          </p>
          <div className="grid grid-cols-1 gap-2" role="list">
            {suggestions.map((suggestion, idx) => (
              <button
                key={idx}
                onClick={() => handleSuggestionClick(suggestion)}
                className="group flex min-h-[40px] items-center gap-2 rounded-lg border border-border bg-muted/30 px-3 py-2 text-left transition-all hover:bg-muted/60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                role="listitem"
                aria-label={`Use suggestion: ${suggestion}`}
              >
                <ChevronRight
                  className="h-3.5 w-3.5 text-teal opacity-0 transition-opacity group-hover:opacity-100"
                  aria-hidden="true"
                />
                <span className="text-xs text-foreground">{suggestion}</span>
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Input Area */}
      <div className="flex-shrink-0 border-t border-border bg-background/50 p-4">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            handleSendMessage();
          }}
          className="flex gap-2"
        >
          <div className="relative flex-1">
            <label htmlFor="ai-input" className="sr-only">
              Ask a question
            </label>
            <textarea
              id="ai-input"
              ref={inputRef}
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Ask a question or request an action..."
              rows={1}
              className="min-h-[42px] w-full resize-none rounded-lg border border-border bg-background px-4 py-2.5 text-sm text-foreground placeholder:text-muted-foreground transition-all focus-visible:border-teal focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal/20"
              style={{ maxHeight: "120px" }}
              aria-label="Ask AI assistant a question"
            />
          </div>
          <button
            type="submit"
            onClick={handleSendMessage}
            disabled={!input.trim() || isThinking}
            className="min-h-[44px] min-w-[44px] rounded-lg bg-teal px-4 py-2.5 text-white transition-all hover:bg-teal/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            aria-label="Send message"
          >
            <Send className="h-4 w-4" />
          </button>
        </form>
        <p className="mt-2 px-1 text-[10px] text-muted-foreground">
          Press Enter to send, Shift+Enter for new line
        </p>
      </div>
    </div>
  );
}

// (mock response generator removed — answers now come from the AI backend
// via aiService.ask)
