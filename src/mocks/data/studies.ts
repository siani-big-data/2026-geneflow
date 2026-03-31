import type { Study, StudyStatus, StudyVisibility } from "@/types";
import { mockUsers } from "./users";

export const mockStudies: Study[] = [
  {
    id: "study-1",
    name: "BRCA1 Mutation Analysis",
    description: "Comprehensive analysis of BRCA1 mutations in breast cancer patients from the 2024 cohort study.",
    status: "active",
    visibility: "team",
    owner: mockUsers[0],
    members: [
      { user: mockUsers[0], role: "owner", joinedAt: "2024-01-15T10:00:00Z" },
      { user: mockUsers[1], role: "editor", joinedAt: "2024-01-20T14:00:00Z" },
    ],
    tracesCount: 342,
    createdAt: "2024-01-15T10:00:00Z",
    updatedAt: "2024-03-28T09:15:00Z",
  },
  {
    id: "study-2",
    name: "COVID-19 Variant Sequencing",
    description: "Tracking genomic variations in SARS-CoV-2 samples collected from multiple regions.",
    status: "active",
    visibility: "public",
    owner: mockUsers[0],
    members: [
      { user: mockUsers[0], role: "owner", joinedAt: "2024-02-01T08:00:00Z" },
    ],
    tracesCount: 1205,
    createdAt: "2024-02-01T08:00:00Z",
    updatedAt: "2024-03-27T16:30:00Z",
  },
  {
    id: "study-3",
    name: "Rare Disease Gene Panel",
    description: "Screening for rare genetic disorders using targeted gene panel sequencing.",
    status: "completed",
    visibility: "private",
    owner: mockUsers[0],
    members: [
      { user: mockUsers[0], role: "owner", joinedAt: "2023-11-10T10:00:00Z" },
      { user: mockUsers[2], role: "viewer", joinedAt: "2023-12-01T09:00:00Z" },
    ],
    tracesCount: 89,
    createdAt: "2023-11-10T10:00:00Z",
    updatedAt: "2024-02-15T11:00:00Z",
  },
  {
    id: "study-4",
    name: "Microbiome Diversity Study",
    description: "Analyzing gut microbiome composition across different dietary groups.",
    status: "active",
    visibility: "team",
    owner: mockUsers[1],
    members: [
      { user: mockUsers[1], role: "owner", joinedAt: "2024-03-01T10:00:00Z" },
      { user: mockUsers[0], role: "editor", joinedAt: "2024-03-05T14:00:00Z" },
    ],
    tracesCount: 567,
    createdAt: "2024-03-01T10:00:00Z",
    updatedAt: "2024-03-29T08:45:00Z",
  },
  {
    id: "study-5",
    name: "Plant Genome Annotation",
    description: "Annotating novel genes in drought-resistant crop varieties.",
    status: "draft",
    visibility: "private",
    owner: mockUsers[0],
    members: [
      { user: mockUsers[0], role: "owner", joinedAt: "2024-03-25T10:00:00Z" },
    ],
    tracesCount: 0,
    createdAt: "2024-03-25T10:00:00Z",
    updatedAt: "2024-03-25T10:00:00Z",
  },
  {
    id: "study-6",
    name: "Antibiotic Resistance Markers",
    description: "Identifying genetic markers associated with antibiotic resistance in bacterial strains.",
    status: "archived",
    visibility: "public",
    owner: mockUsers[2],
    members: [
      { user: mockUsers[2], role: "owner", joinedAt: "2023-06-15T10:00:00Z" },
    ],
    tracesCount: 234,
    createdAt: "2023-06-15T10:00:00Z",
    updatedAt: "2023-12-20T15:00:00Z",
  },
];

export function getStudyById(id: string): Study | undefined {
  return mockStudies.find((study) => study.id === id);
}

export function filterStudies(filters: {
  status?: StudyStatus;
  visibility?: StudyVisibility;
  search?: string;
}): Study[] {
  return mockStudies.filter((study) => {
    if (filters.status && study.status !== filters.status) return false;
    if (filters.visibility && study.visibility !== filters.visibility) return false;
    if (filters.search) {
      const search = filters.search.toLowerCase();
      return (
        study.name.toLowerCase().includes(search) ||
        study.description?.toLowerCase().includes(search)
      );
    }
    return true;
  });
}
