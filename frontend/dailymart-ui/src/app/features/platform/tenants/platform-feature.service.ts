import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantMenuAvailabilityDto } from './platform-feature.model';

/** Nested under /platform/tenants/{id}/features - matches PlatformSubscriptionService's nesting
 * convention against the same backend controller. Restricted (IsGenerallyAvailable=false) menus are
 * the only ones grant/revoke ever applies to; everything else always comes back IsAvailable=true with
 * no grant of its own. */
@Injectable({ providedIn: 'root' })
export class PlatformFeatureService {
  private readonly http = inject(HttpClient);

  getFeatures(tenantId: number): Observable<TenantMenuAvailabilityDto[]> {
    return this.http.get<TenantMenuAvailabilityDto[]>(`/platform/tenants/${tenantId}/features`);
  }

  grant(tenantId: number, menuId: number): Observable<void> {
    return this.http.post<void>(`/platform/tenants/${tenantId}/features/${menuId}/grant`, {});
  }

  revoke(tenantId: number, menuId: number): Observable<void> {
    return this.http.post<void>(`/platform/tenants/${tenantId}/features/${menuId}/revoke`, {});
  }
}
