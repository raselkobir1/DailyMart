import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { PlatformAuthService } from '../../../core/auth/platform-auth.service';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { PlatformTenantDto } from './platform-tenant.model';
import { PlatformTenantService } from './platform-tenant.service';

/** The platform-admin panel's company list - list every tenant, suspend/activate one, and see/manage
 * its billing plan at a glance (Plan/Paid Until/Overdue columns - see PlatformTenantService's doc
 * comment on TenantSummaryDto). No create/edit/delete for the tenant itself: tenants are only ever
 * created via self-service registration. */
@Component({
  selector: 'app-platform-tenant-list',
  standalone: true,
  imports: [DatePipe, RouterLink, PaginationComponent],
  templateUrl: './platform-tenant-list.component.html',
  styleUrl: './platform-tenant-list.component.scss'
})
export class PlatformTenantListComponent implements OnInit {
  private readonly platformTenantService = inject(PlatformTenantService);
  private readonly platformAuthService = inject(PlatformAuthService);
  private readonly toast = inject(Toast);
  private readonly router = inject(Router);

  protected readonly items = signal<PlatformTenantDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);

  protected readonly currentAdmin = this.platformAuthService.currentAdmin;

  ngOnInit(): void {
    this.load();
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

  protected logout(): void {
    this.platformAuthService.logout();
    this.router.navigateByUrl('/platform/login');
  }

  /** The more recent of LastLoginAt/LastActivityAt - kept out of the DTO to keep it a simple data
   * carrier; null if the tenant has never logged in AND has no audited activity. */
  protected lastActiveAt(tenant: PlatformTenantDto): string | null {
    if (!tenant.lastLoginAt) return tenant.lastActivityAt;
    if (!tenant.lastActivityAt) return tenant.lastLoginAt;
    return tenant.lastLoginAt > tenant.lastActivityAt ? tenant.lastLoginAt : tenant.lastActivityAt;
  }

  private load(): void {
    this.loading.set(true);

    this.platformTenantService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
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
