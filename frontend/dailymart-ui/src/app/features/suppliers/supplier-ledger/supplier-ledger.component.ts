import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { SupplierDto, SupplierLedgerEntryDto } from '../supplier.model';
import { SupplierService } from '../supplier.service';

/** Module 11 added the "Pay Supplier" form here - the natural place to record a payment since the
 * ledger is already open and shows CurrentDue right above it. Unlike the customer ledger's "Collect
 * Payment", this button is always shown regardless of CurrentDue - overpaying a supplier (going
 * negative) is a valid advance/credit balance, not blocked the way customer collection is. */
@Component({
  selector: 'app-supplier-ledger',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, PaginationComponent],
  templateUrl: './supplier-ledger.component.html',
  styleUrl: './supplier-ledger.component.scss'
})
export class SupplierLedgerComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly supplierService = inject(SupplierService);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  private readonly supplierId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly supplier = signal<SupplierDto | null>(null);
  protected readonly items = signal<SupplierLedgerEntryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(true);
  protected readonly paymentFormVisible = signal(false);
  protected readonly payingSupplier = signal(false);

  protected readonly paymentForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0.01)]],
    notes: ['']
  });

  ngOnInit(): void {
    this.loadSupplier();
    this.load();
  }

  protected startPaySupplier(): void {
    this.paymentForm.reset({ amount: 0, notes: '' });
    this.paymentFormVisible.set(true);
  }

  protected cancelPaySupplier(): void {
    this.paymentFormVisible.set(false);
  }

  protected paySupplier(): void {
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    const raw = this.paymentForm.getRawValue();
    this.payingSupplier.set(true);

    this.supplierService.paySupplier(this.supplierId, { amount: raw.amount, notes: raw.notes || null }).subscribe({
      next: (supplier) => {
        this.payingSupplier.set(false);
        this.paymentFormVisible.set(false);
        this.supplier.set(supplier);
        this.toast.success('Payment recorded.');
        this.load();
      },
      error: (error) => {
        this.payingSupplier.set(false);
        this.toast.error(error.error?.title ?? 'Could not record payment.');
      }
    });
  }

  private loadSupplier(): void {
    this.supplierService.getById(this.supplierId).subscribe({
      next: (supplier) => this.supplier.set(supplier),
      error: () => this.toast.error('Could not load supplier.')
    });
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

  protected back(): void {
    this.router.navigateByUrl('/suppliers');
  }

  private load(): void {
    this.loading.set(true);

    this.supplierService.getLedger(this.supplierId, { pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load the ledger.');
      }
    });
  }
}
