import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../../shared/models/paged-result.model';
import { BrandDto, BrandRequest } from './brand.model';

@Injectable({ providedIn: 'root' })
export class BrandService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<BrandDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);

    return this.http.get<PagedResult<BrandDto>>('/brands', { params });
  }

  create(request: BrandRequest): Observable<BrandDto> {
    return this.http.post<BrandDto>('/brands', request);
  }

  update(id: number, request: BrandRequest): Observable<BrandDto> {
    return this.http.put<BrandDto>(`/brands/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/brands/${id}`);
  }
}
