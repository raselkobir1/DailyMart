/**
 * Defensive wrapper around localStorage: some environments (privacy mode, and - as hit while testing
 * this module - Angular's Vitest-based unit-test runner) don't expose a working `localStorage`, and a
 * direct access throws rather than returning undefined. Every call site would otherwise need its own
 * try/catch; centralizing it here keeps AuthService readable.
 */
export const safeStorage = {
  getItem(key: string): string | null {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  },
  setItem(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      // Best-effort - if storage isn't available, the session simply won't persist across reloads.
    }
  },
  removeItem(key: string): void {
    try {
      localStorage.removeItem(key);
    } catch {
      // no-op
    }
  }
};
