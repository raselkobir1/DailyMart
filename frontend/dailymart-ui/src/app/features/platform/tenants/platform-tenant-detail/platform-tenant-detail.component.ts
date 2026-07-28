import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Toast } from '../../../../core/toast';
import { PaginationComponent } from '../../../../shared/pagination/pagination.component';
import { PlanDto } from '../../plans/plan.model';
import { PlanService } from '../../plans/plan.service';
import { SubscriptionPaymentDto, TenantSubscriptionDto } from '../platform-subscription.model';
import { PlatformSubscriptionService } from '../platform-subscription.service';
import { PlatformTenantDto } from '../platform-tenant.model';
import { PlatformTenantService } from '../platform-tenant.service';
import { TenantUsageSnapshotDto } from '../platform-tenant-usage.model';
import { PlatformTenantUsageService } from '../platform-tenant-usage.service';
import { TenantMenuAvailabilityDto } from '../platform-feature.model';
import { PlatformFeatureService } from '../platform-feature.service';

/** Per-tenant billing management - change plan, record a manual payment, see payment history. See
 * ISubscriptionService's doc comment on the backend for why this is manual-only (no gateway).
 * Rendered inside PlatformShellComponent's <router-outlet> - navigation/sign-out live in the shell. */
@Component({
  selector: 'app-platform-tenant-detail',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, RouterLink, PaginationComponent],
  templateUrl: './platform-tenant-detail.component.html',
  styleUrl: './platform-tenant-detail.component.scss'
})
export class PlatformTenantDetailComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly platformTenantService = inject(PlatformTenantService);
  private readonly subscriptionService = inject(PlatformSubscriptionService);
  private readonly usageService = inject(PlatformTenantUsageService);
  private readonly planService = inject(PlanService);
  private readonly featureService = inject(PlatformFeatureService);
  private readonly toast = inject(Toast);

  private readonly tenantId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly tenant = signal<PlatformTenantDto | null>(null);
  protected readonly subscription = signal<TenantSubscriptionDto | null>(null);
  protected readonly usage = signal<TenantUsageSnapshotDto | null>(null);
  protected readonly activePlans = signal<PlanDto[]>([]);
  protected readonly features = signal<TenantMenuAvailabilityDto[]>([]);
  protected readonly featuresLoading = signal(true);
  protected readonly grantingMenuId = signal<number | null>(null);
  /** Only restricted menus ever need a grant/revoke action - generally-available ones need no row here
   * at all beyond the summary count shown alongside the table. */
  protected readonly restrictedFeatures = computed(() => this.features().filter((f) => !f.isGenerallyAvailable));

  protected readonly payments = signal<SubscriptionPaymentDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(true);

  protected readonly changingPlan = signal(false);
  protected readonly paymentFormVisible = signal(false);
  protected readonly recordingPayment = signal(false);
  protected readonly sendingReminder = signal(false);

  protected readonly planForm = this.fb.nonNullable.group({
    planId: [0, Validators.required]
  });

  protected readonly paymentForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0.01)]],
    paidUntil: ['', Validators.required],
    method: ['', Validators.required],
    notes: ['']
  });

  ngOnInit(): void {
    this.loadTenant();
    this.loadSubscription();
    this.loadUsage();
    this.loadPayments();
    this.loadFeatures();

    this.planService.getActive().subscribe({
      next: (plans) => this.activePlans.set(plans),
      error: () => this.toast.error('Could not load plans.')
    });
  }

  protected grantFeature(menuId: number): void {
    this.grantingMenuId.set(menuId);
    this.featureService.grant(this.tenantId, menuId).subscribe({
      next: () => {
        this.grantingMenuId.set(null);
        this.toast.success('Feature granted.');
        this.loadFeatures();
      },
      error: (error) => {
        this.grantingMenuId.set(null);
        this.toast.error(error.error?.title ?? 'Could not grant this feature.');
      }
    });
  }

  protected revokeFeature(menuId: number): void {
    this.grantingMenuId.set(menuId);
    this.featureService.revoke(this.tenantId, menuId).subscribe({
      next: () => {
        this.grantingMenuId.set(null);
        this.toast.success('Feature revoked.');
        this.loadFeatures();
      },
      error: (error) => {
        this.grantingMenuId.set(null);
        this.toast.error(error.error?.title ?? 'Could not revoke this feature.');
      }
    });
  }

  private loadFeatures(): void {
    this.featuresLoading.set(true);
    this.featureService.getFeatures(this.tenantId).subscribe({
      next: (features) => {
        this.features.set(features);
        this.featuresLoading.set(false);
      },
      error: () => {
        this.featuresLoading.set(false);
        this.toast.error('Could not load features.');
      }
    });
  }

  protected startChangePlan(): void {
    const current = this.subscription();
    this.planForm.reset({ planId: current?.planId ?? 0 });
    this.changingPlan.set(true);
  }

  protected cancelChangePlan(): void {
    this.changingPlan.set(false);
  }

  protected changePlan(): void {
    if (this.planForm.invalid) {
      this.planForm.markAllAsTouched();
      return;
    }

    this.subscriptionService.changePlan(this.tenantId, { planId: this.planForm.getRawValue().planId }).subscribe({
      next: (subscription) => {
        this.subscription.set(subscription);
        this.changingPlan.set(false);
        this.toast.success('Plan changed.');
      },
      error: (error) => this.toast.error(error.error?.title ?? 'Could not change plan.')
    });
  }

  protected startRecordPayment(): void {
    this.paymentForm.reset({ amount: 0, paidUntil: '', method: '', notes: '' });
    this.paymentFormVisible.set(true);
  }

  protected cancelRecordPayment(): void {
    this.paymentFormVisible.set(false);
  }

  protected recordPayment(): void {
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    const raw = this.paymentForm.getRawValue();
    this.recordingPayment.set(true);

    this.subscriptionService
      .recordPayment(this.tenantId, {
        amount: raw.amount,
        paidUntil: `${raw.paidUntil}T00:00:00.000Z`,
        method: raw.method,
        notes: raw.notes || null
      })
      .subscribe({
        next: () => {
          this.recordingPayment.set(false);
          this.paymentFormVisible.set(false);
          this.toast.success('Payment recorded.');
          this.loadSubscription();
          this.loadPayments();
        },
        error: (error) => {
          this.recordingPayment.set(false);
          this.toast.error(error.error?.title ?? 'Could not record payment.');
        }
      });
  }

  protected sendReminder(): void {
    this.sendingReminder.set(true);

    this.subscriptionService.sendReminder(this.tenantId).subscribe({
      next: (result) => {
        this.sendingReminder.set(false);
        const reason = result.reminderType === 'Overdue' ? 'overdue payment' : 'Free plan';
        this.toast.success(`Reminder about their ${reason} sent to ${result.sentTo}.`);
      },
      error: (error) => {
        this.sendingReminder.set(false);
        this.toast.error(error.error?.title ?? 'Could not send the reminder.');
      }
    });
  }

  protected onPageChange(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
    this.loadPayments();
  }

  protected onPageSizeChange(pageSize: number): void {
    this.pageSize.set(pageSize);
    this.pageNumber.set(1);
    this.loadPayments();
  }

  private loadTenant(): void {
    this.platformTenantService.getById(this.tenantId).subscribe({
      next: (tenant) => this.tenant.set(tenant),
      error: () => this.toast.error('Could not load this company.')
    });
  }

  private loadSubscription(): void {
    this.subscriptionService.get(this.tenantId).subscribe({
      next: (subscription) => this.subscription.set(subscription),
      error: () => this.toast.error('Could not load the subscription.')
    });
  }

  private loadUsage(): void {
    this.usageService.get(this.tenantId).subscribe({
      next: (usage) => this.usage.set(usage),
      error: () => this.toast.error('Could not load usage.')
    });
  }

  private loadPayments(): void {
    this.loading.set(true);

    this.subscriptionService
      .getPaymentHistory(this.tenantId, { pageNumber: this.pageNumber(), pageSize: this.pageSize() })
      .subscribe({
        next: (result) => {
          this.payments.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Could not load payment history.');
        }
      });
  }
}
