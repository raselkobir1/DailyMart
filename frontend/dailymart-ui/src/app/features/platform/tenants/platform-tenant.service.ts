import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../../shared/models/paged-result.model';
import { PlatformTenantDto } from './platform-tenant.model';

@Injectable({ providedIn: 'root' })
export class PlatformTenantService {
  private readonly http = inject(HttpClient);

  getPaged(
    request: PagedRequest,
    status?: 'active' | 'suspended',
    billingStatus?: 'overdue' | 'paid' | 'free'
  ): Observable<PagedResult<PlatformTenantDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortDescending) params = params.set('sortDescending', request.sortDescending);
    if (status) params = params.set('status', status);
    if (billingStatus) params = params.set('billingStatus', billingStatus);

    return this.http.get<PagedResult<PlatformTenantDto>>('/platform/tenants', { params });
  }

  getById(id: number): Observable<PlatformTenantDto> {
    return this.http.get<PlatformTenantDto>(`/platform/tenants/${id}`);
  }

  activate(id: number): Observable<PlatformTenantDto> {
    return this.http.post<PlatformTenantDto>(`/platform/tenants/${id}/activate`, {});
  }

  suspend(id: number): Observable<PlatformTenantDto> {
    return this.http.post<PlatformTenantDto>(`/platform/tenants/${id}/suspend`, {});
  }
}
