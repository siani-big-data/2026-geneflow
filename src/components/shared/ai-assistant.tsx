"use client";

import { useState, useRef, useEffect } from "react";
import { Sparkles, X, Send, ChevronRight, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

interface Message {
  id: string;
  role: "user" | "assistant";
  content: string;
  timestamp: Date;
}

interface AIAssistantProps {
  isOpen: boolean;
  onClose: () => void;
  contextType: "study" | "trace";
  contextTitle: string;
  contextId: string;
  permanent?: boolean;
}

export function AIAssistant({
  isOpen,
  onClose,
  contextType,
  contextTitle,
  contextId,
  permanent,
}: AIAssistantProps) {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: "welcome",
      role: "assistant",
      content: `I'm your research assistant for this ${contextType}. I can help you understand your data, suggest quality improvements, interpret results, and guide you through analysis workflows. What would you like to know?`,
      timestamp: new Date(),
    },
  ]);
  const [input, setInput] = useState("");
  const [isThinking, setIsThinking] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

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
    "Summarize the quality metrics for this study",
    "Which samples have quality issues?",
    "Suggest next steps for analysis",
    "Show me collaboration insights",
  ];

  const traceSuggestions = [
    "Analyze the quality of this trace",
    "Identify regions that need trimming",
    "Explain the quality score",
    "Compare with similar traces",
  ];

  const suggestions = contextType === "study" ? studySuggestions : traceSuggestions;

  const handleSendMessage = async () => {
    if (!input.trim() || isThinking) return;

    const userMessage: Message = {
      id: Date.now().toString(),
      role: "user",
      content: input,
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput("");
    setIsThinking(true);

    // Simulate AI response
    setTimeout(() => {
      const assistantMessage: Message = {
        id: (Date.now() + 1).toString(),
        role: "assistant",
        content: getContextualResponse(input, contextType),
        timestamp: new Date(),
      };
      setMessages((prev) => [...prev, assistantMessage]);
      setIsThinking(false);
    }, 1500);
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
        "fixed bottom-0 right-0 top-0 z-50 flex w-full transform flex-col border-l border-border bg-card shadow-2xl transition-transform duration-300 ease-in-out sm:w-[400px]",
        isOpen ? "translate-x-0" : "translate-x-full"
      )}
      role="dialog"
      aria-labelledby="ai-assistant-title"
      aria-modal="true"
    >
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
                  : "rounded-2xl rounded-tl-md border border-border bg-muted/50 px-4 py-2.5 text-foreground"
              )}
            >
              <p className="text-sm leading-relaxed">{message.content}</p>
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
              <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-blue-deep to-teal text-xs font-semibold text-white">
                SM
              </div>
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

function getContextualResponse(userInput: string, contextType: "study" | "trace"): string {
  const input = userInput.toLowerCase();

  if (contextType === "study") {
    if (input.includes("quality") || input.includes("metrics")) {
      return "Based on the current study data, I've analyzed 1,247 samples across 24 sequencing runs. Overall quality is high with 94% of samples passing QC thresholds. The average Phred quality score is 38.2, and coverage depth is consistent at 120x. I notice 76 samples (6%) have quality scores below Q30 in specific regions—would you like me to identify which samples need attention?";
    }
    if (input.includes("issues") || input.includes("problems")) {
      return "I've identified 76 samples with quality concerns: 42 have low coverage in specific genomic regions, 28 show elevated error rates in homopolymer regions, and 6 have potential contamination indicators. The most affected samples are in sequencing batch SB-2026-03. I can generate a detailed report or suggest resequencing candidates if you'd like.";
    }
    if (input.includes("next steps") || input.includes("analysis")) {
      return "Based on your study design and current data quality, I recommend the following workflow: 1) Run variant calling on the 1,171 high-quality samples, 2) Generate a QC report for the flagged samples to decide on resequencing, 3) Perform initial association analysis on complete sample set, 4) Review coverage uniformity across target regions. Would you like me to help set up any of these pipelines?";
    }
    if (input.includes("collaboration") || input.includes("team")) {
      return "Your study currently has 12 active collaborators across 3 institutions. Dr. James Wong has been most active recently, uploading 124 new traces in the past week. There are 3 pending analysis results that haven't been reviewed by co-investigators. Would you like me to summarize recent team activity or help you share specific findings?";
    }
  } else {
    if (input.includes("quality") || input.includes("analyze")) {
      return "This trace shows excellent overall quality with a Phred score of Q42 (99.994% base call accuracy). The signal is strong and clear across most of the sequence, with well-defined peaks and minimal background noise. However, I notice the quality degrades slightly after position 650, which is normal for Sanger sequencing. The heterozygous variant at position 342 is well-supported with clear double peaks.";
    }
    if (input.includes("trim") || input.includes("regions")) {
      return "I recommend trimming the first 25 bases and everything after position 680. The initial bases show typical sequence startup artifacts with mixed signals, while the tail region has declining quality scores below Q20. The core region (bases 26-680) maintains excellent quality throughout. Would you like me to suggest specific coordinates for your trimming pipeline?";
    }
    if (input.includes("score") || input.includes("explain")) {
      return "The quality score represents base-calling confidence using Phred scaling, where Q30 = 99.9% accuracy and Q40 = 99.99% accuracy. Your trace averages Q42, which is excellent for downstream analysis. The score is calculated from peak spacing, height ratios, background noise, and signal resolution. The slight dip around position 520 is likely due to a GC-rich region causing secondary structure—this is expected and doesn't affect reliability.";
    }
    if (input.includes("compare") || input.includes("similar")) {
      return "Compared to other traces in this study, this sample ranks in the top 15% for quality. The average study trace has Q38, while this one achieves Q42. Peak resolution is 12% better than the median, and the usable read length (655bp) exceeds 78% of samples. This trace is an excellent candidate for variant calling and should produce highly reliable results.";
    }
  }

  return `I understand you're asking about "${userInput}". While I'm analyzing your ${contextType} data, I can help you with quality assessment, data interpretation, workflow suggestions, and actionable insights. Could you provide more specific details about what aspect you'd like me to focus on?`;
}
