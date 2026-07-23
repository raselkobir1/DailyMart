import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { CustomerDto, CustomerLedgerEntryDto } from '../customer.model';
import { CustomerService } from '../customer.service';

/** Mirrors SupplierLedgerComponent. Module 10 added the "Collect Payment" form here - it's the natural
 * place to record a payment since the ledger is already open and shows CurrentDue right above it. */
@Component({
  selector: 'app-customer-ledger',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, PaginationComponent],
  templateUrl: './customer-ledger.component.html',
  styleUrl: './customer-ledger.component.scss'
})
export class CustomerLedgerComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly customerService = inject(CustomerService);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  private readonly customerId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly customer = signal<CustomerDto | null>(null);
  protected readonly items = signal<CustomerLedgerEntryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(true);
  protected readonly paymentFormVisible = signal(false);
  protected readonly collectingPayment = signal(false);

  protected readonly paymentForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0.01)]],
    notes: ['']
  });

  ngOnInit(): void {
    this.loadCustomer();
    this.load();
  }

  protected startCollectPayment(): void {
    this.paymentForm.reset({ amount: 0, notes: '' });
    this.paymentFormVisible.set(true);
  }

  protected cancelCollectPayment(): void {
    this.paymentFormVisible.set(false);
  }

  protected collectPayment(): void {
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    const raw = this.paymentForm.getRawValue();
    this.collectingPayment.set(true);

    this.customerService.collectPayment(this.customerId, { amount: raw.amount, notes: raw.notes || null }).subscribe({
      next: (customer) => {
        this.collectingPayment.set(false);
        this.paymentFormVisible.set(false);
        this.customer.set(customer);
        this.toast.success('Payment collected.');
        this.load();
      },
      error: (error) => {
        this.collectingPayment.set(false);
        this.toast.error(error.error?.title ?? 'Could not collect payment.');
      }
    });
  }

  private loadCustomer(): void {
    this.customerService.getById(this.customerId).subscribe({
      next: (customer) => this.customer.set(customer),
      error: () => this.toast.error('Could not load customer.')
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
    this.router.navigateByUrl('/customers');
  }

  private load(): void {
    this.loading.set(true);

    this.customerService.getLedger(this.customerId, { pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
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
