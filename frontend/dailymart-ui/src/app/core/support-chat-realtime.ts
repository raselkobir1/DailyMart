import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { AuthService } from './auth/auth.service';
import { SupportMessageDto } from './support-chat.model';
import { SupportChatService } from './support-chat.service';

/**
 * Tenant side of the live support chat (SupportChatHub, backend) - the counterpart used by the platform
 * admin's chat panel is PlatformSupportChatService, a separate connection since the two sides need
 * different group-membership behavior (see SupportChatHub's doc comment: a tenant connection auto-joins
 * its own room, a platform-admin connection explicitly joins/leaves whichever tenant it's viewing).
 * Owned by SupportChatWidgetComponent (start on init, stop on destroy) - the widget is the only thing
 * that ever needs this, so there's no separate shell-level lifecycle to coordinate here unlike
 * PlatformRealtimeService/PlatformShellComponent.
 */
@Injectable({ providedIn: 'root' })
export class SupportChatRealtimeService {
  private readonly authService = inject(AuthService);
  private readonly supportChatService = inject(SupportChatService);

  private connection: signalR.HubConnection | null = null;
  private historyLoaded = false;

  private readonly messagesSignal = signal<SupportMessageDto[]>([]);
  readonly messages = this.messagesSignal.asReadonly();

  private readonly unreadCountSignal = signal(0);
  readonly unreadCount = this.unreadCountSignal.asReadonly();

  start(): void {
    if (this.connection) {
      return;
    }

    this.supportChatService.getUnreadCount().subscribe((count) => this.unreadCountSignal.set(count));

    const hubUrl = environment.apiBaseUrl.replace(/\/api\/?$/, '') + '/hubs/support-chat';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => this.authService.accessToken ?? '' })
      .withAutomaticReconnect()
      .build();

    this.connection.on('NewSupportMessage', (message: SupportMessageDto) => {
      this.messagesSignal.update((list) => [...list, message]);
      if (message.fromPlatformAdmin) {
        this.unreadCountSignal.update((count) => count + 1);
      }
    });

    // Best-effort, same reasoning as PlatformRealtimeService - a failed connection still leaves the
    // widget usable via plain REST (send/get), just without live push until reconnected.
    this.connection.start().catch(() => undefined);
  }

  stop(): void {
    this.connection?.stop();
    this.connection = null;
    this.historyLoaded = false;
  }

  /** Loaded lazily the first time the widget is actually opened, not at start() - most sessions never
   * open the chat at all. */
  ensureHistoryLoaded(): void {
    if (this.historyLoaded) {
      return;
    }
    this.historyLoaded = true;
    this.supportChatService.getConversation().subscribe((messages) => this.messagesSignal.set(messages));
  }

  sendMessage(text: string): void {
    // No local optimistic echo - the sender's own connection is already in its tenant's SignalR group,
    // so the message it just sent comes back via the same NewSupportMessage push everyone else gets.
    this.supportChatService.send(text).subscribe();
  }

  markRead(): void {
    this.unreadCountSignal.set(0);
    this.supportChatService.markRead().subscribe();
  }

  /** Called on logout so a different user logging in on the same tab doesn't inherit the previous
   * session's conversation. */
  clear(): void {
    this.messagesSignal.set([]);
    this.unreadCountSignal.set(0);
    this.historyLoaded = false;
  }
}
