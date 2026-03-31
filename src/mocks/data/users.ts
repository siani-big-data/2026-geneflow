import type { User, UserProfile } from "@/types";

export const mockUsers: User[] = [
  {
    id: "user-1",
    email: "sarah.martinez@geneflow.io",
    name: "Dr. Sarah Martinez",
    avatar: undefined,
    role: "researcher",
    plan: "pro",
    organization: "Stanford Genomics Lab",
    createdAt: "2024-01-15T10:00:00Z",
    updatedAt: "2024-03-20T14:30:00Z",
  },
  {
    id: "user-2",
    email: "john.chen@biotech.com",
    name: "John Chen",
    avatar: undefined,
    role: "researcher",
    plan: "enterprise",
    organization: "BioTech Industries",
    createdAt: "2024-02-01T09:00:00Z",
    updatedAt: "2024-03-18T11:00:00Z",
  },
  {
    id: "user-3",
    email: "emily.watson@university.edu",
    name: "Emily Watson",
    avatar: undefined,
    role: "user",
    plan: "free",
    organization: "University Research Center",
    createdAt: "2024-03-01T08:00:00Z",
    updatedAt: "2024-03-25T16:00:00Z",
  },
];

export const mockCurrentUser: UserProfile = {
  ...mockUsers[0],
  bio: "Principal Investigator specializing in genetic sequencing and bioinformatics.",
  location: "Stanford, CA",
  website: "https://stanford.edu/~smartinez",
  studiesCount: 12,
  tracesCount: 1234,
};

export function getUserById(id: string): User | undefined {
  return mockUsers.find((user) => user.id === id);
}
