import type {
  ChromatogramAnnotation,
  ChromatogramData,
  ChromatogramMotifMatch,
  ChromatogramRestrictionSite,
  ChromatogramTrimRegion,
} from "../chromatogram";

/**
 * IUPAC complement map. Covers standard bases plus ambiguous codes,
 * preserving case. Unknown characters are left untouched.
 */
const COMPLEMENT: Record<string, string> = {
  A: "T", T: "A", G: "C", C: "G", N: "N", U: "A",
  R: "Y", Y: "R", S: "S", W: "W", K: "M", M: "K",
  B: "V", V: "B", D: "H", H: "D",
  a: "t", t: "a", g: "c", c: "g", n: "n", u: "a",
  r: "y", y: "r", s: "s", w: "w", k: "m", m: "k",
  b: "v", v: "b", d: "h", h: "d",
};

function complementBase(b: string): string {
  return COMPLEMENT[b] ?? b;
}

function reverseComplementSequence(seq: string): string {
  let out = "";
  for (let i = seq.length - 1; i >= 0; i--) out += complementBase(seq[i]);
  return out;
}

function reverseArray<T>(arr: readonly T[]): T[] {
  const out = new Array<T>(arr.length);
  for (let i = 0; i < arr.length; i++) out[i] = arr[arr.length - 1 - i];
  return out;
}

/**
 * Returns a reverse-complemented copy of `data`. Channels are swapped
 * (A↔T, G↔C) and reversed so the visual peaks line up with the new sequence.
 */
export function reverseComplementChromatogramData(
  data: ChromatogramData,
): ChromatogramData {
  return {
    sequence: reverseComplementSequence(data.sequence),
    quality: reverseArray(data.quality),
    peaks: {
      A: reverseArray(data.peaks.T),
      T: reverseArray(data.peaks.A),
      G: reverseArray(data.peaks.C),
      C: reverseArray(data.peaks.G),
    },
  };
}

/**
 * Flip a 1-based inclusive position so it aligns with the reverse-complement.
 */
export function flipPosition(pos: number, length: number): number {
  return length + 1 - pos;
}

export function flipAnnotations(
  annotations: ChromatogramAnnotation[],
  length: number,
): ChromatogramAnnotation[] {
  return annotations.map((a) => ({
    ...a,
    startPosition: flipPosition(a.endPosition, length),
    endPosition: flipPosition(a.startPosition, length),
  }));
}

export function flipTrims(
  trims: ChromatogramTrimRegion[],
  length: number,
): ChromatogramTrimRegion[] {
  // Trim positions are stored as 0-based [start, end). Map to RC by:
  //   newStart = length - end
  //   newEnd   = length - start
  return trims.map((t) => ({
    ...t,
    startPosition: length - t.endPosition,
    endPosition: length - t.startPosition,
    trimEnd: t.trimEnd === "FivePrime" ? "ThreePrime" : "FivePrime",
  }));
}

export function flipMotifMatches(
  matches: ChromatogramMotifMatch[],
  length: number,
): ChromatogramMotifMatch[] {
  return matches.map((m) => ({
    ...m,
    start: flipPosition(m.end, length),
    end: flipPosition(m.start, length),
    strand: m.strand === "+" ? "-" : m.strand === "-" ? "+" : m.strand,
  }));
}

export function flipRestrictionSites(
  sites: ChromatogramRestrictionSite[],
  length: number,
): ChromatogramRestrictionSite[] {
  return sites.map((s) => ({
    ...s,
    position: flipPosition(s.position, length),
    cutPosition:
      s.cutPosition !== undefined ? flipPosition(s.cutPosition, length) : undefined,
  }));
}
