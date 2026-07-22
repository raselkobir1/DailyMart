import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { SupplierDto, SupplierLedgerEntryDto } from '../supplier.model';
import { SupplierService } from '../supplier.service';

/** Read-only for now - only ever shows an OpeningBalance entry until Purchase (Module 7) and Supplier
 * Due's payment side (Module 11) start adding rows here too. */
@Component({
  selector: 'app-supplier-ledger',
  standalone: true,
  imports: [MatTableModule, MatPaginatorModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, DatePipe],
  templateUrl: './supplier-ledger.component.html',
  styleUrl: './supplier-ledger.component.scss'
})
export class SupplierLedgerComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly supplierService = inject(SupplierService);
  private readonly snackBar = inject(MatSnackBar);

  private readonly supplierId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly displayedColumns = ['transactionDate', 'entryType', 'description', 'amount', 'balanceAfter'];
  protected readonly supplier = signal<SupplierDto | null>(null);
  protected readonly items = signal<SupplierLedgerEntryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.supplierService.getById(this.supplierId).subscribe({
      next: (supplier) => this.supplier.set(supplier),
      error: () => this.snackBar.open('Could not load supplier.', 'Dismiss')
    });

    this.load();
  }

  protected onPageChange(event: PageEvent): void {
    this.pageNumber.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
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
        this.snackBar.open('Could not load the ledger.', 'Dismiss');
      }
    });
  }
}
