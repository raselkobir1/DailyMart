import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { SupplierDto, SupplierLedgerEntryDto } from '../supplier.model';
import { SupplierService } from '../supplier.service';

/** Read-only for now - only ever shows an OpeningBalance entry until Purchase (Module 7) and Supplier
 * Due's payment side (Module 11) start adding rows here too. */
@Component({
  selector: 'app-supplier-ledger',
  standalone: true,
  imports: [DatePipe, PaginationComponent],
  templateUrl: './supplier-ledger.component.html',
  styleUrl: './supplier-ledger.component.scss'
})
export class SupplierLedgerComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly supplierService = inject(SupplierService);
  private readonly toast = inject(Toast);

  private readonly supplierId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly supplier = signal<SupplierDto | null>(null);
  protected readonly items = signal<SupplierLedgerEntryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.supplierService.getById(this.supplierId).subscribe({
      next: (supplier) => this.supplier.set(supplier),
      error: () => this.toast.error('Could not load supplier.')
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
