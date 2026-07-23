import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { downloadCsv } from '../../../shared/utils/csv-export';
import { fetchAllPages } from '../../../shared/utils/fetch-all-pages';
import { CustomerDto } from '../customer.model';
import { CustomerService } from '../customer.service';

@Component({
  selector: 'app-customer-due-report',
  standalone: true,
  imports: [ReactiveFormsModule, PaginationComponent],
  templateUrl: './customer-due-report.component.html',
  styleUrl: './customer-due-report.component.scss'
})
export class CustomerDueReportComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly customerService = inject(CustomerService);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<CustomerDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly collectingForId = signal<number | null>(null);
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

  protected startCollectPayment(customer: CustomerDto): void {
    this.collectingForId.set(customer.id);
    this.paymentForm.reset({ amount: 0, notes: '' });
  }

  protected cancelCollectPayment(): void {
    this.collectingForId.set(null);
  }

  protected collectPayment(customer: CustomerDto): void {
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    const raw = this.paymentForm.getRawValue();
    this.saving.set(true);

    this.customerService.collectPayment(customer.id, { amount: raw.amount, notes: raw.notes || null }).subscribe({
      next: () => {
        this.saving.set(false);
        this.collectingForId.set(null);
        this.toast.success('Payment collected.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not collect payment.');
      }
    });
  }

  protected viewLedger(customer: CustomerDto): void {
    this.router.navigateByUrl(`/customers/${customer.id}/ledger`);
  }

  protected print(): void {
    window.print();
  }

  protected exportCsv(): void {
    fetchAllPages((pageNumber) => this.customerService.getDueReport({ pageNumber, pageSize: 100 })).subscribe({
      next: (items) => {
        downloadCsv(
          `customer-due-${new Date().toISOString().substring(0, 10)}.csv`,
          ['Name', 'Phone', 'Current Due'],
          items.map((c) => [c.name, c.phone, c.currentDue])
        );
      },
      error: () => this.toast.error('Could not export the due report.')
    });
  }

  private load(): void {
    this.loading.set(true);

    this.customerService.getDueReport({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
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
