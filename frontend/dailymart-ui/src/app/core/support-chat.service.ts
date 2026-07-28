import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SupportMessageDto } from './support-chat.model';

/** Tenant side of the support conversation - always the current tenant, resolved server-side from the
 * JWT. See SupportChatController on the backend. */
@Injectable({ providedIn: 'root' })
export class SupportChatService {
  private readonly http = inject(HttpClient);

  getConversation(take = 50): Observable<SupportMessageDto[]> {
    return this.http.get<SupportMessageDto[]>('/support-chat', { params: new HttpParams().set('take', take) });
  }

  send(message: string): Observable<SupportMessageDto> {
    return this.http.post<SupportMessageDto>('/support-chat', { message });
  }

  markRead(): Observable<void> {
    return this.http.post<void>('/support-chat/read', {});
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>('/support-chat/unread-count');
  }
}
