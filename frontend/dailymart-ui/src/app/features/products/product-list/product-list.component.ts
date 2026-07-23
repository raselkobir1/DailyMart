import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { ProductDto } from '../product.model';
import { ProductService } from '../product.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [FormsModule, PaginationComponent],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<ProductDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected searchTerm = '';

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

  protected search(): void {
    this.pageNumber.set(1);
    this.load();
  }

  protected addProduct(): void {
    this.router.navigateByUrl('/products/new');
  }

  protected editProduct(product: ProductDto): void {
    this.router.navigateByUrl(`/products/${product.id}/edit`);
  }

  protected deleteProduct(product: ProductDto): void {
    if (!confirm(`Delete product "${product.name}"?`)) {
      return;
    }

    this.productService.delete(product.id).subscribe({
      next: () => {
        this.toast.success('Product deleted.');
        this.load();
      },
      error: () => this.toast.error('Could not delete product.')
    });
  }

  protected exportCsv(): void {
    this.productService.exportCsv().subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'products.csv';
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.toast.error('Could not export products.')
    });
  }

  private load(): void {
    this.loading.set(true);

    this.productService
      .getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize(), searchTerm: this.searchTerm || undefined })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Could not load products.');
        }
      });
  }
}
