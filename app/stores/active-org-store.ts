"use client";

import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";

/**
 * The currently active workspace context. The user is either operating in
 * their personal context, or scoped to an organization (by handle).
 */
export type ActiveContext =
  | { type: "personal" }
  | { type: "org"; handle: string };

interface ActiveOrgStore {
  activeContext: ActiveContext;
  setActiveContext: (next: ActiveContext) => void;
  reset: () => void;
}

const DEFAULT_CONTEXT: ActiveContext = { type: "personal" };

export const useActiveOrgStore = create<ActiveOrgStore>()(
  persist(
    (set) => ({
      activeContext: DEFAULT_CONTEXT,
      setActiveContext: (next) => set({ activeContext: next }),
      reset: () => set({ activeContext: DEFAULT_CONTEXT }),
    }),
    {
      name: "geneflow:active-org",
      storage: createJSONStorage(() => localStorage),
    },
  ),
);

/** Selectors */
export const selectActiveContext = (s: ActiveOrgStore) => s.activeContext;
export const selectIsOrgContext = (s: ActiveOrgStore) =>
  s.activeContext.type === "org";
