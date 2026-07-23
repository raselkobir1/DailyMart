import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../shared/models/paged-result.model';
import { SaleDto, SaleRequest } from './sale.model';

/** Create + read only - no update/delete, matching the backend's ISaleService contract (a posted sale is
 * corrected via a SaleReturn, not edited). */
@Injectable({ providedIn: 'root' })
export class SaleService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<SaleDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);

    return this.http.get<PagedResult<SaleDto>>('/sales', { params });
  }

  getById(id: number): Observable<SaleDto> {
    return this.http.get<SaleDto>(`/sales/${id}`);
  }

  create(request: SaleRequest): Observable<SaleDto> {
    return this.http.post<SaleDto>('/sales', request);
  }
}
