import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';
import { MenuPermission } from './menu-permission.model';

/**
 * Holds the current user's permitted-menu list (fetched from GET /api/auth/me/permissions - never
 * decoded from the JWT, see JwtTokenGenerator's doc comment). Loaded once at app bootstrap
 * (provideAppInitializer in app.config.ts) and again right after login, so a permission change an admin
 * makes takes effect for other signed-in tabs the next time they navigate, without needing to log out.
 */
@Injectable({ providedIn: 'root' })
export class Perms {
  private readonly http = inject(HttpClient);

  readonly menus = signal<MenuPermission[]>([]);
  readonly loaded = signal(false);
  /** True only when the last load() call failed (network/server error), as opposed to succeeding with
   * zero permitted menus - callers that need to tell "couldn't check" apart from "genuinely no access"
   * (e.g. the login screen) should check this before treating an empty menus() as "no access". Existing
   * callers (the route guards) intentionally keep treating both cases the same way they always have -
   * this only adds a way to distinguish them, it doesn't change what guards do with an empty list. */
  readonly lastLoadFailed = signal(false);

  load(): Observable<MenuPermission[]> {
    return this.http.get<MenuPermission[]>('/auth/me/permissions').pipe(
      tap((menus) => {
        this.menus.set(menus);
        this.loaded.set(true);
        this.lastLoadFailed.set(false);
      }),
      catchError(() => {
        this.menus.set([]);
        this.loaded.set(true);
        this.lastLoadFailed.set(true);
        return of([]);
      })
    );
  }

  clear(): void {
    this.menus.set([]);
    this.loaded.set(false);
    this.lastLoadFailed.set(false);
  }

  canView(menuKey: string): boolean {
    return this.menus().some((m) => m.menuKey === menuKey && m.canView);
  }

  can(menuKey: string, action: 'create' | 'edit' | 'delete'): boolean {
    const menu = this.menus().find((m) => m.menuKey === menuKey);
    if (!menu) {
      return false;
    }
    if (action === 'create') return menu.canCreate;
    if (action === 'edit') return menu.canEdit;
    return menu.canDelete;
  }
}
