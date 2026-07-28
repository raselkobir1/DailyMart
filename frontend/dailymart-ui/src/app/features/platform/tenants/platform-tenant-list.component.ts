import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { PlatformRealtimeService } from '../../../core/platform-realtime';
import { PlatformSupportChatRealtimeService } from '../../../core/platform-support-chat-realtime';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { PlatformTenantDto } from './platform-tenant.model';
import { PlatformTenantService } from './platform-tenant.service';

type StatusFilter = '' | 'active' | 'suspended';
type BillingFilter = '' | 'overdue' | 'paid' | 'free';

/** The platform-admin panel's company list - list every tenant, suspend/activate one, and see/manage
 * its billing plan at a glance (Plan/Paid Until/Overdue columns - see PlatformTenantService's doc
 * comment on TenantSummaryDto). No create/edit/delete for the tenant itself: tenants are only ever
 * created via self-service registration. Rendered inside PlatformShellComponent's <router-outlet> -
 * navigation/sign-out live in the shell, not here. Sorting/filtering (search, Status, Billing, and
 * clickable column headers) all go through PlatformTenantService.GetPagedAsync, which enriches every
 * tenant before filtering/sorting/paging - see its own doc comment for why that's necessary here. */
@Component({
  selector: 'app-platform-tenant-list',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, PaginationComponent],
  templateUrl: './platform-tenant-list.component.html',
  styleUrl: './platform-tenant-list.component.scss'
})
export class PlatformTenantListComponent implements OnInit, OnDestroy {
  private readonly platformTenantService = inject(PlatformTenantService);
  private readonly platformRealtimeService = inject(PlatformRealtimeService);
  private readonly platformSupportChatRealtimeService = inject(PlatformSupportChatRealtimeService);
  private readonly toast = inject(Toast);
  private newSignupSubscription?: Subscription;
  private supportChatSubscription?: Subscription;

  protected readonly items = signal<PlatformTenantDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);

  protected searchTerm = '';
  protected readonly statusFilter = signal<StatusFilter>('');
  protected readonly billingFilter = signal<BillingFilter>('');

  protected readonly sortBy = signal<string | null>(null);
  protected readonly sortDescending = signal(false);

  ngOnInit(): void {
    this.load();

    // Live push (see PlatformRealtimeService's doc comment) - the toast itself is shown once, shell-wide,
    // by PlatformShellComponent; this only re-loads the list so a new row appears live when this happens
    // to be the page currently open, without a manual refresh.
    this.newSignupSubscription = this.platformRealtimeService.newTenantSignup$.subscribe(() => this.load());

    // Same live-refresh reasoning as above - a new support-chat message anywhere updates this row's
    // unread badge without a manual refresh.
    this.supportChatSubscription = this.platformSupportChatRealtimeService.conversationUpdated$.subscribe(() => this.load());
  }

  ngOnDestroy(): void {
    this.newSignupSubscription?.unsubscribe();
    this.supportChatSubscription?.unsubscribe();
  }

  protected onPageChange(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
    this.load();
  }

  protected onPageSizeChange(pageSize: number): void {
    this.pageSize.set(pageSize);
    this.pageNumber.set(1);
    this.load();
  }

  protected search(): void {
    this.pageNumber.set(1);
    this.load();
  }

  protected onStatusFilterChange(value: StatusFilter): void {
    this.statusFilter.set(value);
    this.pageNumber.set(1);
    this.load();
  }

  protected onBillingFilterChange(value: BillingFilter): void {
    this.billingFilter.set(value);
    this.pageNumber.set(1);
    this.load();
  }

  /** Clicking an already-sorted column flips direction; clicking a different one switches to it,
   * ascending first - the usual spreadsheet-style convention. */
  protected onSort(column: string): void {
    if (this.sortBy() === column) {
      this.sortDescending.set(!this.sortDescending());
    } else {
      this.sortBy.set(column);
      this.sortDescending.set(false);
    }
    this.pageNumber.set(1);
    this.load();
  }

  protected sortIndicator(column: string): string {
    if (this.sortBy() !== column) {
      return '';
    }
    return this.sortDescending() ? ' ▼' : ' ▲';
  }

  protected suspend(tenant: PlatformTenantDto): void {
    if (!confirm(`Suspend "${tenant.name}"? Its users will not be able to log in until reactivated.`)) {
      return;
    }

    this.platformTenantService.suspend(tenant.id).subscribe({
      next: () => {
        this.toast.success(`${tenant.name} suspended.`);
        this.load();
      },
      error: () => this.toast.error('Could not suspend this company.')
    });
  }

  protected activate(tenant: PlatformTenantDto): void {
    this.platformTenantService.activate(tenant.id).subscribe({
      next: () => {
        this.toast.success(`${tenant.name} reactivated.`);
        this.load();
      },
      error: () => this.toast.error('Could not reactivate this company.')
    });
  }

  private load(): void {
    this.loading.set(true);

    const request = {
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      searchTerm: this.searchTerm || undefined,
      sortBy: this.sortBy() ?? undefined,
      sortDescending: this.sortDescending()
    };

    this.platformTenantService
      .getPaged(request, this.statusFilter() || undefined, this.billingFilter() || undefined)
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Could not load companies.');
        }
      });
  }
}
