import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../../shared/models/paged-result.model';
import { PurchaseReturnDto, PurchaseReturnRequest } from './purchase-return.model';

/** Create + read only - no update/delete, matching the backend's IPurchaseReturnService contract. */
@Injectable({ providedIn: 'root' })
export class PurchaseReturnService {
  private readonly http = inject(HttpClient);

  getPaged(purchaseId: number, request: PagedRequest): Observable<PagedResult<PurchaseReturnDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);

    return this.http.get<PagedResult<PurchaseReturnDto>>(`/purchases/${purchaseId}/returns`, { params });
  }

  getById(purchaseId: number, returnId: number): Observable<PurchaseReturnDto> {
    return this.http.get<PurchaseReturnDto>(`/purchases/${purchaseId}/returns/${returnId}`);
  }

  create(purchaseId: number, request: PurchaseReturnRequest): Observable<PurchaseReturnDto> {
    return this.http.post<PurchaseReturnDto>(`/purchases/${purchaseId}/returns`, request);
  }
}
