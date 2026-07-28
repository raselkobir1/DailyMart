import { DatePipe } from '@angular/common';
import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Subscription } from 'rxjs';
import { PlatformAuthService } from '../../../core/auth/platform-auth.service';
import { PlatformNotificationItem, PlatformRealtimeService } from '../../../core/platform-realtime';
import { PlatformSupportChatRealtimeService } from '../../../core/platform-support-chat-realtime';
import { Theme } from '../../../core/theme';
import { Toast } from '../../../core/toast';

/**
 * The platform-admin panel's shell - reuses the exact sidebar/topbar/main-column CSS classes the
 * tenant-facing app shell (app.html) already defines in styles.scss, so this panel looks like part of
 * the same product instead of a bare, unstyled afterthought. Deliberately simpler than the tenant
 * shell: a flat 2-item nav (no nested menu groups, no RBAC - platformAuthGuard is "is there a platform-
 * admin session" full stop, see its own doc comment) and no accent-color picker, just the light/dark
 * toggle. Wraps every /platform/* route except /platform/login itself (see app.routes.ts - the login
 * page has nothing to navigate to yet).
 *
 * Also owns the live new-signup toast and the notification bell (PlatformRealtimeService) at the shell
 * level rather than on any one page, so both appear no matter which platform-admin screen is open -
 * PlatformTenantListComponent separately subscribes to the raw event stream just to refresh its own list
 * live when it happens to be the page currently showing.
 */
@Component({
  selector: 'app-platform-shell',
  standalone: true,
  imports: [DatePipe, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './platform-shell.component.html',
  styleUrl: './platform-shell.component.scss'
})
export class PlatformShellComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  protected readonly platformRealtimeService = inject(PlatformRealtimeService);
  private readonly platformSupportChatRealtimeService = inject(PlatformSupportChatRealtimeService);
  private readonly toast = inject(Toast);
  protected readonly platformAuthService = inject(PlatformAuthService);
  protected readonly theme = inject(Theme);

  protected readonly notificationsOpen = signal(false);

  /** Wraps both the bell button and its dropdown (platform-shell.component.html) - see app.ts's
   * accountMenu for the same click-outside-to-close pattern. */
  @ViewChild('notificationMenu') private notificationMenuRef?: ElementRef<HTMLElement>;

  private newSignupSubscription?: Subscription;

  ngOnInit(): void {
    this.platformRealtimeService.start();
    this.newSignupSubscription = this.platformRealtimeService.newTenantSignup$.subscribe((event) => {
      this.toast.success(`New signup: ${event.tenantName}`);
    });
    this.platformSupportChatRealtimeService.start();
  }

  ngOnDestroy(): void {
    this.newSignupSubscription?.unsubscribe();
    this.platformRealtimeService.stop();
    this.platformSupportChatRealtimeService.stop();
  }

  protected initials(): string {
    const name = this.platformAuthService.currentAdmin()?.fullName ?? '';
    return name
      .split(' ')
      .map((part) => part[0])
      .filter(Boolean)
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  protected toggleTheme(): void {
    this.theme.toggleMode();
  }

  protected toggleNotifications(): void {
    this.notificationsOpen.update((open) => !open);
  }

  /** Marking read is what "removes" a notification from the unread count - the item itself stays in the
   * list (see PlatformRealtimeService's doc comment). Also navigates to that company's detail page, since
   * that's the concrete "show details" a new-signup notification can offer. */
  protected openNotification(notification: PlatformNotificationItem): void {
    this.platformRealtimeService.markAsRead(notification.id);
    this.notificationsOpen.set(false);
    this.router.navigate(['/platform/tenants', notification.tenantId]);
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (!this.notificationsOpen()) {
      return;
    }

    const target = event.target as Node | null;
    if (target && !this.notificationMenuRef?.nativeElement.contains(target)) {
      this.notificationsOpen.set(false);
    }
  }

  protected logout(): void {
    this.newSignupSubscription?.unsubscribe();
    this.platformRealtimeService.stop();
    this.platformRealtimeService.clearNotifications();
    this.platformSupportChatRealtimeService.stop();
    this.platformAuthService.logout();
    this.router.navigateByUrl('/platform/login');
  }
}
