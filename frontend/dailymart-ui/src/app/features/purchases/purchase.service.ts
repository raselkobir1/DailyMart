import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../shared/models/paged-result.model';
import { PurchaseDto, PurchaseRequest } from './purchase.model';

@Injectable({ providedIn: 'root' })
export class PurchaseService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<PurchaseDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);

    return this.http.get<PagedResult<PurchaseDto>>('/purchases', { params });
  }

  getById(id: number): Observable<PurchaseDto> {
    return this.http.get<PurchaseDto>(`/purchases/${id}`);
  }

  create(request: PurchaseRequest): Observable<PurchaseDto> {
    return this.http.post<PurchaseDto>('/purchases', request);
  }

  update(id: number, request: PurchaseRequest): Observable<PurchaseDto> {
    return this.http.put<PurchaseDto>(`/purchases/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/purchases/${id}`);
  }
}
