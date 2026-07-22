import { Component, OnInit, inject, signal } from '@angular/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { ProductDto } from '../../products/product.model';
import { ProductService } from '../../products/product.service';

/** Read-only - reuses ProductService.getLowStock() rather than an inventory-specific model, since this
 * is fundamentally a filtered Product list (see the backend's ownership decision in Module 8 Step 6). */
@Component({
  selector: 'app-low-stock-list',
  standalone: true,
  imports: [MatTableModule, MatPaginatorModule, MatProgressSpinnerModule],
  templateUrl: './low-stock-list.component.html',
  styleUrl: './low-stock-list.component.scss'
})
export class LowStockListComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly displayedColumns = ['code', 'name', 'currentStock', 'minimumStock'];
  protected readonly items = signal<ProductDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);

  ngOnInit(): void {
    this.load();
  }

  protected onPageChange(event: PageEvent): void {
    this.pageNumber.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  private load(): void {
    this.loading.set(true);

    this.productService.getLowStock({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Could not load low stock products.', 'Dismiss');
      }
    });
  }
}
