import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../../shared/models/paged-result.model';
import { SaleReturnDto, SaleReturnRequest } from './sale-return.model';

/** Create + read only - no update/delete, matching the backend's ISaleReturnService contract. */
@Injectable({ providedIn: 'root' })
export class SaleReturnService {
  private readonly http = inject(HttpClient);

  getPaged(saleId: number, request: PagedRequest): Observable<PagedResult<SaleReturnDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);

    return this.http.get<PagedResult<SaleReturnDto>>(`/sales/${saleId}/returns`, { params });
  }

  getById(saleId: number, returnId: number): Observable<SaleReturnDto> {
    return this.http.get<SaleReturnDto>(`/sales/${saleId}/returns/${returnId}`);
  }

  create(saleId: number, request: SaleReturnRequest): Observable<SaleReturnDto> {
    return this.http.post<SaleReturnDto>(`/sales/${saleId}/returns`, request);
  }
}
