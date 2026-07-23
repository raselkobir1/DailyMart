import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { CustomerDto, CustomerLedgerEntryDto } from '../customer.model';
import { CustomerService } from '../customer.service';

/** Mirrors SupplierLedgerComponent - only ever shows Sale/SaleReturn entries until Module 10's payment
 * collection also starts adding Payment entries to the same table. */
@Component({
  selector: 'app-customer-ledger',
  standalone: true,
  imports: [DatePipe, PaginationComponent],
  templateUrl: './customer-ledger.component.html',
  styleUrl: './customer-ledger.component.scss'
})
export class CustomerLedgerComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly customerService = inject(CustomerService);
  private readonly toast = inject(Toast);

  private readonly customerId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly customer = signal<CustomerDto | null>(null);
  protected readonly items = signal<CustomerLedgerEntryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.customerService.getById(this.customerId).subscribe({
      next: (customer) => this.customer.set(customer),
      error: () => this.toast.error('Could not load customer.')
    });

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
