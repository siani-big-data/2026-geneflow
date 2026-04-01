"use client";

import {
  useState,
  useRef,
  useEffect,
  useCallback,
  useMemo,
} from "react";
import {
  ZoomIn,
  ZoomOut,
  Maximize2,
  BarChart3,
  Copy,
  Download,
  Activity,
  List,
  Search,
  X,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";
import { cn } from "@/lib/utils";

// Types
export interface ChromatogramControls {
  zoomIn: () => void;
  zoomOut: () => void;
  resetZoom: () => void;
  zoomLevel: number;
  minZoom: number;
  maxZoom: number;
  viewMode: "trace" | "sequence";
  setViewMode: (mode: "trace" | "sequence") => void;
  showQuality: boolean;
  setShowQuality: (show: boolean) => void;
  copySequence: () => void;
  exportFasta: () => void;
  copied: boolean;
}

export interface ChromatogramStats {
  sequenceLength: number;
  averageQuality: number;
  gcContent: number;
}

export interface ChromatogramData {
  sequence: string;
  quality: number[];
  peaks: {
    A: number[];
    T: number[];
    G: number[];
    C: number[];
  };
}

interface ChromatogramProps {
  data?: ChromatogramData;
  className?: string;
  onControlsReady?: (controls: ChromatogramControls) => void;
  onStatsReady?: (stats: ChromatogramStats) => void;
  hideHeader?: boolean;
}

// Colors for each nucleotide
const BASE_COLORS: Record<string, string> = {
  A: "#22c55e",
  T: "#ef4444",
  G: "#374151",
  C: "#3b82f6",
  N: "#9ca3af",
};

const BASE_COLORS_DARK: Record<string, string> = {
  A: "#4ade80",
  T: "#f87171",
  G: "#d1d5db",
  C: "#60a5fa",
  N: "#9ca3af",
};

// Constants - moved outside component to avoid recreation
const BASE_WIDTH = 14;
const MIN_ZOOM = 0.5;
const MAX_ZOOM = 6;
const MINIMAP_HEIGHT = 40;
const MINIMAP_GAP = 0;
const HEADER_HEIGHT = 20;
const SCROLLBAR_HEIGHT = 12;
const QUALITY_AREA_HEIGHT = 35;
const SEQUENCE_AREA_HEIGHT = 20;

// Generate mock data for demo purposes - memoized outside component
function generateMockData(length: number = 800): ChromatogramData {
  const bases = ["A", "T", "G", "C"];
  let sequence = "";
  const quality: number[] = [];
  const peaks = {
    A: [] as number[],
    T: [] as number[],
    G: [] as number[],
    C: [] as number[],
  };

  for (let i = 0; i < length; i++) {
    const rand = Math.random();
    let base: string;

    if (rand < 0.26) base = "A";
    else if (rand < 0.52) base = "T";
    else if (rand < 0.76) base = "G";
    else base = "C";

    sequence += base;

    const positionFactor = 1 - (Math.abs(i - length / 2) / (length / 2)) * 0.3;
    const baseQuality = Math.round((25 + Math.random() * 15) * positionFactor);
    quality.push(Math.min(40, Math.max(5, baseQuality)));

    const mainPeakHeight = 0.6 + Math.random() * 0.4;
    const noise = () => Math.random() * 0.12;

    peaks.A.push(base === "A" ? mainPeakHeight : noise());
    peaks.T.push(base === "T" ? mainPeakHeight : noise());
    peaks.G.push(base === "G" ? mainPeakHeight : noise());
    peaks.C.push(base === "C" ? mainPeakHeight : noise());
  }

  return { sequence, quality, peaks };
}

export function Chromatogram({ data: propData, className, onControlsReady, onStatsReady, hideHeader = false }: ChromatogramProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // Core state
  const [data, setData] = useState<ChromatogramData | null>(null);
  const [zoomLevel, setZoomLevel] = useState(1);
  const [scrollOffset, setScrollOffset] = useState(0);
  const [showQuality, setShowQuality] = useState(true);
  const [viewMode, setViewMode] = useState<"trace" | "sequence">("trace");
  const [hoveredBase, setHoveredBase] = useState<number | null>(null);
  const [isDarkMode, setIsDarkMode] = useState(false);
  const [copied, setCopied] = useState(false);

  // Search state
  const [searchQuery, setSearchQuery] = useState("");
  const [searchMatches, setSearchMatches] = useState<number[]>([]);
  const [currentMatchIndex, setCurrentMatchIndex] = useState(0);

  // Use refs for values that change frequently but don't need re-render
  const isDraggingRef = useRef(false);
  const isDraggingMinimapRef = useRef(false);
  const isDraggingScrollbarRef = useRef(false);
  const lastMouseXRef = useRef(0);
  const canvasSizeRef = useRef({ width: 0, height: 0 });
  const scrollOffsetRef = useRef(0);
  const zoomLevelRef = useRef(1);
  const hoveredBaseRef = useRef<number | null>(null);
  const showQualityRef = useRef(true);
  const viewModeRef = useRef<"trace" | "sequence">("trace");
  const searchMatchesRef = useRef<number[]>([]);
  const searchQueryRef = useRef("");
  const currentMatchIndexRef = useRef(0);

  // Animation frame ref for debouncing
  const rafIdRef = useRef<number>(0);
  const drawCanvasFnRef = useRef<() => void>(() => {});

  // Keep refs in sync with state
  useEffect(() => { scrollOffsetRef.current = scrollOffset; }, [scrollOffset]);
  useEffect(() => { zoomLevelRef.current = zoomLevel; }, [zoomLevel]);
  useEffect(() => { hoveredBaseRef.current = hoveredBase; }, [hoveredBase]);
  useEffect(() => { showQualityRef.current = showQuality; }, [showQuality]);
  useEffect(() => { viewModeRef.current = viewMode; }, [viewMode]);
  useEffect(() => { searchMatchesRef.current = searchMatches; }, [searchMatches]);
  useEffect(() => { searchQueryRef.current = searchQuery; }, [searchQuery]);
  useEffect(() => { currentMatchIndexRef.current = currentMatchIndex; }, [currentMatchIndex]);

  // Computed values
  const sequenceLength = data?.sequence.length || 0;

  const totalContentWidth = useMemo(() => {
    if (!data) return 0;
    return data.sequence.length * BASE_WIDTH * zoomLevel;
  }, [data, zoomLevel]);

  const maxScrollOffset = useMemo(() => {
    return Math.max(0, totalContentWidth - canvasSizeRef.current.width);
  }, [totalContentWidth]);

  const averageQuality = useMemo(() => {
    if (!data) return 0;
    const sum = data.quality.reduce((a, b) => a + b, 0);
    return Math.round(sum / data.quality.length);
  }, [data]);

  const gcContent = useMemo(() => {
    if (!data) return 0;
    const gc = data.sequence.split("").filter((b) => b === "G" || b === "C").length;
    return Math.round((gc / data.sequence.length) * 100);
  }, [data]);

  // Initialize data
  useEffect(() => {
    if (propData) {
      setData(propData);
    } else {
      setData(generateMockData(800));
    }
  }, [propData]);

  // Check dark mode
  useEffect(() => {
    const checkDarkMode = () => {
      setIsDarkMode(document.documentElement.classList.contains("dark"));
    };

    checkDarkMode();

    const observer = new MutationObserver(checkDarkMode);
    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ["class"],
    });

    return () => observer.disconnect();
  }, []);

  // Draw canvas function - uses refs to minimize dependencies and avoid recreations
  const drawCanvas = useCallback(() => {
    const canvas = canvasRef.current;
    const container = containerRef.current;
    if (!canvas || !container || !data) return;

    const width = container.clientWidth;
    const height = container.clientHeight;
    if (width <= 0 || height <= 0) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

    const isDark = document.documentElement.classList.contains("dark");
    const colors = isDark ? BASE_COLORS_DARK : BASE_COLORS;
    const bgColor = isDark ? "#111827" : "#ffffff";
    const textColor = isDark ? "#e5e7eb" : "#374151";
    const borderColor = isDark ? "#374151" : "#e5e7eb";

    // Read all values from refs
    const zoom = zoomLevelRef.current;
    const offset = scrollOffsetRef.current;
    const showQualityVal = showQualityRef.current;
    const viewModeVal = viewModeRef.current;
    const hoveredBaseVal = hoveredBaseRef.current;
    const searchMatchesVal = searchMatchesRef.current;
    const searchQueryVal = searchQueryRef.current;
    const currentMatchIndexVal = currentMatchIndexRef.current;

    // Clear
    ctx.fillStyle = bgColor;
    ctx.fillRect(0, 0, width, height);

    const baseW = BASE_WIDTH * zoom;

    // Calculate dynamic heights
    const dynPeakAreaHeight = Math.max(60, height - MINIMAP_HEIGHT - HEADER_HEIGHT - QUALITY_AREA_HEIGHT - SEQUENCE_AREA_HEIGHT - SCROLLBAR_HEIGHT - 10);

    // Calculate visible range
    const startBase = Math.floor(offset / baseW);
    const endBase = Math.ceil((offset + width) / baseW);

    // Draw minimap
    const minimapWidth = width - 20;
    const minimapX = 10;
    const minimapY = 5;

    ctx.fillStyle = isDark ? "#1f2937" : "#f3f4f6";
    ctx.fillRect(minimapX, minimapY, minimapWidth, MINIMAP_HEIGHT - 10);

    // Draw compressed peaks in minimap
    const basesPerPixel = data.sequence.length / minimapWidth;
    for (let px = 0; px < minimapWidth; px++) {
      const baseIndex = Math.floor(px * basesPerPixel);
      const base = data.sequence[baseIndex];
      if (base && data.peaks[base as keyof typeof data.peaks]) {
        const intensity = data.peaks[base as keyof typeof data.peaks][baseIndex] || 0;
        const barHeight = intensity * (MINIMAP_HEIGHT - 15);
        ctx.fillStyle = colors[base];
        ctx.globalAlpha = 0.7;
        ctx.fillRect(
          minimapX + px,
          minimapY + (MINIMAP_HEIGHT - 10 - barHeight),
          1,
          barHeight
        );
      }
    }
    ctx.globalAlpha = 1;

    // Viewport indicator
    const totalWidth = data.sequence.length * baseW;
    const viewportStart = (offset / totalWidth) * minimapWidth;
    const viewportWidth = Math.max(20, (width / totalWidth) * minimapWidth);

    ctx.strokeStyle = "#3b82f6";
    ctx.lineWidth = 2;
    ctx.strokeRect(minimapX + viewportStart, minimapY, viewportWidth, MINIMAP_HEIGHT - 10);
    ctx.fillStyle = "rgba(59, 130, 246, 0.1)";
    ctx.fillRect(minimapX + viewportStart, minimapY, viewportWidth, MINIMAP_HEIGHT - 10);

    // Draw main content
    const minimapEndY = minimapY + (MINIMAP_HEIGHT - 10);
    const peakY = minimapEndY + MINIMAP_GAP + HEADER_HEIGHT;
    const qualityY = peakY + dynPeakAreaHeight + 10;
    const sequenceY = qualityY + (showQualityVal ? QUALITY_AREA_HEIGHT + 12 : 0);

    // Draw position header
    ctx.font = "10px sans-serif";
    ctx.fillStyle = textColor;
    ctx.textAlign = "center";

    const step = zoom >= 2 ? 10 : zoom >= 1 ? 25 : 50;
    for (let i = 0; i < data.sequence.length; i += step) {
      const x = i * baseW - offset;
      if (x >= -baseW && x <= width + baseW) {
        ctx.fillText(String(i + 1), x + baseW / 2, peakY - 8);
      }
    }

    if (viewModeVal === "trace") {
      // Draw peaks
      const baseTypes: ("A" | "T" | "G" | "C")[] = ["G", "A", "T", "C"];

      for (const base of baseTypes) {
        ctx.beginPath();
        ctx.strokeStyle = colors[base];
        ctx.lineWidth = 1.5;

        let started = false;
        const startIdx = Math.max(0, startBase - 2);
        const endIdx = Math.min(data.sequence.length - 1, endBase + 2);

        const peakArray = data.peaks[base];

        for (let i = startIdx; i <= endIdx; i++) {
          const x = i * baseW - offset + baseW / 2;
          const peakValue = peakArray[i] ?? 0;
          const pY = peakY + dynPeakAreaHeight - peakValue * dynPeakAreaHeight * 0.9;

          if (!started) {
            ctx.moveTo(x, pY);
            started = true;
          } else {
            const prevX = (i - 1) * baseW - offset + baseW / 2;
            const prevPeakValue = peakArray[i - 1] ?? 0;
            const prevY = peakY + dynPeakAreaHeight - prevPeakValue * dynPeakAreaHeight * 0.9;
            const cpX = (prevX + x) / 2;
            ctx.bezierCurveTo(cpX, prevY, cpX, pY, x, pY);
          }
        }

        ctx.stroke();
      }

      // Draw quality bars
      if (showQualityVal) {
        for (let i = startBase; i <= Math.min(endBase, data.quality.length - 1); i++) {
          if (i < 0) continue;

          const x = i * baseW - offset;
          const q = data.quality[i];
          const barHeight = (Math.min(q, 60) / 60) * QUALITY_AREA_HEIGHT;

          let color: string;
          if (q >= 30) color = "#22c55e";
          else if (q >= 20) color = "#eab308";
          else if (q >= 10) color = "#f97316";
          else color = "#ef4444";

          ctx.fillStyle = color;
          ctx.globalAlpha = 0.6;
          ctx.fillRect(x + 2, qualityY + QUALITY_AREA_HEIGHT - barHeight, baseW - 4, barHeight);
        }
        ctx.globalAlpha = 1;
      }

      // Draw sequence letters
      if (zoom >= 0.8) {
        ctx.font = `bold ${Math.min(14, 10 * zoom)}px monospace`;
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";

        for (let i = startBase; i <= Math.min(endBase, data.sequence.length - 1); i++) {
          if (i < 0) continue;

          const x = i * baseW - offset + baseW / 2;
          const base = data.sequence[i];

          ctx.fillStyle = colors[base] || colors["N"];
          ctx.fillText(base, x, sequenceY);
        }
      }
    } else {
      // Sequence view - colored blocks
      const blockHeight = 28;
      const gap = 2;

      for (let i = startBase; i <= Math.min(endBase, data.sequence.length - 1); i++) {
        if (i < 0) continue;

        const x = i * baseW - offset;
        const base = data.sequence[i];

        ctx.fillStyle = colors[base] || colors["N"];
        ctx.fillRect(x + gap / 2, peakY, baseW - gap, blockHeight);

        if (zoom >= 0.6) {
          ctx.font = `bold ${Math.min(16, 12 * zoom)}px monospace`;
          ctx.textAlign = "center";
          ctx.textBaseline = "middle";
          ctx.fillStyle = "#ffffff";
          ctx.fillText(base, x + baseW / 2, peakY + blockHeight / 2);
        }
      }
    }

    // Draw search highlights
    if (searchMatchesVal.length > 0 && searchQueryVal) {
      const queryLength = searchQueryVal.length;

      for (let i = 0; i < searchMatchesVal.length; i++) {
        const matchStart = searchMatchesVal[i];
        const matchX = matchStart * baseW - offset;
        const matchWidth = queryLength * baseW;

        if (matchX + matchWidth < 0 || matchX > width) continue;

        const isCurrent = i === currentMatchIndexVal;

        ctx.fillStyle = isCurrent ? "rgba(251, 191, 36, 0.3)" : "rgba(251, 191, 36, 0.15)";
        ctx.strokeStyle = isCurrent ? "#f59e0b" : "rgba(245, 158, 11, 0.5)";
        ctx.lineWidth = isCurrent ? 2 : 1;

        ctx.fillRect(matchX, peakY, matchWidth, dynPeakAreaHeight + QUALITY_AREA_HEIGHT);
        ctx.strokeRect(matchX, peakY, matchWidth, dynPeakAreaHeight + QUALITY_AREA_HEIGHT);
      }
    }

    // Draw hover info
    if (hoveredBaseVal !== null && hoveredBaseVal >= 0 && hoveredBaseVal < data.sequence.length) {
      const x = hoveredBaseVal * baseW - offset + baseW / 2;

      ctx.strokeStyle = "rgba(59, 130, 246, 0.5)";
      ctx.lineWidth = 1;
      ctx.setLineDash([4, 4]);
      ctx.beginPath();
      ctx.moveTo(x, peakY);
      ctx.lineTo(x, sequenceY + 20);
      ctx.stroke();
      ctx.setLineDash([]);

      const base = data.sequence[hoveredBaseVal];
      const quality = data.quality[hoveredBaseVal];

      const boxWidth = 100;
      const boxHeight = 50;
      let boxX = x + 10;
      if (boxX + boxWidth > width) {
        boxX = x - boxWidth - 10;
      }
      const boxY = peakY + 10;

      ctx.fillStyle = isDark ? "rgba(31, 41, 55, 0.95)" : "rgba(255, 255, 255, 0.95)";
      ctx.strokeStyle = borderColor;
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.roundRect(boxX, boxY, boxWidth, boxHeight, 6);
      ctx.fill();
      ctx.stroke();

      ctx.font = "bold 13px sans-serif";
      ctx.fillStyle = colors[base];
      ctx.textAlign = "left";
      ctx.fillText(`${base} - Pos ${hoveredBaseVal + 1}`, boxX + 10, boxY + 20);

      ctx.font = "11px sans-serif";
      ctx.fillStyle = textColor;
      ctx.fillText(`Quality: Q${quality}`, boxX + 10, boxY + 38);
    }

    // Draw scrollbar
    if (totalWidth > width) {
      const scrollbarY = height - SCROLLBAR_HEIGHT - 5;
      const trackX = 20;
      const trackWidth = width - 40;

      ctx.fillStyle = isDark ? "#1f2937" : "#f3f4f6";
      ctx.beginPath();
      ctx.roundRect(trackX, scrollbarY, trackWidth, SCROLLBAR_HEIGHT, 6);
      ctx.fill();

      const maxScroll = Math.max(0, totalWidth - width);
      const thumbWidth = Math.max(40, (width / totalWidth) * trackWidth);
      const thumbX = maxScroll > 0 ? trackX + (offset / maxScroll) * (trackWidth - thumbWidth) : trackX;

      ctx.fillStyle = isDark ? "#4b5563" : "#9ca3af";
      ctx.beginPath();
      ctx.roundRect(thumbX, scrollbarY + 2, thumbWidth, SCROLLBAR_HEIGHT - 4, 4);
      ctx.fill();
    }
  }, [data]);

  // Setup canvas - only sets dimensions, drawCanvas handles the actual drawing
  const setupCanvas = useCallback(() => {
    const canvas = canvasRef.current;
    const container = containerRef.current;
    if (!canvas || !container) return;

    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const width = container.clientWidth;
    const height = container.clientHeight;

    if (width === 0 || height === 0) return;

    // Only update if size actually changed
    if (canvasSizeRef.current.width === width && canvasSizeRef.current.height === height) {
      return;
    }

    canvas.width = Math.floor(width * dpr);
    canvas.height = Math.floor(height * dpr);
    canvas.style.width = `${width}px`;
    canvas.style.height = `${height}px`;

    canvasSizeRef.current = { width, height };
  }, []);

  // Keep drawCanvas ref updated
  useEffect(() => {
    drawCanvasFnRef.current = drawCanvas;
  }, [drawCanvas]);

  // Request render - schedules a single RAF
  const requestRender = useCallback(() => {
    cancelAnimationFrame(rafIdRef.current);
    rafIdRef.current = requestAnimationFrame(() => {
      drawCanvasFnRef.current();
    });
  }, []);

  // Setup resize observer - runs once
  useEffect(() => {
    setupCanvas();
    requestRender();

    const observer = new ResizeObserver(() => {
      setupCanvas();
      requestRender();
    });

    if (containerRef.current) {
      observer.observe(containerRef.current);
    }

    return () => {
      observer.disconnect();
      cancelAnimationFrame(rafIdRef.current);
    };
  }, [setupCanvas, requestRender]);

  // Render when data changes
  useEffect(() => {
    requestRender();
  }, [data, requestRender]);

  // Render on visual state changes
  useEffect(() => {
    requestRender();
  }, [zoomLevel, scrollOffset, showQuality, viewMode, hoveredBase, searchMatches, currentMatchIndex, isDarkMode, requestRender]);

  // Clamp scroll
  useEffect(() => {
    const max = Math.max(0, totalContentWidth - canvasSizeRef.current.width);
    if (scrollOffset > max) {
      setScrollOffset(max);
    }
  }, [totalContentWidth, scrollOffset]);

  // Event handlers - optimized with refs
  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    const width = canvasSizeRef.current.width;
    const height = canvasSizeRef.current.height;

    // Check minimap
    const minimapX = 10;
    const minimapWidth = width - 20;
    const minimapY = 5;

    if (y >= minimapY && y <= minimapY + MINIMAP_HEIGHT - 10 && x >= minimapX && x <= minimapX + minimapWidth) {
      isDraggingMinimapRef.current = true;
      lastMouseXRef.current = e.clientX;

      const totalWidth = (data?.sequence.length || 0) * BASE_WIDTH * zoomLevelRef.current;
      const viewportWidth = Math.max(20, (width / totalWidth) * minimapWidth);
      const clickRatio = (x - minimapX - viewportWidth / 2) / (minimapWidth - viewportWidth);
      const max = Math.max(0, totalWidth - width);
      setScrollOffset(Math.max(0, Math.min(max, clickRatio * max)));

      canvas.style.cursor = "grabbing";
      return;
    }

    // Check scrollbar
    const trackX = 20;
    const trackWidth = width - 40;
    const scrollbarY = height - SCROLLBAR_HEIGHT - 5;

    if (y >= scrollbarY && y <= scrollbarY + SCROLLBAR_HEIGHT && x >= trackX && x <= trackX + trackWidth) {
      isDraggingScrollbarRef.current = true;
      lastMouseXRef.current = e.clientX;

      const totalWidth = (data?.sequence.length || 0) * BASE_WIDTH * zoomLevelRef.current;
      const thumbWidth = Math.max(40, (width / totalWidth) * trackWidth);
      const clickRatio = (x - trackX - thumbWidth / 2) / (trackWidth - thumbWidth);
      const max = Math.max(0, totalWidth - width);
      setScrollOffset(Math.max(0, Math.min(max, clickRatio * max)));

      canvas.style.cursor = "grabbing";
      return;
    }

    // Panning
    isDraggingRef.current = true;
    lastMouseXRef.current = e.clientX;
    canvas.style.cursor = "grabbing";
  }, [data]);

  const handleMouseMove = useCallback((e: React.MouseEvent) => {
    const canvas = canvasRef.current;
    if (!canvas || !data) return;

    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    const width = canvasSizeRef.current.width;
    const height = canvasSizeRef.current.height;

    const totalWidth = data.sequence.length * BASE_WIDTH * zoomLevelRef.current;
    const max = Math.max(0, totalWidth - width);

    // Handle minimap dragging
    if (isDraggingMinimapRef.current) {
      const minimapX = 10;
      const minimapWidth = width - 20;
      const viewportWidth = Math.max(20, (width / totalWidth) * minimapWidth);
      const clickRatio = (x - minimapX - viewportWidth / 2) / (minimapWidth - viewportWidth);
      setScrollOffset(Math.max(0, Math.min(max, clickRatio * max)));
      return;
    }

    // Handle scrollbar dragging
    if (isDraggingScrollbarRef.current) {
      const trackX = 20;
      const trackWidth = width - 40;
      const thumbWidth = Math.max(40, (width / totalWidth) * trackWidth);
      const clickRatio = (x - trackX - thumbWidth / 2) / (trackWidth - thumbWidth);
      setScrollOffset(Math.max(0, Math.min(max, clickRatio * max)));
      return;
    }

    // Handle panning
    if (isDraggingRef.current) {
      const delta = lastMouseXRef.current - e.clientX;
      lastMouseXRef.current = e.clientX;
      setScrollOffset((prev) => Math.max(0, Math.min(max, prev + delta)));
      return;
    }

    // Update cursor and hover
    const minimapX = 10;
    const minimapWidth = width - 20;
    const minimapY = 5;
    const trackX = 20;
    const trackWidth = width - 40;
    const scrollbarY = height - SCROLLBAR_HEIGHT - 5;
    const peakY = MINIMAP_HEIGHT + HEADER_HEIGHT;

    if ((y >= minimapY && y <= minimapY + MINIMAP_HEIGHT - 10 && x >= minimapX && x <= minimapX + minimapWidth) ||
        (y >= scrollbarY && y <= scrollbarY + SCROLLBAR_HEIGHT && x >= trackX && x <= trackX + trackWidth)) {
      canvas.style.cursor = "pointer";
    } else if (y > peakY && y < height - SCROLLBAR_HEIGHT - 10) {
      canvas.style.cursor = "grab";
      const baseW = BASE_WIDTH * zoomLevelRef.current;
      const baseIndex = Math.floor((x + scrollOffsetRef.current) / baseW);
      setHoveredBase(baseIndex);
    } else {
      canvas.style.cursor = "default";
      setHoveredBase(null);
    }
  }, [data]);

  const handleMouseUp = useCallback(() => {
    isDraggingRef.current = false;
    isDraggingMinimapRef.current = false;
    isDraggingScrollbarRef.current = false;
    if (canvasRef.current) {
      canvasRef.current.style.cursor = "grab";
    }
  }, []);

  const handleMouseLeave = useCallback(() => {
    setHoveredBase(null);
  }, []);

  // Global mouse events
  useEffect(() => {
    const handleGlobalMouseUp = () => {
      isDraggingRef.current = false;
      isDraggingMinimapRef.current = false;
      isDraggingScrollbarRef.current = false;
      if (canvasRef.current) {
        canvasRef.current.style.cursor = "grab";
      }
    };

    const handleGlobalMouseMove = (e: MouseEvent) => {
      const canvas = canvasRef.current;
      if (!canvas || !data) return;

      const rect = canvas.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const width = canvasSizeRef.current.width;
      const totalWidth = data.sequence.length * BASE_WIDTH * zoomLevelRef.current;
      const max = Math.max(0, totalWidth - width);

      if (isDraggingMinimapRef.current) {
        const minimapX = 10;
        const minimapWidth = width - 20;
        const viewportWidth = Math.max(20, (width / totalWidth) * minimapWidth);
        const clickRatio = (x - minimapX - viewportWidth / 2) / (minimapWidth - viewportWidth);
        setScrollOffset(Math.max(0, Math.min(max, clickRatio * max)));
        return;
      }

      if (isDraggingScrollbarRef.current) {
        const trackX = 20;
        const trackWidth = width - 40;
        const thumbWidth = Math.max(40, (width / totalWidth) * trackWidth);
        const clickRatio = (x - trackX - thumbWidth / 2) / (trackWidth - thumbWidth);
        setScrollOffset(Math.max(0, Math.min(max, clickRatio * max)));
        return;
      }

      if (isDraggingRef.current) {
        const delta = lastMouseXRef.current - e.clientX;
        lastMouseXRef.current = e.clientX;
        setScrollOffset((prev) => Math.max(0, Math.min(max, prev + delta)));
      }
    };

    window.addEventListener("mouseup", handleGlobalMouseUp);
    window.addEventListener("mousemove", handleGlobalMouseMove);

    return () => {
      window.removeEventListener("mouseup", handleGlobalMouseUp);
      window.removeEventListener("mousemove", handleGlobalMouseMove);
    };
  }, [data]);

  // Wheel handler
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const handleWheel = (e: WheelEvent) => {
      e.preventDefault();
      e.stopPropagation();

      const canvas = canvasRef.current;
      if (!canvas || !data) return;

      const rect = canvas.getBoundingClientRect();
      const mouseX = e.clientX - rect.left;
      const width = canvasSizeRef.current.width;

      if (e.ctrlKey || e.metaKey) {
        // Zoom
        const zoomDelta = e.deltaY > 0 ? -0.2 : 0.2;
        setZoomLevel((oldZoom) => {
          const newZoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, oldZoom + zoomDelta));
          if (newZoom !== oldZoom) {
            setScrollOffset((prevOffset) => {
              const mouseOffset = mouseX + prevOffset;
              const baseAtMouse = mouseOffset / (BASE_WIDTH * oldZoom);
              const newMouseOffset = baseAtMouse * BASE_WIDTH * newZoom;
              return Math.max(0, newMouseOffset - mouseX);
            });
          }
          return newZoom;
        });
      } else {
        // Scroll
        const scrollDelta = (Math.abs(e.deltaX) > Math.abs(e.deltaY) ? e.deltaX : e.deltaY) * 1.5;
        setScrollOffset((prev) => {
          const totalWidth = data.sequence.length * BASE_WIDTH * zoomLevelRef.current;
          const max = Math.max(0, totalWidth - width);
          return Math.max(0, Math.min(max, prev + scrollDelta));
        });
      }
    };

    container.addEventListener("wheel", handleWheel, { passive: false });
    return () => container.removeEventListener("wheel", handleWheel);
  }, [data]);

  // Memoized actions
  const zoomIn = useCallback(() => {
    setZoomLevel((prev) => Math.min(MAX_ZOOM, prev + 0.5));
  }, []);

  const zoomOut = useCallback(() => {
    setZoomLevel((prev) => Math.max(MIN_ZOOM, prev - 0.5));
  }, []);

  const resetZoom = useCallback(() => {
    setZoomLevel(1);
  }, []);

  const copySequence = useCallback(() => {
    if (data) {
      navigator.clipboard.writeText(data.sequence);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  }, [data]);

  const exportFasta = useCallback(() => {
    if (!data) return;

    const gc = data.sequence.split("").filter((b) => b === "G" || b === "C").length;
    const gcPct = Math.round((gc / data.sequence.length) * 100);
    const header = `>sequence | length=${data.sequence.length}bp | gc=${gcPct}%`;
    const formattedSequence = data.sequence.match(/.{1,60}/g)?.join("\n") || data.sequence;
    const fastaContent = `${header}\n${formattedSequence}`;

    const blob = new Blob([fastaContent], { type: "text/plain" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "sequence.fasta";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }, [data]);

  // Memoized controls object
  const controls = useMemo<ChromatogramControls>(() => ({
    zoomIn,
    zoomOut,
    resetZoom,
    zoomLevel,
    minZoom: MIN_ZOOM,
    maxZoom: MAX_ZOOM,
    viewMode,
    setViewMode,
    showQuality,
    setShowQuality,
    copySequence,
    exportFasta,
    copied,
  }), [zoomIn, zoomOut, resetZoom, zoomLevel, viewMode, showQuality, copySequence, exportFasta, copied]);

  // Expose controls to parent - only when controls change
  useEffect(() => {
    if (onControlsReady) {
      onControlsReady(controls);
    }
  }, [onControlsReady, controls]);

  // Memoized stats object
  const stats = useMemo<ChromatogramStats>(() => ({
    sequenceLength,
    averageQuality,
    gcContent,
  }), [sequenceLength, averageQuality, gcContent]);

  // Expose stats to parent
  useEffect(() => {
    if (onStatsReady) {
      onStatsReady(stats);
    }
  }, [onStatsReady, stats]);

  // Search
  const performSearch = useCallback(() => {
    if (!data || searchQuery.length < 2) {
      setSearchMatches([]);
      return;
    }

    const query = searchQuery.toUpperCase();
    const matches: number[] = [];
    let pos = 0;

    while ((pos = data.sequence.indexOf(query, pos)) !== -1) {
      matches.push(pos);
      pos++;
    }

    setSearchMatches(matches);
    setCurrentMatchIndex(0);

    if (matches.length > 0) {
      const matchStart = matches[0];
      const baseW = BASE_WIDTH * zoomLevelRef.current;
      const matchCenterX = matchStart * baseW + (query.length * baseW) / 2;
      const width = canvasSizeRef.current.width;
      const totalWidth = data.sequence.length * baseW;
      const max = Math.max(0, totalWidth - width);
      const targetScroll = matchCenterX - width / 2;
      setScrollOffset(Math.max(0, Math.min(max, targetScroll)));
    }
  }, [data, searchQuery]);

  useEffect(() => {
    const timeout = setTimeout(performSearch, 150);
    return () => clearTimeout(timeout);
  }, [performSearch]);

  const navigateToMatch = useCallback((index: number) => {
    if (index < 0 || index >= searchMatches.length || !data) return;

    const matchStart = searchMatches[index];
    const baseW = BASE_WIDTH * zoomLevelRef.current;
    const matchCenterX = matchStart * baseW + (searchQuery.length * baseW) / 2;
    const width = canvasSizeRef.current.width;
    const totalWidth = data.sequence.length * baseW;
    const max = Math.max(0, totalWidth - width);
    const targetScroll = matchCenterX - width / 2;

    setScrollOffset(Math.max(0, Math.min(max, targetScroll)));
  }, [searchMatches, searchQuery, data]);

  const nextMatch = useCallback(() => {
    if (searchMatches.length === 0) return;
    const newIndex = (currentMatchIndex + 1) % searchMatches.length;
    setCurrentMatchIndex(newIndex);
    navigateToMatch(newIndex);
  }, [searchMatches.length, currentMatchIndex, navigateToMatch]);

  const prevMatch = useCallback(() => {
    if (searchMatches.length === 0) return;
    const newIndex = currentMatchIndex === 0 ? searchMatches.length - 1 : currentMatchIndex - 1;
    setCurrentMatchIndex(newIndex);
    navigateToMatch(newIndex);
  }, [searchMatches.length, currentMatchIndex, navigateToMatch]);

  const colors = isDarkMode ? BASE_COLORS_DARK : BASE_COLORS;

  return (
    <div className={cn("flex h-full flex-col gap-2", className)}>
      {/* Header */}
      {!hideHeader && (
        <div className="flex flex-shrink-0 flex-wrap items-center justify-between gap-4">
          <div className="flex gap-6">
            <div className="flex flex-col gap-0.5">
              <span className="text-xs uppercase tracking-wide text-muted-foreground">Length</span>
              <span className="text-lg font-semibold text-foreground">{sequenceLength} bp</span>
            </div>
            <div className="flex flex-col gap-0.5">
              <span className="text-xs uppercase tracking-wide text-muted-foreground">Avg Quality</span>
              <span className={cn(
                "text-lg font-semibold",
                averageQuality >= 30 ? "text-emerald-500" : averageQuality >= 20 ? "text-amber-500" : "text-red-500"
              )}>
                Q{averageQuality}
              </span>
            </div>
            <div className="flex flex-col gap-0.5">
              <span className="text-xs uppercase tracking-wide text-muted-foreground">GC Content</span>
              <span className="text-lg font-semibold text-foreground">{gcContent}%</span>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <div className="flex rounded-lg border border-border bg-muted/30 p-0.5">
              <button
                onClick={() => setViewMode("trace")}
                className={cn(
                  "flex h-7 w-8 items-center justify-center rounded-md transition-all",
                  viewMode === "trace" ? "bg-teal text-white" : "text-muted-foreground hover:text-foreground"
                )}
                title="Trace View"
              >
                <Activity className="h-4 w-4" />
              </button>
              <button
                onClick={() => setViewMode("sequence")}
                className={cn(
                  "flex h-7 w-8 items-center justify-center rounded-md transition-all",
                  viewMode === "sequence" ? "bg-teal text-white" : "text-muted-foreground hover:text-foreground"
                )}
                title="Sequence View"
              >
                <List className="h-4 w-4" />
              </button>
            </div>

            <div className="h-6 w-px bg-border" />

            <div className="flex items-center gap-1 rounded-lg border border-border bg-muted/30 p-0.5">
              <button
                onClick={zoomOut}
                disabled={zoomLevel <= MIN_ZOOM}
                className="flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground transition-all hover:bg-background hover:text-foreground disabled:opacity-40"
                title="Zoom Out"
              >
                <ZoomOut className="h-4 w-4" />
              </button>
              <span className="min-w-[45px] text-center text-sm font-medium text-foreground">
                {Math.round(zoomLevel * 100)}%
              </span>
              <button
                onClick={zoomIn}
                disabled={zoomLevel >= MAX_ZOOM}
                className="flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground transition-all hover:bg-background hover:text-foreground disabled:opacity-40"
                title="Zoom In"
              >
                <ZoomIn className="h-4 w-4" />
              </button>
              <button
                onClick={resetZoom}
                className="flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground transition-all hover:bg-background hover:text-foreground"
                title="Reset Zoom"
              >
                <Maximize2 className="h-4 w-4" />
              </button>
            </div>

            <div className="h-6 w-px bg-border" />

            <button
              onClick={() => setShowQuality(!showQuality)}
              className={cn(
                "flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-sm transition-all",
                showQuality
                  ? "border-teal bg-teal text-white"
                  : "border-border text-muted-foreground hover:border-teal hover:text-foreground"
              )}
            >
              <BarChart3 className="h-4 w-4" />
              Quality
            </button>

            <button
              onClick={copySequence}
              className="flex items-center gap-1.5 rounded-lg border border-border px-3 py-1.5 text-sm text-muted-foreground transition-all hover:border-teal hover:text-foreground"
            >
              <Copy className="h-4 w-4" />
              {copied ? "Copied!" : "Copy"}
            </button>

            <button
              onClick={exportFasta}
              className="flex items-center gap-1.5 rounded-lg border border-border px-3 py-1.5 text-sm text-muted-foreground transition-all hover:border-teal hover:text-foreground"
            >
              <Download className="h-4 w-4" />
              FASTA
            </button>
          </div>
        </div>
      )}

      {/* Search Bar */}
      <div className="flex flex-shrink-0 items-center gap-3 rounded-lg border border-border bg-background px-3 py-2">
        <div className="flex flex-1 items-center gap-2">
          <Search className="h-4 w-4 text-muted-foreground" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value.toUpperCase())}
            placeholder="Search sequence (e.g., ATCG)..."
            className="flex-1 border-none bg-transparent font-mono text-sm uppercase tracking-wider text-foreground placeholder:normal-case placeholder:tracking-normal focus:outline-none"
          />
          {searchQuery && (
            <button
              onClick={() => setSearchQuery("")}
              className="rounded-full p-0.5 text-muted-foreground hover:bg-muted hover:text-foreground"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>

        {searchMatches.length > 0 && (
          <div className="flex items-center gap-1">
            <span className="px-2 text-sm font-medium text-foreground">
              {currentMatchIndex + 1} / {searchMatches.length}
            </span>
            <button
              onClick={prevMatch}
              className="flex h-7 w-7 items-center justify-center rounded-md border border-border text-muted-foreground hover:bg-muted hover:text-foreground"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            <button
              onClick={nextMatch}
              className="flex h-7 w-7 items-center justify-center rounded-md border border-border text-muted-foreground hover:bg-muted hover:text-foreground"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        )}

        {searchQuery.length >= 2 && searchMatches.length === 0 && (
          <span className="text-sm italic text-muted-foreground">No matches</span>
        )}
      </div>

      {/* Legend */}
      <div className="flex flex-shrink-0 items-center gap-4 rounded-lg bg-muted/30 px-3 py-2">
        {["A", "T", "G", "C"].map((base) => (
          <div key={base} className="flex items-center gap-1.5">
            <span
              className="h-3.5 w-3.5 rounded-sm"
              style={{ backgroundColor: colors[base] }}
            />
            <span className="text-sm font-medium text-foreground">{base}</span>
          </div>
        ))}
        <span className="ml-auto text-xs text-muted-foreground">
          Scroll to pan, Ctrl+Scroll to zoom
        </span>
      </div>

      {/* Canvas */}
      <div
        ref={containerRef}
        className="relative min-h-[120px] flex-1 overflow-hidden rounded-lg border border-border bg-background"
      >
        <canvas
          ref={canvasRef}
          className="block"
          onMouseDown={handleMouseDown}
          onMouseMove={handleMouseMove}
          onMouseUp={handleMouseUp}
          onMouseLeave={handleMouseLeave}
        />
      </div>
    </div>
  );
}
