import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../shared/models/paged-result.model';
import { ExpenseDto, ExpenseRequest } from './expense.model';

export interface ExpenseFilter extends PagedRequest {
  category?: number | null;
  fromDate?: string | null;
  toDate?: string | null;
}

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private readonly http = inject(HttpClient);

  getPaged(request: ExpenseFilter): Observable<PagedResult<ExpenseDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);
    if (request.category !== null && request.category !== undefined) params = params.set('category', request.category);
    if (request.fromDate) params = params.set('fromDate', `${request.fromDate}T00:00:00.000Z`);
    if (request.toDate) params = params.set('toDate', `${request.toDate}T23:59:59.999Z`);

    return this.http.get<PagedResult<ExpenseDto>>('/expenses', { params });
  }

  getById(id: number): Observable<ExpenseDto> {
    return this.http.get<ExpenseDto>(`/expenses/${id}`);
  }

  create(request: ExpenseRequest): Observable<ExpenseDto> {
    return this.http.post<ExpenseDto>('/expenses', request);
  }

  update(id: number, request: ExpenseRequest): Observable<ExpenseDto> {
    return this.http.put<ExpenseDto>(`/expenses/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/expenses/${id}`);
  }
}
