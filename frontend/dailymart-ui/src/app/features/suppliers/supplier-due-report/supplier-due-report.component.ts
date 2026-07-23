import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { SupplierDto } from '../supplier.model';
import { SupplierService } from '../supplier.service';

@Component({
  selector: 'app-supplier-due-report',
  standalone: true,
  imports: [ReactiveFormsModule, PaginationComponent],
  templateUrl: './supplier-due-report.component.html',
  styleUrl: './supplier-due-report.component.scss'
})
export class SupplierDueReportComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly supplierService = inject(SupplierService);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<SupplierDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly payingForId = signal<number | null>(null);
  protected readonly saving = signal(false);

  protected readonly paymentForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0.01)]],
    notes: ['']
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

  protected startPaySupplier(supplier: SupplierDto): void {
    this.payingForId.set(supplier.id);
    this.paymentForm.reset({ amount: 0, notes: '' });
  }

  protected cancelPaySupplier(): void {
    this.payingForId.set(null);
  }

  protected paySupplier(supplier: SupplierDto): void {
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    const raw = this.paymentForm.getRawValue();
    this.saving.set(true);

    this.supplierService.paySupplier(supplier.id, { amount: raw.amount, notes: raw.notes || null }).subscribe({
      next: () => {
        this.saving.set(false);
        this.payingForId.set(null);
        this.toast.success('Payment recorded.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not record payment.');
      }
    });
  }

  protected viewLedger(supplier: SupplierDto): void {
    this.router.navigateByUrl(`/suppliers/${supplier.id}/ledger`);
  }

  private load(): void {
    this.loading.set(true);

    this.supplierService.getDueReport({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load the due report.');
      }
    });
  }
}
