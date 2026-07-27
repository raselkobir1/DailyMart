import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../../shared/models/paged-result.model';
import { PlatformTenantDto } from './platform-tenant.model';

@Injectable({ providedIn: 'root' })
export class PlatformTenantService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<PlatformTenantDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);

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
