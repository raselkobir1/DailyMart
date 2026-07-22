import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

const AUTH_ENDPOINTS = ['/auth/login', '/auth/refresh', '/auth/logout'];

/**
 * Attaches the access token to outgoing requests and, on a 401, tries exactly one silent
 * refresh-and-retry before giving up and sending the user back to /login.
 *
 * Known simplification: concurrent requests that 401 at the same time each trigger their own
 * refresh() call independently rather than sharing one in-flight refresh. Since refresh tokens are
 * rotated server-side (single-use), the second concurrent refresh would fail - acceptable for now
 * given how rarely two requests should race a token expiry at the exact same moment, but worth
 * revisiting (a shared in-flight-refresh subject) if it turns out to matter in practice.
 */
export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const isAuthEndpoint = AUTH_ENDPOINTS.some((path) => req.url.includes(path));
  const token = authService.accessToken;

  const authorizedReq = token && !isAuthEndpoint
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authorizedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAuthEndpoint) {
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
