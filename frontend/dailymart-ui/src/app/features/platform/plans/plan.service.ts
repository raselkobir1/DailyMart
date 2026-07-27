import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../../shared/models/paged-result.model';
import { PlanDto, PlanRequest } from './plan.model';

@Injectable({ providedIn: 'root' })
export class PlanService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<PlanDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);

    return this.http.get<PagedResult<PlanDto>>('/platform/plans', { params });
  }

  getActive(): Observable<PlanDto[]> {
    return this.http.get<PlanDto[]>('/platform/plans/active');
  }

  create(request: PlanRequest): Observable<PlanDto> {
    return this.http.post<PlanDto>('/platform/plans', request);
  }

  update(id: number, request: PlanRequest): Observable<PlanDto> {
    return this.http.put<PlanDto>(`/platform/plans/${id}`, request);
  }

  activate(id: number): Observable<PlanDto> {
    return this.http.post<PlanDto>(`/platform/plans/${id}/activate`, {});
  }

  deactivate(id: number): Observable<PlanDto> {
    return this.http.post<PlanDto>(`/platform/plans/${id}/deactivate`, {});
  }
}
