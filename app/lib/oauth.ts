/**
 * OAuth Utilities for GeneFlow.
 *
 * Handles OAuth flow for Google and GitHub providers.
 */

// Google OAuth configuration
const GOOGLE_CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID || "";

// GitHub OAuth configuration
const GITHUB_CLIENT_ID = process.env.NEXT_PUBLIC_GITHUB_CLIENT_ID || "";
const GITHUB_REDIRECT_URI = typeof window !== "undefined"
  ? `${window.location.origin}/auth/callback/github`
  : "";

/**
 * Initialize Google Identity Services and trigger sign-in.
 * Returns the ID token on success.
 */
export async function signInWithGoogle(): Promise<string> {
  return new Promise((resolve, reject) => {
    if (!GOOGLE_CLIENT_ID) {
      reject(new Error("Google OAuth is not configured"));
      return;
    }

    // Load Google Identity Services script if not already loaded
    if (!window.google?.accounts) {
      const script = document.createElement("script");
      script.src = "https://accounts.google.com/gsi/client";
      script.async = true;
      script.defer = true;
      script.onload = () => initializeGoogleSignIn(resolve, reject);
      script.onerror = () => reject(new Error("Failed to load Google Identity Services"));
      document.body.appendChild(script);
    } else {
      initializeGoogleSignIn(resolve, reject);
    }
  });
}

function initializeGoogleSignIn(
  resolve: (token: string) => void,
  reject: (error: Error) => void
) {
  try {
    window.google.accounts.id.initialize({
      client_id: GOOGLE_CLIENT_ID,
      callback: (response: { credential: string }) => {
        if (response.credential) {
          resolve(response.credential);
        } else {
          reject(new Error("No credential received from Google"));
        }
      },
      auto_select: false,
      cancel_on_tap_outside: true,
    });

    // Trigger the One Tap prompt
    window.google.accounts.id.prompt((notification: { isNotDisplayed: () => boolean; isSkippedMoment: () => boolean }) => {
      if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
        // Fall back to popup
        window.google.accounts.oauth2.initTokenClient({
          client_id: GOOGLE_CLIENT_ID,
          scope: "email profile openid",
          callback: (tokenResponse: { access_token?: string; error?: string }) => {
            if (tokenResponse.access_token) {
              resolve(tokenResponse.access_token);
            } else {
              reject(new Error(tokenResponse.error || "Failed to get Google access token"));
            }
          },
        }).requestAccessToken();
      }
    });
  } catch (error) {
    reject(error instanceof Error ? error : new Error("Google sign-in failed"));
  }
}

/**
 * Initiate GitHub OAuth flow.
 * Opens a popup window for GitHub authorization.
 * Returns the access token on success.
 */
export async function signInWithGitHub(): Promise<string> {
  return new Promise((resolve, reject) => {
    if (!GITHUB_CLIENT_ID) {
      reject(new Error("GitHub OAuth is not configured"));
      return;
    }

    const scope = "read:user user:email";
    const state = generateRandomState();

    // Store state for validation
    sessionStorage.setItem("github_oauth_state", state);

    const authUrl = `https://github.com/login/oauth/authorize?client_id=${GITHUB_CLIENT_ID}&redirect_uri=${encodeURIComponent(GITHUB_REDIRECT_URI)}&scope=${encodeURIComponent(scope)}&state=${state}`;

    const width = 500;
    const height = 600;
    const left = window.screenX + (window.outerWidth - width) / 2;
    const top = window.screenY + (window.outerHeight - height) / 2;

    const popup = window.open(
      authUrl,
      "GitHub Sign In",
      `width=${width},height=${height},left=${left},top=${top}`
    );

    if (!popup) {
      reject(new Error("Failed to open popup window. Please allow popups for this site."));
      return;
    }

    // Listen for the callback message
    const handleMessage = (event: MessageEvent) => {
      if (event.origin !== window.location.origin) return;

      if (event.data.type === "github-oauth-callback") {
        window.removeEventListener("message", handleMessage);

        if (event.data.error) {
          reject(new Error(event.data.error));
        } else if (event.data.token) {
          resolve(event.data.token);
        } else {
          reject(new Error("No token received from GitHub"));
        }

        popup.close();
      }
    };

    window.addEventListener("message", handleMessage);

    // Check if popup was closed without completing auth
    const checkClosed = setInterval(() => {
      if (popup.closed) {
        clearInterval(checkClosed);
        window.removeEventListener("message", handleMessage);
        reject(new Error("Authentication cancelled"));
      }
    }, 500);
  });
}

function generateRandomState(): string {
  const array = new Uint8Array(32);
  crypto.getRandomValues(array);
  return Array.from(array, byte => byte.toString(16).padStart(2, "0")).join("");
}

// Type declarations for Google Identity Services
declare global {
  interface Window {
    google: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string;
            callback: (response: { credential: string }) => void;
            auto_select?: boolean;
            cancel_on_tap_outside?: boolean;
          }) => void;
          prompt: (callback?: (notification: {
            isNotDisplayed: () => boolean;
            isSkippedMoment: () => boolean;
          }) => void) => void;
        };
        oauth2: {
          initTokenClient: (config: {
            client_id: string;
            scope: string;
            callback: (response: { access_token?: string; error?: string }) => void;
          }) => { requestAccessToken: () => void };
        };
      };
    };
  }
}
