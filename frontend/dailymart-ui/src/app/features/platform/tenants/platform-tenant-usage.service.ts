import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantUsageSnapshotDto } from './platform-tenant-usage.model';

/** Nested under /platform/tenants/{id}/usage - same PlatformTenantsController nesting convention as
 * platform-subscription.service.ts's /subscription routes. */
@Injectable({ providedIn: 'root' })
export class PlatformTenantUsageService {
  private readonly http = inject(HttpClient);

  get(tenantId: number): Observable<TenantUsageSnapshotDto> {
    return this.http.get<TenantUsageSnapshotDto>(`/platform/tenants/${tenantId}/usage`);
  }
}
