import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthService } from './core/auth/auth.service';
import { jwtInterceptor } from './core/auth/jwt.interceptor';
import { apiBaseUrlInterceptor } from './core/interceptors/api-base-url.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { Perms } from './core/perms';
import { Theme } from './core/theme';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // Order matters: apiBaseUrlInterceptor prefixes the URL first; jwtInterceptor is listed last so it's
    // innermost and sees the response before errorInterceptor logs it - letting it silently retry a 401
    // after a token refresh instead of that attempt being logged as a hard failure.
    provideHttpClient(withInterceptors([apiBaseUrlInterceptor, errorInterceptor, jwtInterceptor])),
    // Loads the current user's permitted-menu list before the app renders any route, so the authGuard/
    // canView guards and the sidebar have real data on a hard refresh, not just after an in-app login.
    // Instantiating Theme here (rather than waiting for some component to inject it) applies the saved
    // mode/accent before first paint, avoiding a flash of the default theme.
    provideAppInitializer(() => {
      inject(Theme);
      const auth = inject(AuthService);
      const perms = inject(Perms);
      if (!auth.isAuthenticated()) {
        return Promise.resolve();
      }
      return firstValueFrom(perms.load());
    })
  ]
};
