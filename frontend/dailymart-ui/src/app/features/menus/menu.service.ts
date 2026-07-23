import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateMenuRequest, MenuDto, MenuRequest } from './menu.model';

/** Unpaginated - see the backend's IMenuService doc comment: Menus is a small, fully admin-managed set,
 * and both this page and the Permissions matrix need the complete list every time. */
@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<MenuDto[]> {
    return this.http.get<MenuDto[]>('/menus');
  }

  create(request: CreateMenuRequest): Observable<MenuDto> {
    return this.http.post<MenuDto>('/menus', request);
  }

  update(id: number, request: MenuRequest): Observable<MenuDto> {
    return this.http.put<MenuDto>(`/menus/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/menus/${id}`);
  }
}
