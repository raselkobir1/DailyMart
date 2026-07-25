import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NavHistory } from '../../../core/nav-history';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { downloadCsv } from '../../../shared/utils/csv-export';
import { fetchAllPages } from '../../../shared/utils/fetch-all-pages';
import { ProductDto } from '../../products/product.model';
import { ProductService } from '../../products/product.service';
import { InventoryTransactionDto } from '../inventory.model';
import { InventoryService } from '../inventory.service';

/** Full stock-movement history for one product - reached from InventoryListComponent's "History" action.
 * Fetches every transaction for the product up front (via fetchAllPages, same approach as CSV export
 * elsewhere) so the summary stats (times sold, total quantity sold, etc.) reflect the whole history, not
 * just whatever page happens to be showing; the table below then paginates that already-fetched array
 * client-side rather than re-querying the server per page. */
@Component({
  selector: 'app-inventory-history',
  standalone: true,
  imports: [DatePipe, PaginationComponent],
  templateUrl: './inventory-history.component.html',
  styleUrl: './inventory-history.component.scss'
})
export class InventoryHistoryComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly productService = inject(ProductService);
  private readonly inventoryService = inject(InventoryService);
  private readonly navHistory = inject(NavHistory);
  private readonly toast = inject(Toast);

  private readonly productId = Number(this.route.snapshot.paramMap.get('productId'));

  protected readonly product = signal<ProductDto | null>(null);
  protected readonly transactions = signal<InventoryTransactionDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);

  protected readonly totalCount = computed(() => this.transactions().length);

  protected readonly pagedTransactions = computed(() => {
    const start = (this.pageNumber() - 1) * this.pageSize();
    return this.transactions().slice(start, start + this.pageSize());
  });

  protected readonly saleCount = computed(() => this.transactions().filter((t) => t.transactionType === 'Sale').length);

  protected readonly totalSoldQty = computed(() =>
    this.transactions()
      .filter((t) => t.transactionType === 'Sale')
      .reduce((sum, t) => sum + Math.abs(t.quantityChange), 0)
  );

  protected readonly totalPurchasedQty = computed(() =>
    this.transactions()
      .filter((t) => t.transactionType === 'Purchase')
      .reduce((sum, t) => sum + t.quantityChange, 0)
  );

  protected readonly totalDamagedQty = computed(() =>
    this.transactions()
      .filter((t) => t.transactionType === 'Damaged')
      .reduce((sum, t) => sum + Math.abs(t.quantityChange), 0)
  );

  ngOnInit(): void {
    if (!Number.isInteger(this.productId) || this.productId <= 0) {
      this.toast.error('Invalid product.');
      this.router.navigateByUrl('/inventory');
      return;
    }

    this.productService.getById(this.productId).subscribe({
      next: (product) => this.product.set(product),
      error: () => this.toast.error('Could not load product.')
    });
    this.load();
  }

  protected onPageChange(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }

  protected onPageSizeChange(pageSize: number): void {
    this.pageSize.set(pageSize);
    this.pageNumber.set(1);
  }

  protected back(): void {
    this.navHistory.back('/inventory');
  }

  protected print(): void {
    window.print();
  }

  protected exportCsv(): void {
    const product = this.product();
    downloadCsv(
      `inventory-history-${product?.code ?? this.productId}.csv`,
      ['Date', 'Type', 'Qty Change', 'Closing Stock', 'Notes'],
      this.transactions().map((e) => [e.transactionDate, e.transactionType, e.quantityChange, e.balanceAfter, e.notes])
    );
  }

  private load(): void {
    this.loading.set(true);

    fetchAllPages((pageNumber) => this.inventoryService.getTransactionHistory({ pageNumber, pageSize: 100 }, this.productId)).subscribe({
      next: (items) => {
        this.transactions.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load transaction history.');
      }
    });
  }
}
