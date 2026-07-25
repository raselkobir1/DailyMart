import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../shared/models/paged-result.model';
import { AuditLogDto, AuditLogFilter } from './audit-log.model';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest, filter?: AuditLogFilter): Observable<PagedResult<AuditLogDto>> {
    let params = new HttpParams();

    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortDescending) params = params.set('sortDescending', request.sortDescending);

    if (filter?.entityName) params = params.set('entityName', filter.entityName);
    if (filter?.action) params = params.set('action', filter.action);
    if (filter?.fromDate) params = params.set('fromDate', filter.fromDate);
    if (filter?.toDate) params = params.set('toDate', filter.toDate);

    return this.http.get<PagedResult<AuditLogDto>>('/audit-logs', { params });
  }

  /** Backs the entity-type filter dropdown - distinct EntityName values actually present in the log. */
  getEntityNames(): Observable<string[]> {
    return this.http.get<string[]>('/audit-logs/entity-names');
  }
}
