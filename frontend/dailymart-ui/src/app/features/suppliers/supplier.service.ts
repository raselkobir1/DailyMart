import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../shared/models/paged-result.model';
import { CreateSupplierRequest, SupplierDto, SupplierLedgerEntryDto, SupplierRequest } from './supplier.model';

@Injectable({ providedIn: 'root' })
export class SupplierService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<SupplierDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);

    return this.http.get<PagedResult<SupplierDto>>('/suppliers', { params });
  }

  getById(id: number): Observable<SupplierDto> {
    return this.http.get<SupplierDto>(`/suppliers/${id}`);
  }

  create(request: CreateSupplierRequest): Observable<SupplierDto> {
    return this.http.post<SupplierDto>('/suppliers', request);
  }

  update(id: number, request: SupplierRequest): Observable<SupplierDto> {
    return this.http.put<SupplierDto>(`/suppliers/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/suppliers/${id}`);
  }

  getLedger(id: number, request: PagedRequest): Observable<PagedResult<SupplierLedgerEntryDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);

    return this.http.get<PagedResult<SupplierLedgerEntryDto>>(`/suppliers/${id}/ledger`, { params });
  }
}
