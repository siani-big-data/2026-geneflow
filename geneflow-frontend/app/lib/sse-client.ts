/**
 * Shared SSE transport used by every React Query hook that subscribes to a
 * server-sent events stream. Mirrors the wire-level behaviour previously
 * baked into `use-analysis-events.ts`:
 *
 *  - `fetch` + `ReadableStream` + `TextDecoderStream` (rather than the
 *    browser's `EventSource`, which can't carry a custom `Authorization`
 *    header).
 *  - Bearer token pulled from `tokenStorage` at every (re)connect.
 *  - Custom frame parser that respects `event:`, `data:`, and `:` comment
 *    lines — including multi-line `data:` payloads.
 *  - Exponential reconnect backoff (2s → 30s cap) on any transport error.
 *
 * The transport is intentionally protocol-only: it does not invalidate
 * caches, push toasts, or interpret payloads. Each hook does that itself.
 */

import { tokenStorage } from "@/lib/api-client";

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL || "http://localhost:5145";

const RECONNECT_INITIAL_MS = 2_000;
const RECONNECT_MAX_MS = 30_000;

/** Parsed server-sent event with its `event:` name and `data:` payload. */
export interface SseFrame {
  event: string;
  data: string;
}

/** Options accepted by {@link createSseSubscription}. */
export interface SseSubscriptionOptions {
  /**
   * Path or absolute URL of the SSE endpoint. Relative paths are resolved
   * against `NEXT_PUBLIC_API_URL`.
   */
  url: string;
  /**
   * Disables the connection without unmounting the host component. Useful
   * when an SSE stream is only relevant for transient resource states.
   */
  enabled: boolean;
  /**
   * Invoked for every parsed frame **except** the synthetic `ready` frame.
   * Receives the raw event name (e.g. `"trace.processing.completed"`) and
   * the parsed JSON payload as `unknown` — the caller narrows the type.
   */
  onFrame: (event: string, payload: unknown) => void;
  /**
   * Invoked once after the server emits its initial `ready` frame. The
   * back-off counter is reset at the same moment so a healthy reconnect
   * does not get throttled later.
   */
  onReady?: () => void;
}

/**
 * Splits a raw SSE chunk buffer at frame boundaries (double newline) and
 * returns the parsed frames plus the unparsed remainder.
 */
function parseFrames(buffer: string): { frames: SseFrame[]; rest: string } {
  const frames: SseFrame[] = [];
  let rest = buffer;

  while (true) {
    const boundary = rest.indexOf("\n\n");
    if (boundary === -1) break;

    const raw = rest.slice(0, boundary);
    rest = rest.slice(boundary + 2);

    let event = "message";
    const dataLines: string[] = [];

    for (const line of raw.split("\n")) {
      if (line.startsWith(":")) continue; // SSE comment / keepalive
      if (line.startsWith("event:")) event = line.slice(6).trim();
      else if (line.startsWith("data:")) dataLines.push(line.slice(5).trim());
    }

    if (dataLines.length > 0) {
      frames.push({ event, data: dataLines.join("\n") });
    } else if (event !== "message") {
      // Event without data (e.g. server-emitted `ready`) — still useful as
      // a signal that the connection is live.
      frames.push({ event, data: "" });
    }
  }

  return { frames, rest };
}

function buildUrl(url: string): string {
  if (/^https?:\/\//i.test(url)) return url;
  return `${API_BASE_URL}${url.startsWith("/") ? url : `/${url}`}`;
}

/**
 * Opens a persistent SSE subscription to `opts.url` and returns a disposer
 * that aborts the in-flight request and cancels any pending reconnect.
 *
 * The function never throws synchronously: connection failures are caught
 * and retried with exponential back-off. Disposers are idempotent.
 */
export function createSseSubscription(opts: SseSubscriptionOptions): () => void {
  const { url, enabled, onFrame, onReady } = opts;

  if (!enabled) {
    return () => {};
  }

  let abortController: AbortController | null = null;
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  let backoff = RECONNECT_INITIAL_MS;
  let cancelled = false;

  const fullUrl = buildUrl(url);

  const handleFrame = (frame: SseFrame): void => {
    if (frame.event === "ready") {
      backoff = RECONNECT_INITIAL_MS;
      onReady?.();
      return;
    }

    if (!frame.data) return;

    let parsed: unknown;
    try {
      parsed = JSON.parse(frame.data);
    } catch {
      // Malformed payload — drop it. The next event will reconcile.
      return;
    }

    onFrame(frame.event, parsed);
  };

  const connect = async (): Promise<void> => {
    if (cancelled) return;

    abortController = new AbortController();
    const headers: Record<string, string> = { Accept: "text/event-stream" };
    const token = tokenStorage.getAccessToken();
    if (token) headers["Authorization"] = `Bearer ${token}`;

    try {
      const response = await fetch(fullUrl, {
        method: "GET",
        headers,
        credentials: "include",
        signal: abortController.signal,
        cache: "no-store",
      });

      if (!response.ok || !response.body) {
        throw new Error(`SSE connect failed: ${response.status}`);
      }

      const reader = response.body
        .pipeThrough(new TextDecoderStream())
        .getReader();
      let buffer = "";

      while (!cancelled) {
        const { value, done } = await reader.read();
        if (done) throw new Error("SSE stream ended");

        buffer += value;
        const { frames, rest } = parseFrames(buffer);
        buffer = rest;
        for (const frame of frames) handleFrame(frame);
      }
    } catch (err) {
      if (cancelled) return;
      if (err instanceof DOMException && err.name === "AbortError") return;

      reconnectTimer = setTimeout(connect, backoff);
      backoff = Math.min(backoff * 2, RECONNECT_MAX_MS);
    }
  };

  void connect();

  return () => {
    cancelled = true;
    if (reconnectTimer) clearTimeout(reconnectTimer);
    abortController?.abort();
    abortController = null;
  };
}
