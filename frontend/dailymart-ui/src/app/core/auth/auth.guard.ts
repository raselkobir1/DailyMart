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
