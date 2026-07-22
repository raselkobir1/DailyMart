import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ShopSettingsDto, UpdateShopSettingsRequest } from './settings.model';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(HttpClient);

  get(): Observable<ShopSettingsDto> {
    return this.http.get<ShopSettingsDto>('/settings');
  }

  update(request: UpdateShopSettingsRequest): Observable<ShopSettingsDto> {
    return this.http.put<ShopSettingsDto>('/settings', request);
  }

  uploadLogo(file: File): Observable<ShopSettingsDto> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ShopSettingsDto>('/settings/logo', formData);
  }
}
