/**
 * Real trace fixtures shipped with the analysis module.
 *
 * Located in `geneflow-analysis/data/` and used by the upload + parsing
 * E2E tests so we exercise the same binary content the analysis worker
 * is built to handle.
 */
import * as fs from "node:fs";
import * as path from "node:path";

const ANALYSIS_DATA_DIR = path.resolve(
  __dirname,
  "../../../geneflow-analysis/data",
);

export type TraceFormat = "ab1" | "fasta";

export interface TraceFixture {
  /** Absolute path on disk. */
  filePath: string;
  /** Base filename (e.g. `310.ab1`). */
  filename: string;
  /** Logical format. */
  format: TraceFormat;
  /** Suggested MIME for upload. */
  mimeType: string;
}

function buildFixture(format: TraceFormat, filename: string): TraceFixture {
  return {
    filePath: path.join(ANALYSIS_DATA_DIR, format, filename),
    filename,
    format,
    mimeType:
      format === "ab1"
        ? "application/octet-stream"
        : "text/x-fasta",
  };
}

/** Sanger chromatograms from different sequencer models. */
export const AB1_FIXTURES: TraceFixture[] = [
  buildFixture("ab1", "310.ab1"),
  buildFixture("ab1", "3100.ab1"),
  buildFixture("ab1", "3730.ab1"),
  buildFixture("ab1", "sanger_example.ab1"),
];

/** Reference sequences for alignment / pipelines. */
export const FASTA_FIXTURES: TraceFixture[] = [
  buildFixture("fasta", "BRCA1_mRNA.fasta"),
  buildFixture("fasta", "TP53_mRNA.fasta"),
  buildFixture("fasta", "ecoli_16s.fasta"),
  buildFixture("fasta", "human_mitochondrion.fasta"),
  buildFixture("fasta", "lambda_phage.fasta"),
];

export const ALL_TRACE_FIXTURES: TraceFixture[] = [
  ...AB1_FIXTURES,
  ...FASTA_FIXTURES,
];

/** Read the raw bytes of a fixture (for `setInputFiles` payload). */
export function readTraceBuffer(fixture: TraceFixture): Buffer {
  return fs.readFileSync(fixture.filePath);
}

/** Throws early with a clear message if fixtures are missing. */
export function assertFixturesAvailable(): void {
  for (const f of ALL_TRACE_FIXTURES) {
    if (!fs.existsSync(f.filePath)) {
      throw new Error(
        `Missing trace fixture: ${f.filePath}. ` +
          `Copy sample data into geneflow-analysis/data/{ab1,fasta}/.`,
      );
    }
  }
}
