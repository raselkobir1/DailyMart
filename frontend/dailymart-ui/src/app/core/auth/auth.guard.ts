import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Perms } from '../perms';
import { AuthService } from './auth.service';

/** Gates the whole Shell layout route - must be authenticated AND have at least one visible menu (an
 * account whose role permits nothing is treated the same as "not signed in", matching the reference
 * app's "no admin access" outcome). */
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const perms = inject(Perms);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  if (perms.loaded() && perms.menus().length === 0) {
    return router.createUrlTree(['/login']);
  }

  return true;
};

/** Gates the login/register/landing routes - an already-authenticated session landing here (e.g. via the
 * browser back button right after logging in, or navigating to the public marketing page at '/') should
 * bounce straight into the app instead of rendering a guest-facing page, since app.html's shell/sidebar
 * toggle keys off isAuthenticated() alone, not the route. Targets /dashboard explicitly (not '/') since
 * '/' is itself one of the guest-only routes this guards - redirecting there would just re-trigger this
 * same check. */
export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.isAuthenticated() ? router.createUrlTree(['/dashboard']) : true;
};

/** Per-route factory guard - denies access to a menu the current user's role can't view, redirecting to
 * their first permitted menu instead (never a blank/broken page). */
export function canView(menuKey: string): CanActivateFn {
  return () => {
    const perms = inject(Perms);
    const router = inject(Router);

    if (perms.canView(menuKey)) {
      return true;
    }

    const first = perms.menus()[0];
    return router.createUrlTree([first ? first.route : '/login']);
  };
}
