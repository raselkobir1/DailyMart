import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../../shared/models/paged-result.model';
import { UnitDto, UnitRequest } from './unit.model';

@Injectable({ providedIn: 'root' })
export class UnitService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<UnitDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);

    return this.http.get<PagedResult<UnitDto>>('/units', { params });
  }

  create(request: UnitRequest): Observable<UnitDto> {
    return this.http.post<UnitDto>('/units', request);
  }

  update(id: number, request: UnitRequest): Observable<UnitDto> {
    return this.http.put<UnitDto>(`/units/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/units/${id}`);
  }
}
