import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { MenuPermissionResponse, SetPermissionsRequest } from './menu-permission.model';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly http = inject(HttpClient);

  getForRole(roleId: number): Observable<MenuPermissionResponse[]> {
    return this.http.get<MenuPermissionResponse[]>(`/roles/${roleId}/permissions`);
  }

  setForRole(roleId: number, request: SetPermissionsRequest): Observable<void> {
    return this.http.put<void>(`/roles/${roleId}/permissions`, request);
  }
}
