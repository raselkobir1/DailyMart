import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { PlatformAuthService } from './auth/platform-auth.service';
import { SupportMessageDto } from './support-chat.model';

export interface SupportChatUpdatedEvent {
  tenantId: number;
}

/**
 * Platform-admin side of the live support chat (SupportChatHub, backend) - a separate connection from
 * the tenant-side SupportChatRealtimeService, since a platform admin's group membership works
 * differently: not auto-joined to any one tenant's room (they might view several in one session), so
 * joinTenantConversation/leaveTenantConversation explicitly move this connection in and out of a specific
 * tenant's room as PlatformTenantDetailComponent opens/closes. Owned by PlatformShellComponent (start on
 * init, stop on logout/destroy) so newMessage$/conversationUpdated$ keep working across the whole panel,
 * not just while a specific tenant's detail page is open - the Companies list needs conversationUpdated$
 * live too.
 */
@Injectable({ providedIn: 'root' })
export class PlatformSupportChatRealtimeService {
  private readonly platformAuthService = inject(PlatformAuthService);

  private connection: signalR.HubConnection | null = null;
  private startPromise: Promise<void> | null = null;

  private readonly newMessageSubject = new Subject<SupportMessageDto>();
  readonly newMessage$ = this.newMessageSubject.asObservable();

  private readonly conversationUpdatedSubject = new Subject<SupportChatUpdatedEvent>();
  readonly conversationUpdated$ = this.conversationUpdatedSubject.asObservable();

  start(): void {
    if (this.connection) {
      return;
    }

    const hubUrl = environment.apiBaseUrl.replace(/\/api\/?$/, '') + '/hubs/support-chat';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => this.platformAuthService.accessToken ?? '' })
      .withAutomaticReconnect()
      .build();

    this.connection.on('NewSupportMessage', (message: SupportMessageDto) => this.newMessageSubject.next(message));
    this.connection.on('SupportChatUpdated', (event: SupportChatUpdatedEvent) => this.conversationUpdatedSubject.next(event));

    this.startPromise = this.connection.start().catch(() => undefined);
  }

  stop(): void {
    this.connection?.stop();
    this.connection = null;
    this.startPromise = null;
  }

  /** Awaits the connection actually being up first - a component can call this immediately on its own
   * ngOnInit, before the shell-level start() above has necessarily finished its handshake. */
  async joinTenantConversation(tenantId: number): Promise<void> {
    await this.startPromise;
    await this.connection?.invoke('JoinTenantConversation', tenantId).catch(() => undefined);
  }

  async leaveTenantConversation(tenantId: number): Promise<void> {
    await this.startPromise;
    await this.connection?.invoke('LeaveTenantConversation', tenantId).catch(() => undefined);
  }
}
