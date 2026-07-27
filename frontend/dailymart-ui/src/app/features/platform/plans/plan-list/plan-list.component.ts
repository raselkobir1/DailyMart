import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PlatformAuthService } from '../../../../core/auth/platform-auth.service';
import { Toast } from '../../../../core/toast';
import { PaginationComponent } from '../../../../shared/pagination/pagination.component';
import { BILLING_CYCLES, PlanDto } from '../plan.model';
import { PlanService } from '../plan.service';

/** The platform-admin Plan catalog (Free/Basic/Pro/...) - a billing label only, see Plan's backend doc
 * comment. No perms gating anywhere in this panel (unlike the tenant-scoped app) - reaching this route
 * at all already requires a PlatformAdmin session (platformAuthGuard). */
@Component({
  selector: 'app-plan-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PaginationComponent],
  templateUrl: './plan-list.component.html',
  styleUrl: './plan-list.component.scss'
})
export class PlanListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly planService = inject(PlanService);
  private readonly platformAuthService = inject(PlatformAuthService);
  private readonly toast = inject(Toast);
  private readonly router = inject(Router);

  protected readonly billingCycles = BILLING_CYCLES;
  protected readonly currentAdmin = this.platformAuthService.currentAdmin;

  protected readonly items = signal<PlanDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly editingId = signal<number | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: [''],
    isFree: [false],
    price: [0, [Validators.min(0)]],
    billingCycle: [0],
    sortOrder: [0, [Validators.min(0)]]
  });

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

  protected startCreate(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', description: '', isFree: false, price: 0, billingCycle: 0, sortOrder: 0 });
    this.formVisible.set(true);
  }

  protected startEdit(plan: PlanDto): void {
    this.editingId.set(plan.id);
    this.form.reset({
      name: plan.name,
      description: plan.description ?? '',
      isFree: plan.isFree,
      price: plan.price,
      billingCycle: this.billingCycles.find((c) => c.label === plan.billingCycle)?.value ?? 0,
      sortOrder: plan.sortOrder
    });
    this.formVisible.set(true);
  }

  protected cancelEdit(): void {
    this.formVisible.set(false);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request = {
      name: raw.name,
      description: raw.description || null,
      price: raw.price,
      billingCycle: raw.billingCycle,
      isFree: raw.isFree,
      sortOrder: raw.sortOrder
    };

    this.saving.set(true);
    const id = this.editingId();
    const result$ = id === null ? this.planService.create(request) : this.planService.update(id, request);

    result$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.toast.success('Plan saved.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save plan.');
      }
    });
  }

  protected deactivate(plan: PlanDto): void {
    if (!confirm(`Retire the "${plan.name}" plan? Existing subscribers keep it - it just won't be assignable to anyone new.`)) {
      return;
    }

    this.planService.deactivate(plan.id).subscribe({
      next: () => {
        this.toast.success(`${plan.name} retired.`);
        this.load();
      },
      error: () => this.toast.error('Could not retire this plan.')
    });
  }

  protected activate(plan: PlanDto): void {
    this.planService.activate(plan.id).subscribe({
      next: () => {
        this.toast.success(`${plan.name} reactivated.`);
        this.load();
      },
      error: () => this.toast.error('Could not reactivate this plan.')
    });
  }

  protected logout(): void {
    this.platformAuthService.logout();
    this.router.navigateByUrl('/platform/login');
  }

  private load(): void {
    this.loading.set(true);

    this.planService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load plans.');
      }
    });
  }
}
