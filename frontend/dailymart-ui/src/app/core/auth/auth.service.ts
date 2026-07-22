import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import {
  AuthResponse,
  AuthenticatedUser,
  ChangePasswordRequest,
  LoginRequest,
  RefreshTokenRequest
} from './auth.models';
import { safeStorage } from './safe-storage';

const ACCESS_TOKEN_KEY = 'dailymart.accessToken';
const REFRESH_TOKEN_KEY = 'dailymart.refreshToken';
const USER_KEY = 'dailymart.user';

/**
 * Both tokens are kept in localStorage rather than an httpOnly cookie for the refresh token. That
 * cookie approach is more resistant to XSS token theft, but needs cookie/CORS/SameSite plumbing on the
 * API side. This is a single-shop, single-admin internal tool (not public-facing/multi-tenant), so the
 * simpler storage was chosen - revisit if this app ever needs a stronger threat model.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  // Storage is a write-behind persistence layer for surviving page reloads, not the source of truth
  // during a session - these signals are, so the service still works correctly (just without surviving
  // a reload) if storage is unavailable. A real gap here (getRefreshToken() reading storage directly on
  // every call) surfaced as a test failure when the test environment's localStorage was a no-op.
  private readonly accessTokenSignal = signal<string | null>(safeStorage.getItem(ACCESS_TOKEN_KEY));
  private readonly refreshTokenSignal = signal<string | null>(safeStorage.getItem(REFRESH_TOKEN_KEY));
  private readonly currentUserSignal = signal<AuthenticatedUser | null>(this.readStoredUser());

  readonly isAuthenticated = computed(() => this.accessTokenSignal() !== null);
  readonly currentUser = this.currentUserSignal.asReadonly();

  get accessToken(): string | null {
    return this.accessTokenSignal();
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/auth/login', request).pipe(tap((response) => this.storeSession(response)));
  }

  /** Rotates the refresh token server-side - the caller doesn't need to do anything special with that. */
  refresh(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available.');
    }

    const request: RefreshTokenRequest = { refreshToken };
    return this.http.post<AuthResponse>('/auth/refresh', request).pipe(tap((response) => this.storeSession(response)));
  }

  logout(): Observable<void> {
    const request: RefreshTokenRequest = { refreshToken: this.getRefreshToken() ?? '' };
    return this.http.post<void>('/auth/logout', request).pipe(tap(() => this.clearSession()));
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>('/auth/change-password', request);
  }

  getRefreshToken(): string | null {
    return this.refreshTokenSignal();
  }

  /** Used by the JWT interceptor on an unrecoverable 401 (e.g. refresh itself failed). */
  clearSession(): void {
    safeStorage.removeItem(ACCESS_TOKEN_KEY);
    safeStorage.removeItem(REFRESH_TOKEN_KEY);
    safeStorage.removeItem(USER_KEY);
    this.accessTokenSignal.set(null);
    this.refreshTokenSignal.set(null);
    this.currentUserSignal.set(null);
  }

  private storeSession(response: AuthResponse): void {
    const user: AuthenticatedUser = {
      username: response.username,
      fullName: response.fullName,
      role: response.role
    };

    safeStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
    safeStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    safeStorage.setItem(USER_KEY, JSON.stringify(user));

    this.accessTokenSignal.set(response.accessToken);
    this.refreshTokenSignal.set(response.refreshToken);
    this.currentUserSignal.set(user);
  }

  private readStoredUser(): AuthenticatedUser | null {
    const raw = safeStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthenticatedUser) : null;
  }
}
