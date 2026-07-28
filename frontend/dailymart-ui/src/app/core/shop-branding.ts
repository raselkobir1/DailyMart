import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { SettingsService } from '../features/settings/settings.service';

/**
 * Holds the current tenant's Shop Name (from ShopSettings, edited on the Settings page) for display in
 * the app shell's brand area - see app.html. Loaded once at app bootstrap (provideAppInitializer in
 * app.config.ts) alongside Perms, and pushed a fresh value directly by SettingsFormComponent right after
 * a successful save/logo-upload, so a Shop Name change is visible immediately in the same session without
 * needing to log out and back in. Deliberately separate from Tenant.Name (the company name set at
 * signup, carried in AuthService.currentUser().companyName) - ShopSettings.ShopName is the operational
 * name a shop's own Admin can rename any time, which is what the sidebar should reflect.
 */
@Injectable({ providedIn: 'root' })
export class ShopBranding {
  private readonly settingsService = inject(SettingsService);

  readonly shopName = signal<string | null>(null);

  load(): Observable<string | null> {
    return this.settingsService.get().pipe(
      tap((settings) => this.shopName.set(settings.shopName)),
      map((settings) => settings.shopName),
      catchError(() => {
        this.shopName.set(null);
        return of(null);
      })
    );
  }

  setShopName(name: string): void {
    this.shopName.set(name);
  }

  clear(): void {
    this.shopName.set(null);
  }
}
