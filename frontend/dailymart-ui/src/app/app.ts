import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';
import { MenuPermission } from './core/menu-permission.model';
import { Perms } from './core/perms';
import { ACCENT_NAMES, ACCENT_PREVIEW, AccentName, Theme } from './core/theme';
import { ToastContainerComponent } from './shared/toast-container/toast-container.component';

interface NavNode extends MenuPermission {
  children: NavNode[];
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastContainerComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly router = inject(Router);
  protected readonly authService = inject(AuthService);
  protected readonly perms = inject(Perms);
  protected readonly theme = inject(Theme);

  protected readonly accents = ACCENT_NAMES;
  protected readonly accentPreview = ACCENT_PREVIEW;
  protected readonly userMenuOpen = signal(false);

  /** Builds a parent/child tree from the flat permitted-menu list - unused today (the seeded menu set is
   * flat), but the data model supports nesting, so the sidebar already renders it correctly if it's ever
   * used (see Menu.ParentId's doc comment). */
  protected readonly navTree = computed<NavNode[]>(() => {
    const menus = this.perms.menus();
    const byParent = new Map<number | null, MenuPermission[]>();
    for (const menu of menus) {
      const key = menu.parentId ?? null;
      const list = byParent.get(key) ?? [];
      list.push(menu);
      byParent.set(key, list);
    }

    const build = (parentId: number | null): NavNode[] =>
      (byParent.get(parentId) ?? [])
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map((menu) => ({ ...menu, children: build(menu.menuId) }));

    return build(null);
  });

  protected initials(): string {
    const name = this.authService.currentUser()?.fullName ?? '';
    return name
      .split(' ')
      .map((part) => part[0])
      .filter(Boolean)
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  protected toggleUserMenu(): void {
    this.userMenuOpen.update((open) => !open);
  }

  protected closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  protected setAccent(accent: AccentName): void {
    this.theme.setAccent(accent);
  }

  protected toggleTheme(): void {
    this.theme.toggleMode();
  }

  protected logout(): void {
    this.authService.logout().subscribe({
      next: () => this.afterLogout(),
      error: () => this.afterLogout()
    });
  }

  private afterLogout(): void {
    this.perms.clear();
    this.router.navigateByUrl('/login');
  }
}
