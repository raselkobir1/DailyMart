import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { PlatformAuthService } from './platform-auth.service';

const AUTH_ENDPOINTS = ['/auth/login', '/auth/refresh', '/auth/logout'];
const PLATFORM_PREFIX = '/platform/';
const PLATFORM_AUTH_ENDPOINTS = ['/platform/auth/login'];

/**
 * Attaches the right access token to outgoing requests - the platform-admin token for `/platform/*`
 * calls (a wholly separate session from the tenant-scoped one, see PlatformAuthService), the regular
 * tenant token otherwise - and handles a 401 for each accordingly.
 *
 * Tenant requests get one silent refresh-and-retry before giving up and sending the user back to
 * /login. Platform requests have no refresh flow (see PlatformAuthService's doc comment) - a 401
 * there just clears the platform session and redirects to /platform/login.
 *
 * Known simplification (tenant flow only): concurrent requests that 401 at the same time each
 * trigger their own refresh() call independently rather than sharing one in-flight refresh. Since
 * refresh tokens are rotated server-side (single-use), the second concurrent refresh would fail -
 * acceptable for now given how rarely two requests should race a token expiry at the exact same
 * moment, but worth revisiting (a shared in-flight-refresh subject) if it turns out to matter in
 * practice.
 */
export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const platformAuthService = inject(PlatformAuthService);
  const router = inject(Router);

  const isPlatformRequest = req.url.includes(PLATFORM_PREFIX);
  const isAuthEndpoint = isPlatformRequest
    ? PLATFORM_AUTH_ENDPOINTS.some((path) => req.url.includes(path))
    : AUTH_ENDPOINTS.some((path) => req.url.includes(path));

  const token = isPlatformRequest ? platformAuthService.accessToken : authService.accessToken;

  const authorizedReq = token && !isAuthEndpoint
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authorizedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAuthEndpoint) {
        return throwError(() => error);
      }

      if (isPlatformRequest) {
        platformAuthService.clearSession();
        router.navigateByUrl('/platform/login');
        return throwError(() => error);
      }

      if (!authService.getRefreshToken()) {
        authService.clearSession();
        router.navigateByUrl('/login');
        return throwError(() => error);
      }

      return authService.refresh().pipe(
        switchMap(() => {
          const retriedReq = req.clone({
            setHeaders: { Authorization: `Bearer ${authService.accessToken}` }
          });
          return next(retriedReq);
        }),
        catchError((refreshError) => {
          authService.clearSession();
          router.navigateByUrl('/login');
          return throwError(() => refreshError);
        })
      );
    })
  );
};
