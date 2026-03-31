import type { Trace, TraceStatus, TraceFileType } from "@/types";
import { mockUsers } from "./users";

export const mockTraces: Trace[] = [
  {
    id: "trace-1",
    name: "BRCA1_Sample_001.ab1",
    studyId: "study-1",
    studyName: "BRCA1 Mutation Analysis",
    status: "completed",
    fileType: "ab1",
    fileSize: 245760,
    sequenceLength: 892,
    qualityScore: 94.5,
    uploadedBy: mockUsers[0],
    createdAt: "2024-03-28T09:00:00Z",
    updatedAt: "2024-03-28T09:15:00Z",
    processedAt: "2024-03-28T09:15:00Z",
  },
  {
    id: "trace-2",
    name: "BRCA1_Sample_002.ab1",
    studyId: "study-1",
    studyName: "BRCA1 Mutation Analysis",
    status: "completed",
    fileType: "ab1",
    fileSize: 251904,
    sequenceLength: 905,
    qualityScore: 91.2,
    uploadedBy: mockUsers[0],
    createdAt: "2024-03-28T09:05:00Z",
    updatedAt: "2024-03-28T09:20:00Z",
    processedAt: "2024-03-28T09:20:00Z",
  },
  {
    id: "trace-3",
    name: "COVID_Variant_Delta_01.fasta",
    studyId: "study-2",
    studyName: "COVID-19 Variant Sequencing",
    status: "completed",
    fileType: "fasta",
    fileSize: 32768,
    sequenceLength: 29903,
    qualityScore: 98.7,
    uploadedBy: mockUsers[0],
    createdAt: "2024-03-27T14:00:00Z",
    updatedAt: "2024-03-27T14:05:00Z",
    processedAt: "2024-03-27T14:05:00Z",
  },
  {
    id: "trace-4",
    name: "COVID_Variant_Omicron_01.fasta",
    studyId: "study-2",
    studyName: "COVID-19 Variant Sequencing",
    status: "processing",
    fileType: "fasta",
    fileSize: 33280,
    sequenceLength: undefined,
    qualityScore: undefined,
    uploadedBy: mockUsers[0],
    createdAt: "2024-03-29T10:00:00Z",
    updatedAt: "2024-03-29T10:00:00Z",
  },
  {
    id: "trace-5",
    name: "RareDisease_Patient_042.ab1",
    studyId: "study-3",
    studyName: "Rare Disease Gene Panel",
    status: "completed",
    fileType: "ab1",
    fileSize: 198656,
    sequenceLength: 756,
    qualityScore: 88.3,
    uploadedBy: mockUsers[0],
    createdAt: "2024-02-10T11:00:00Z",
    updatedAt: "2024-02-10T11:10:00Z",
    processedAt: "2024-02-10T11:10:00Z",
  },
  {
    id: "trace-6",
    name: "Microbiome_Sample_A1.fastq",
    studyId: "study-4",
    studyName: "Microbiome Diversity Study",
    status: "pending",
    fileType: "fastq",
    fileSize: 524288,
    sequenceLength: undefined,
    qualityScore: undefined,
    uploadedBy: mockUsers[1],
    createdAt: "2024-03-29T08:30:00Z",
    updatedAt: "2024-03-29T08:30:00Z",
  },
  {
    id: "trace-7",
    name: "Microbiome_Sample_A2.fastq",
    studyId: "study-4",
    studyName: "Microbiome Diversity Study",
    status: "failed",
    fileType: "fastq",
    fileSize: 0,
    sequenceLength: undefined,
    qualityScore: undefined,
    uploadedBy: mockUsers[1],
    createdAt: "2024-03-29T08:35:00Z",
    updatedAt: "2024-03-29T08:40:00Z",
  },
  {
    id: "trace-8",
    name: "BRCA1_Sample_003.ab1",
    studyId: "study-1",
    studyName: "BRCA1 Mutation Analysis",
    status: "completed",
    fileType: "ab1",
    fileSize: 239616,
    sequenceLength: 878,
    qualityScore: 96.1,
    uploadedBy: mockUsers[1],
    createdAt: "2024-03-26T15:00:00Z",
    updatedAt: "2024-03-26T15:12:00Z",
    processedAt: "2024-03-26T15:12:00Z",
  },
];

export function getTraceById(id: string): Trace | undefined {
  return mockTraces.find((trace) => trace.id === id);
}

export function filterTraces(filters: {
  studyId?: string;
  status?: TraceStatus;
  fileType?: TraceFileType;
  search?: string;
}): Trace[] {
  return mockTraces.filter((trace) => {
    if (filters.studyId && trace.studyId !== filters.studyId) return false;
    if (filters.status && trace.status !== filters.status) return false;
    if (filters.fileType && trace.fileType !== filters.fileType) return false;
    if (filters.search) {
      const search = filters.search.toLowerCase();
      return (
        trace.name.toLowerCase().includes(search) ||
        trace.studyName.toLowerCase().includes(search)
      );
    }
    return true;
  });
}
