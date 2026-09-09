"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/lib/navigation";
import {
  Search as SearchIcon,
  FileText,
  MessageSquare,
  User,
  FlaskConical,
  Loader2,
  CornerDownLeft,
} from "lucide-react";
import { useGlobalSearch } from "@/hooks/use-search";
import { useCommandPalette } from "@/providers/command-palette-provider";
import type { SearchHit, SearchObjectType } from "@/types/search";
import { cn } from "@/lib/utils";

const TYPE_ICON: Record<SearchObjectType, typeof SearchIcon> = {
  Study: FlaskConical,
  Trace: FileText,
  Discussion: MessageSquare,
  User: User,
};

/** Maps a search hit to the in-app route it represents. */
function hrefFor(hit: SearchHit): string {
  switch (hit.objectType) {
    case "Study":
      return `/studies/${hit.objectId}`;
    case "Trace":
      return `/traces/${hit.objectId}`;
    case "Discussion":
      return hit.tags ? `/studies/${hit.tags}/discussions/${hit.objectId}` : `/`;
    case "User":
      return `/users/${hit.objectId}`;
  }
}

/**
 * Global Ctrl+K command palette. Renders an overlay dialog with a debounced
 * search input that drives the GlobalSearch query. Only shown when the
 * shared CommandPaletteProvider state is open.
 */
export function CommandPalette() {
  const t = useTranslations("commandPalette");
  const router = useRouter();
  const { open, setOpen } = useCommandPalette();
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const activeItemRef = useRef<HTMLButtonElement>(null);

  // Debounce keystrokes to avoid hammering the API.
  useEffect(() => {
    const id = setTimeout(() => setDebounced(query.trim()), 200);
    return () => clearTimeout(id);
  }, [query]);

  // Reset state every time the palette opens.
  useEffect(() => {
    if (open) {
      setQuery("");
      setDebounced("");
      setActiveIndex(0);
      // Defer focus until after the dialog mounts.
      const id = window.setTimeout(() => inputRef.current?.focus(), 30);
      return () => window.clearTimeout(id);
    }
  }, [open]);

  const { data, isFetching } = useGlobalSearch(
    { q: debounced, pageSize: 8 },
    { enabled: open && debounced.length > 0 },
  );

  // Keep the highlighted row scrolled into view as the user navigates with
  // the keyboard. block: "nearest" avoids jumpy scrolling.
  useEffect(() => {
    activeItemRef.current?.scrollIntoView({ block: "nearest" });
  }, [activeIndex]);

  const hits: SearchHit[] = data?.pages.flatMap((page) => page.items) ?? [];

  function activate(hit: SearchHit) {
    setOpen(false);
    router.push(hrefFor(hit));
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Escape") {
      setOpen(false);
      return;
    }
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIndex((i) => Math.min(hits.length - 1, i + 1));
      return;
    }
    if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((i) => Math.max(0, i - 1));
      return;
    }
    if (e.key === "Enter" && hits[activeIndex]) {
      e.preventDefault();
      activate(hits[activeIndex]);
    }
  }

  if (!open) return null;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label={t("ariaLabel")}
      className="fixed inset-0 z-50 flex items-start justify-center bg-background/60 px-4 pt-[10vh] backdrop-blur-sm"
      onClick={() => setOpen(false)}
    >
      <div
        className="w-full max-w-2xl overflow-hidden rounded-xl border border-border bg-card shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center gap-3 border-b border-border px-4 py-3">
          <SearchIcon className="h-5 w-5 text-muted-foreground" />
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setActiveIndex(0);
            }}
            onKeyDown={handleKeyDown}
            placeholder={t("placeholder")}
            className="flex-1 bg-transparent text-base text-foreground placeholder:text-muted-foreground/60 focus:outline-none"
          />
          {isFetching && (
            <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
          )}
          <kbd className="hidden rounded border border-border bg-muted px-1.5 py-0.5 text-xs text-muted-foreground sm:inline">
            esc
          </kbd>
        </div>

        <ul className="max-h-[60vh] overflow-y-auto py-2">
          {debounced.length === 0 && (
            <li className="px-4 py-6 text-center text-sm text-muted-foreground">
              {t("emptyPrompt")}
            </li>
          )}

          {debounced.length > 0 && hits.length === 0 && !isFetching && (
            <li className="px-4 py-6 text-center text-sm text-muted-foreground">
              {t("noResults", { query: debounced })}
            </li>
          )}

          {hits.map((hit, idx) => {
            const Icon = TYPE_ICON[hit.objectType];
            const isActive = idx === activeIndex;
            return (
              <li key={`${hit.objectType}:${hit.objectId}`} className="px-2">
                <button
                  type="button"
                  ref={isActive ? activeItemRef : undefined}
                  aria-selected={isActive}
                  onMouseEnter={() => setActiveIndex(idx)}
                  onClick={() => activate(hit)}
                  className={cn(
                    "relative flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left transition-colors",
                    "before:absolute before:inset-y-1.5 before:left-0 before:w-0.5 before:rounded-full before:transition-colors",
                    isActive
                      ? "bg-teal/10 ring-1 ring-inset ring-teal/30 before:bg-teal"
                      : "hover:bg-muted/50 before:bg-transparent",
                  )}
                >
                  <Icon
                    className={cn(
                      "h-4 w-4 shrink-0 transition-colors",
                      isActive ? "text-teal" : "text-muted-foreground",
                    )}
                  />
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span
                        className={cn(
                          "truncate text-sm",
                          isActive
                            ? "font-semibold text-foreground"
                            : "font-medium text-foreground",
                        )}
                      >
                        {hit.title}
                      </span>
                      <span
                        className={cn(
                          "rounded px-1.5 py-0.5 text-[10px] uppercase tracking-wide",
                          isActive
                            ? "bg-teal/15 text-teal"
                            : "bg-muted text-muted-foreground",
                        )}
                      >
                        {t(`types.${hit.objectType}`)}
                      </span>
                    </div>
                    {hit.snippet && (
                      <p className="line-clamp-1 text-xs text-muted-foreground">
                        {hit.snippet}
                      </p>
                    )}
                  </div>
                  {isActive && (
                    <CornerDownLeft
                      className="h-3.5 w-3.5 shrink-0 text-teal"
                      aria-hidden
                    />
                  )}
                </button>
              </li>
            );
          })}
        </ul>

        <div className="border-t border-border px-4 py-2 text-[11px] text-muted-foreground">
          <span className="inline-flex items-center gap-1">
            <kbd className="rounded border border-border bg-muted px-1">↑↓</kbd>
            {t("hints.navigate")}
            <span className="mx-2">·</span>
            <kbd className="rounded border border-border bg-muted px-1">↵</kbd>
            {t("hints.open")}
          </span>
        </div>
      </div>
    </div>
  );
}
