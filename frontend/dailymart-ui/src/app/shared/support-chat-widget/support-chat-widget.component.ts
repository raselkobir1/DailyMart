import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { SupportChatRealtimeService } from '../../core/support-chat-realtime';

/**
 * The floating "chat with support" bubble shown on every tenant page (see app.html) - available to any
 * signed-in user of the shop, not Admin-only (CLAUDE.md's Support chat bullet). One conversation per
 * tenant with the platform; there's no per-menu RBAC wiring here deliberately, matching how the toast
 * container is also just always present rather than a routed page.
 */
@Component({
  selector: 'app-support-chat-widget',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './support-chat-widget.component.html',
  styleUrl: './support-chat-widget.component.scss'
})
export class SupportChatWidgetComponent implements OnInit, OnDestroy {
  private readonly supportChatRealtimeService = inject(SupportChatRealtimeService);

  protected readonly open = signal(false);
  protected readonly messages = this.supportChatRealtimeService.messages;
  protected readonly unreadCount = this.supportChatRealtimeService.unreadCount;
  protected draftMessage = '';

  /** Wraps both the bubble button and its panel - see app.ts's accountMenu for the same
   * click-outside-to-close pattern. */
  @ViewChild('chatWidget') private chatWidgetRef?: ElementRef<HTMLElement>;

  ngOnInit(): void {
    this.supportChatRealtimeService.start();
  }

  ngOnDestroy(): void {
    this.supportChatRealtimeService.stop();
  }

  protected toggle(): void {
    this.open.update((isOpen) => !isOpen);
    if (this.open()) {
      this.supportChatRealtimeService.ensureHistoryLoaded();
      this.supportChatRealtimeService.markRead();
    }
  }

  protected send(): void {
    const text = this.draftMessage.trim();
    if (!text) {
      return;
    }
    this.supportChatRealtimeService.sendMessage(text);
    this.draftMessage = '';
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (!this.open()) {
      return;
    }

    const target = event.target as Node | null;
    if (target && !this.chatWidgetRef?.nativeElement.contains(target)) {
      this.open.set(false);
    }
  }
}
