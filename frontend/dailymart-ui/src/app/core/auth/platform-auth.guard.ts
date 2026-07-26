import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PlatformAuthService } from './platform-auth.service';

/** Gates the platform-admin route branch - a wholly separate identity from the tenant-scoped
 * authGuard/canView (see PlatformAuthService's doc comment); no menu/permissions concept applies
 * here, just "is there a platform-admin session." */
export const platformAuthGuard: CanActivateFn = () => {
  const platformAuthService = inject(PlatformAuthService);
  const router = inject(Router);

  return platformAuthService.isAuthenticated() ? true : router.createUrlTree(['/platform/login']);
};
