import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../shared/models/paged-result.model';
import { RoleDto, RoleRequest } from './role.model';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<RoleDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);

    return this.http.get<PagedResult<RoleDto>>('/roles', { params });
  }

  create(request: RoleRequest): Observable<RoleDto> {
    return this.http.post<RoleDto>('/roles', request);
  }

  update(id: number, request: RoleRequest): Observable<RoleDto> {
    return this.http.put<RoleDto>(`/roles/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/roles/${id}`);
  }
}
