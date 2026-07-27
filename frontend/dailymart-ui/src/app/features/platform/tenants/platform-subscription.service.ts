import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../../shared/models/paged-result.model';
import {
  ChangePlanRequest,
  RecordPaymentRequest,
  SubscriptionPaymentDto,
  TenantSubscriptionDto
} from './platform-subscription.model';

/** Nested under /platform/tenants/{id}/subscription - matches the backend's PlatformTenantsController,
 * which carries these routes alongside the tenant list/suspend endpoints rather than a separate
 * controller (see PurchasesController's {id}/returns for the same nesting convention). */
@Injectable({ providedIn: 'root' })
export class PlatformSubscriptionService {
  private readonly http = inject(HttpClient);

  get(tenantId: number): Observable<TenantSubscriptionDto> {
    return this.http.get<TenantSubscriptionDto>(`/platform/tenants/${tenantId}/subscription`);
  }

  getPaymentHistory(tenantId: number, request: PagedRequest): Observable<PagedResult<SubscriptionPaymentDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);

    return this.http.get<PagedResult<SubscriptionPaymentDto>>(
      `/platform/tenants/${tenantId}/subscription/payments`, { params });
  }

  changePlan(tenantId: number, request: ChangePlanRequest): Observable<TenantSubscriptionDto> {
    return this.http.post<TenantSubscriptionDto>(`/platform/tenants/${tenantId}/subscription/change-plan`, request);
  }

  recordPayment(tenantId: number, request: RecordPaymentRequest): Observable<SubscriptionPaymentDto> {
    return this.http.post<SubscriptionPaymentDto>(`/platform/tenants/${tenantId}/subscription/payments`, request);
  }
}
