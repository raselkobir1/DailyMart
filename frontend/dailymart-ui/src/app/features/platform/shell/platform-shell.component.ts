import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { PlatformAuthService } from '../../../core/auth/platform-auth.service';
import { Theme } from '../../../core/theme';

/**
 * The platform-admin panel's shell - reuses the exact sidebar/topbar/main-column CSS classes the
 * tenant-facing app shell (app.html) already defines in styles.scss, so this panel looks like part of
 * the same product instead of a bare, unstyled afterthought. Deliberately simpler than the tenant
 * shell: a flat 2-item nav (no nested menu groups, no RBAC - platformAuthGuard is "is there a platform-
 * admin session" full stop, see its own doc comment) and no accent-color picker, just the light/dark
 * toggle. Wraps every /platform/* route except /platform/login itself (see app.routes.ts - the login
 * page has nothing to navigate to yet).
 */
@Component({
  selector: 'app-platform-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './platform-shell.component.html',
  styleUrl: './platform-shell.component.scss'
})
export class PlatformShellComponent {
  private readonly router = inject(Router);
  protected readonly platformAuthService = inject(PlatformAuthService);
  protected readonly theme = inject(Theme);

  protected initials(): string {
    const name = this.platformAuthService.currentAdmin()?.fullName ?? '';
    return name
      .split(' ')
      .map((part) => part[0])
      .filter(Boolean)
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  protected toggleTheme(): void {
    this.theme.toggleMode();
  }

  protected logout(): void {
    this.platformAuthService.logout();
    this.router.navigateByUrl('/platform/login');
  }
}
