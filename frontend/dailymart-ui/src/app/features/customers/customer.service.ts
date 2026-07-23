import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../shared/models/paged-result.model';
import { CustomerDto, CustomerLedgerEntryDto, CustomerRequest } from './customer.model';

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<CustomerDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);

    return this.http.get<PagedResult<CustomerDto>>('/customers', { params });
  }

  getById(id: number): Observable<CustomerDto> {
    return this.http.get<CustomerDto>(`/customers/${id}`);
  }

  create(request: CustomerRequest): Observable<CustomerDto> {
    return this.http.post<CustomerDto>('/customers', request);
  }

  update(id: number, request: CustomerRequest): Observable<CustomerDto> {
    return this.http.put<CustomerDto>(`/customers/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/customers/${id}`);
  }

  getLedger(id: number, request: PagedRequest): Observable<PagedResult<CustomerLedgerEntryDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);

    return this.http.get<PagedResult<CustomerLedgerEntryDto>>(`/customers/${id}/ledger`, { params });
  }
}
