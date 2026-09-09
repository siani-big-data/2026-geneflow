"use client";

import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { cn } from "@/lib/utils";

export interface MarkdownProps {
  source: string;
  className?: string;
}

/**
 * Safe markdown renderer.
 *
 * react-markdown does NOT parse raw HTML by default (no rehype-raw is wired in),
 * so user-supplied content cannot inject `<script>`, `onerror=`, or similar
 * vectors. GFM is enabled for tables, task lists, strikethrough and autolinks.
 *
 * All links are forced to `target="_blank" rel="noopener noreferrer"` to prevent
 * tabnabbing on README-rendered URLs.
 */
export function Markdown({ source, className }: MarkdownProps) {
  return (
    <div
      className={cn(
        "prose prose-sm dark:prose-invert max-w-none",
        "prose-headings:font-semibold prose-headings:text-foreground",
        "prose-p:text-foreground prose-li:text-foreground",
        "prose-a:text-blue-deep prose-a:no-underline hover:prose-a:underline",
        "prose-code:rounded prose-code:bg-muted prose-code:px-1 prose-code:py-0.5 prose-code:text-foreground prose-code:before:content-none prose-code:after:content-none",
        "prose-pre:rounded-md prose-pre:bg-muted prose-pre:text-foreground",
        "prose-blockquote:border-l-4 prose-blockquote:border-border prose-blockquote:text-muted-foreground",
        "prose-hr:border-border",
        className,
      )}
    >
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          a: ({ node: _node, ...props }) => (
            <a {...props} target="_blank" rel="noopener noreferrer" />
          ),
        }}
      >
        {source}
      </ReactMarkdown>
    </div>
  );
}
