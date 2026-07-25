import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthService } from './core/auth/auth.service';
import { MenuPermission } from './core/menu-permission.model';
import { NavHistory } from './core/nav-history';
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
  /** Constructed here (root component) purely so it starts observing navigation from app bootstrap -
   * every page's "Back" button injects the same singleton to actually use it. */
  private readonly navHistory = inject(NavHistory);

  protected readonly accents = ACCENT_NAMES;
  protected readonly accentPreview = ACCENT_PREVIEW;
  protected readonly userMenuOpen = signal(false);

  /** Builds a parent/child tree from the flat permitted-menu list, one level deep (grandchildren aren't
   * rendered - the seeded set never nests that deep, see Menu.ParentId's doc comment). */
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

  /** Explicit arrow-click overrides, keyed by menuId - collapsed by default (no entry), and a group with
   * no override still shows expanded (see isExpanded) while the current page is one of its children, so
   * navigating there directly doesn't hide it. An override always wins over that route-based default:
   * without this, clicking the arrow on an auto-expanded group would toggle an override that the route
   * match then ignored, so the arrow could look like it does nothing (or need two clicks) on whichever
   * group the active page happens to belong to. */
  private readonly expandOverrides = signal<Map<number, boolean>>(new Map());

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  protected isExpanded(item: NavNode): boolean {
    const override = this.expandOverrides().get(item.menuId);
    if (override !== undefined) {
      return override;
    }

    return this.hasActiveChild(item);
  }

  /** True while the current page is one of this group's children - independent of expand/collapse state,
   * so a manually-collapsed group can still show a "you're in here" cue. Kept as a text-color cue only
   * (see .nav-group.child-active in styles.scss), not the full hover/active pill background, so it reads
   * as "this section contains your page" rather than looking like the parent row itself got clicked. */
  protected hasActiveChild(item: NavNode): boolean {
    return item.children.some((child) => this.currentUrl() === child.route || this.currentUrl().startsWith(child.route + '/'));
  }

  /** Accordion behavior: expanding one top-level group explicitly collapses every other one, even a group
   * that was only showing expanded via the hasActiveChild default (not an override) - otherwise that group
   * would keep looking "open" alongside the one just clicked. */
  protected toggleExpand(item: NavNode): void {
    const collapseIt = this.isExpanded(item);
    const next = new Map<number, boolean>();
    for (const node of this.navTree()) {
      if (node.menuId !== item.menuId) {
        next.set(node.menuId, false);
      }
    }
    next.set(item.menuId, !collapseIt);
    this.expandOverrides.set(next);
  }

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
