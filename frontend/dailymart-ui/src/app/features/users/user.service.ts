import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../shared/models/paged-result.model';
import { CreateUserRequest, UpdateUserRequest, UserDto } from './user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<UserDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);

    return this.http.get<PagedResult<UserDto>>('/users', { params });
  }

  create(request: CreateUserRequest): Observable<UserDto> {
    return this.http.post<UserDto>('/users', request);
  }

  update(id: number, request: UpdateUserRequest): Observable<UserDto> {
    return this.http.put<UserDto>(`/users/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/users/${id}`);
  }
}
