import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PlatformNotificationDto } from './platform-notification.model';

@Injectable({ providedIn: 'root' })
export class PlatformNotificationService {
  private readonly http = inject(HttpClient);

  getRecent(take = 20): Observable<PlatformNotificationDto[]> {
    return this.http.get<PlatformNotificationDto[]>('/platform/notifications', { params: new HttpParams().set('take', take) });
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>('/platform/notifications/unread-count');
  }

  markAsRead(id: number): Observable<void> {
    return this.http.post<void>(`/platform/notifications/${id}/read`, {});
  }
}
