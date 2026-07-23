import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { CustomerDto, CustomerLedgerEntryDto } from '../customer.model';
import { CustomerService } from '../customer.service';

/** Mirrors SupplierLedgerComponent - only ever shows Sale/SaleReturn entries until Module 10's payment
 * collection also starts adding Payment entries to the same table. */
@Component({
  selector: 'app-customer-ledger',
  standalone: true,
  imports: [MatTableModule, MatPaginatorModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, DatePipe],
  templateUrl: './customer-ledger.component.html',
  styleUrl: './customer-ledger.component.scss'
})
export class CustomerLedgerComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly customerService = inject(CustomerService);
  private readonly snackBar = inject(MatSnackBar);

  private readonly customerId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly displayedColumns = ['transactionDate', 'entryType', 'description', 'amount', 'balanceAfter'];
  protected readonly customer = signal<CustomerDto | null>(null);
  protected readonly items = signal<CustomerLedgerEntryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.customerService.getById(this.customerId).subscribe({
      next: (customer) => this.customer.set(customer),
      error: () => this.snackBar.open('Could not load customer.', 'Dismiss')
    });

    this.load();
  }

  protected onPageChange(event: PageEvent): void {
    this.pageNumber.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
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
        this.snackBar.open('Could not load the ledger.', 'Dismiss');
      }
    });
  }
}
