import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/**
 * Prefixes relative API requests (e.g. '/audit-logs') with environment.apiBaseUrl, so feature
 * services never hardcode a host - only the environment file changes between dev/prod.
 */
export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (/^https?:\/\//i.test(req.url)) {
    return next(req);
  }

  return next(req.clone({ url: `${environment.apiBaseUrl}${req.url}` }));
};
