import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

/**
 * Central place to log/observe failed API calls. Redirect-on-401 will be added in Module 1
 * (Authentication) once there's a login route to redirect to.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      console.error(`API error on ${req.method} ${req.url}:`, error.status, error.error);
      return throwError(() => error);
    })
  );
