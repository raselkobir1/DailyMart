import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../../shared/models/paged-result.model';
import { CategoryDto, CategoryRequest } from './category.model';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<CategoryDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);

    return this.http.get<PagedResult<CategoryDto>>('/categories', { params });
  }

  create(request: CategoryRequest): Observable<CategoryDto> {
    return this.http.post<CategoryDto>('/categories', request);
  }

  update(id: number, request: CategoryRequest): Observable<CategoryDto> {
    return this.http.put<CategoryDto>(`/categories/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/categories/${id}`);
  }
}
