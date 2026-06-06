"use client";

import * as React from "react";
import {
  MutationCache,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import { ApiClientError } from "@/lib/api-client";
import {
  isSubscriptionLimitCode,
  useSubscriptionLimitStore,
} from "@/stores/subscription-limit-store";

function makeQueryClient() {
  return new QueryClient({
    // Catch backend `SubscriptionLimitBehavior` errors centrally so every
    // mutation (create study, upload trace, invite member, ...) raises the
    // same upgrade dialog without each call site reimplementing it.
    mutationCache: new MutationCache({
      onError: (error) => {
        if (
          error instanceof ApiClientError &&
          error.status === 403 &&
          isSubscriptionLimitCode(error.code)
        ) {
          useSubscriptionLimitStore.getState().open({
            code: error.code,
            message: error.message,
          });
        }
      },
    }),
    defaultOptions: {
      queries: {
        staleTime: 60 * 1000,
        refetchOnWindowFocus: false,
        retry: 1,
      },
    },
  });
}

let browserQueryClient: QueryClient | undefined = undefined;

function getQueryClient() {
  if (typeof window === "undefined") {
    return makeQueryClient();
  } else {
    if (!browserQueryClient) {
      browserQueryClient = makeQueryClient();
    }
    return browserQueryClient;
  }
}

interface QueryProviderProps {
  children: React.ReactNode;
}

export function QueryProvider({ children }: QueryProviderProps) {
  const queryClient = getQueryClient();

  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}
