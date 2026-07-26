import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthenticatedPlatformAdmin, PlatformAuthResponse, PlatformLoginRequest } from './platform-auth.models';
import { safeStorage } from './safe-storage';

const ACCESS_TOKEN_KEY = 'dailymart.platform.accessToken';
const ADMIN_KEY = 'dailymart.platform.admin';

/**
 * A wholly separate identity from AuthService/the tenant-scoped app - a platform admin manages the
 * Tenant list itself, not any one company's data (see PlatformTenantsController). Own storage keys so
 * the two sessions never collide (e.g. a browser tab open on both /login and /platform/login at once).
 * No refresh-token flow - see IPlatformAdminAuthService's doc comment on the backend for why that's an
 * acceptable tradeoff for this "basic" internal ops panel; a token that expires just requires logging
 * in again, not an involved recovery flow.
 */
@Injectable({ providedIn: 'root' })
export class PlatformAuthService {
  private readonly http = inject(HttpClient);

  private readonly accessTokenSignal = signal<string | null>(safeStorage.getItem(ACCESS_TOKEN_KEY));
  private readonly currentAdminSignal = signal<AuthenticatedPlatformAdmin | null>(this.readStoredAdmin());

  readonly isAuthenticated = computed(() => this.accessTokenSignal() !== null);
  readonly currentAdmin = this.currentAdminSignal.asReadonly();

  get accessToken(): string | null {
    return this.accessTokenSignal();
  }

  login(request: PlatformLoginRequest): Observable<PlatformAuthResponse> {
    return this.http
      .post<PlatformAuthResponse>('/platform/auth/login', request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  logout(): void {
    this.clearSession();
  }

  /** Used by the JWT interceptor on an unrecoverable 401 - there's no refresh to attempt first. */
  clearSession(): void {
    safeStorage.removeItem(ACCESS_TOKEN_KEY);
    safeStorage.removeItem(ADMIN_KEY);
    this.accessTokenSignal.set(null);
    this.currentAdminSignal.set(null);
  }

  private storeSession(response: PlatformAuthResponse): void {
    const admin: AuthenticatedPlatformAdmin = {
      username: response.username,
      fullName: response.fullName
    };

    safeStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
    safeStorage.setItem(ADMIN_KEY, JSON.stringify(admin));

    this.accessTokenSignal.set(response.accessToken);
    this.currentAdminSignal.set(admin);
  }

  private readStoredAdmin(): AuthenticatedPlatformAdmin | null {
    const raw = safeStorage.getItem(ADMIN_KEY);
    return raw ? (JSON.parse(raw) as AuthenticatedPlatformAdmin) : null;
  }
}
