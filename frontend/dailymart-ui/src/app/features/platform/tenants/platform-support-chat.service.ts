import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SupportMessageDto } from '../../../core/support-chat.model';

/** Nested under /platform/tenants/{id}/support-chat - matches PlatformSubscriptionService's nesting
 * convention against the same backend controller. */
@Injectable({ providedIn: 'root' })
export class PlatformSupportChatService {
  private readonly http = inject(HttpClient);

  getConversation(tenantId: number, take = 50): Observable<SupportMessageDto[]> {
    return this.http.get<SupportMessageDto[]>(
      `/platform/tenants/${tenantId}/support-chat`, { params: new HttpParams().set('take', take) });
  }

  send(tenantId: number, message: string): Observable<SupportMessageDto> {
    return this.http.post<SupportMessageDto>(`/platform/tenants/${tenantId}/support-chat`, { message });
  }

  markRead(tenantId: number): Observable<void> {
    return this.http.post<void>(`/platform/tenants/${tenantId}/support-chat/read`, {});
  }
}
