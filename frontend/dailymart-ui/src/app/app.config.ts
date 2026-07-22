import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';

import { jwtInterceptor } from './core/auth/jwt.interceptor';
import { apiBaseUrlInterceptor } from './core/interceptors/api-base-url.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimationsAsync(),
    // Order matters: apiBaseUrlInterceptor prefixes the URL first; jwtInterceptor is listed last so it's
    // innermost and sees the response before errorInterceptor logs it - letting it silently retry a 401
    // after a token refresh instead of that attempt being logged as a hard failure.
    provideHttpClient(withInterceptors([apiBaseUrlInterceptor, errorInterceptor, jwtInterceptor]))
  ]
};
