import { Injectable, computed, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { PlatformAuthService } from './auth/platform-auth.service';
import { PlatformNotificationService } from './platform-notification.service';

export interface NewTenantSignupEvent {
  tenantId: number;
  tenantName: string;
  adminUsername: string;
}

export interface PlatformNotificationItem extends NewTenantSignupEvent {
  id: number;
  receivedAt: Date;
  read: boolean;
}

/**
 * Live push from PlatformNotificationHub (backend) - a second, complementary channel alongside the
 * email notification and the persisted record (PlatformNotificationService/IPlatformNotificationStore on
 * the backend), for a platform admin actively watching the panel right now rather than checking their
 * inbox or reopening the panel later. Connection lifecycle is owned by PlatformShellComponent (start on
 * init, stop on logout/destroy).
 *
 * Holds two things for consumers: newTenantSignup$ (a raw, fire-once-per-event stream -
 * PlatformTenantListComponent uses this just to reload its own list live) and notifications/unreadCount
 * (backing the shell's notification bell). start() seeds both from the backend (GET
 * /platform/notifications + /unread-count) before opening the live connection, so a signup that happened
 * while nobody was connected is still there - see IPlatformNotificationStore's doc comment on the
 * backend for why that persistence exists. Every live-pushed item carries the same real database Id the
 * history endpoint would return for it, so there's never a mismatch between "seen live" and "seen on
 * reopen." An item stays in the list, and counted as unread, until markAsRead() is called for it -
 * nothing here ever auto-expires or auto-removes an item.
 */
@Injectable({ providedIn: 'root' })
export class PlatformRealtimeService {
  private readonly platformAuthService = inject(PlatformAuthService);
  private readonly platformNotificationService = inject(PlatformNotificationService);

  private connection: signalR.HubConnection | null = null;
  private readonly newTenantSignupSubject = new Subject<NewTenantSignupEvent>();

  readonly newTenantSignup$ = this.newTenantSignupSubject.asObservable();

  private readonly notificationsSignal = signal<PlatformNotificationItem[]>([]);
  /** Most-recent-first, capped at whatever GetRecentAsync's default cap is (see the backend's own doc
   * comment) - this is a bell dropdown, not a full history browser. */
  readonly notifications = this.notificationsSignal.asReadonly();

  // Tracked separately from notifications' own length so the badge stays correct even if there are more
  // unread items than the capped list above actually holds.
  private readonly unreadCountSignal = signal(0);
  readonly unreadCount = computed(() => this.unreadCountSignal());

  start(): void {
    if (this.connection) {
      return;
    }

    this.platformNotificationService.getRecent().subscribe((items) => {
      this.notificationsSignal.set(
        items.map((dto) => ({
          id: dto.id,
          tenantId: dto.tenantId ?? 0,
          tenantName: dto.tenantName,
          adminUsername: dto.adminUsername ?? '',
          receivedAt: new Date(dto.createdAt),
          read: dto.isRead
        }))
      );
    });
    this.platformNotificationService.getUnreadCount().subscribe((count) => this.unreadCountSignal.set(count));

    // environment.apiBaseUrl is "/api" in production (same-origin, nginx-proxied - see nginx.conf's
    // /hubs/ location) or an absolute "http://localhost:5299/api" in dev; the hub lives one path
    // segment up from /api on the same host either way.
    const hubUrl = environment.apiBaseUrl.replace(/\/api\/?$/, '') + '/hubs/platform-notifications';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => this.platformAuthService.accessToken ?? '' })
      .withAutomaticReconnect()
      .build();

    this.connection.on(
      'NewTenantSignup',
      (event: { id: number; tenantId: number; tenantName: string; adminUsername: string; createdAt: string }) => {
        this.newTenantSignupSubject.next(event);

        // Guards against ever double-adding the same notification, in the unlikely case a live push
        // arrives before the initial getRecent() fetch above has resolved.
        if (this.notificationsSignal().some((n) => n.id === event.id)) {
          return;
        }

        this.notificationsSignal.update((list) => [
          {
            id: event.id,
            tenantId: event.tenantId,
            tenantName: event.tenantName,
            adminUsername: event.adminUsername,
            receivedAt: new Date(event.createdAt),
            read: false
          },
          ...list
        ]);
        this.unreadCountSignal.update((count) => count + 1);
      }
    );

    // Best-effort - a platform admin who can't establish the live connection still has the persisted
    // history (fetched above) and the email notification, so a connection failure here is silent rather
    // than surfaced as an app error.
    this.connection.start().catch(() => undefined);
  }

  stop(): void {
    this.connection?.stop();
    this.connection = null;
  }

  /** Never deletes the item - see this class' own doc comment on why "unless clicked, don't remove it"
   * means read/unread, not present/absent. */
  markAsRead(id: number): void {
    const wasUnread = this.notificationsSignal().find((n) => n.id === id)?.read === false;

    this.notificationsSignal.update((list) => list.map((n) => (n.id === id ? { ...n, read: true } : n)));
    if (wasUnread) {
      this.unreadCountSignal.update((count) => Math.max(0, count - 1));
    }

    this.platformNotificationService.markAsRead(id).subscribe();
  }

  /** Called on logout so a different platform admin logging in on the same tab doesn't inherit the
   * previous session's notification history. */
  clearNotifications(): void {
    this.notificationsSignal.set([]);
    this.unreadCountSignal.set(0);
  }
}
