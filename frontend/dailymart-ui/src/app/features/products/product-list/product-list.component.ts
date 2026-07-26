import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { printBarcodeSheet } from '../../../shared/utils/barcode-print';
import { ProductDto, ProductImportResult } from '../product.model';
import { ProductService } from '../product.service';

/** A sheet this large would take noticeably long to render as SVG and print - block it outright rather
 * than silently truncating, so the cashier knows to print in smaller batches instead of assuming the
 * sheet matches their actual stock count. */
const MAX_BARCODE_PRINT_COPIES = 500;

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [FormsModule, PaginationComponent],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {
  @ViewChild('importFileInput') private importFileInputRef?: ElementRef<HTMLInputElement>;

  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<ProductDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly importing = signal(false);
  protected readonly importResult = signal<ProductImportResult | null>(null);
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

  /** One barcode label per physical unit currently on the shelf - a weight-sold product's fractional
   * stock (e.g. 12.5 kg) rounds down to a whole label count, since a label sticks to one discrete item.
   * Stock above the print limit is capped rather than rejected - printing the first 500 labels is still
   * useful, whereas refusing to print anything is not. */
  protected printBarcodes(product: ProductDto): void {
    const stockCopies = Math.floor(product.currentStock);

    if (stockCopies <= 0) {
      this.toast.error(`${product.name} has no stock on hand - nothing to print.`);
      return;
    }

    const copies = Math.min(stockCopies, MAX_BARCODE_PRINT_COPIES);

    if (stockCopies > MAX_BARCODE_PRINT_COPIES) {
      this.toast.success(
        `${product.name} has ${stockCopies} in stock - printing the first ${MAX_BARCODE_PRINT_COPIES} labels.`
      );
    }

    printBarcodeSheet(product.barcode, `${product.name} (${product.code})`, copies);
  }

  protected exportCsv(): void {
    this.productService.exportCsv().subscribe({
      next: (blob) => this.downloadBlob(blob, 'products.csv'),
      error: () => this.toast.error('Could not export products.')
    });
  }

  protected downloadImportTemplate(): void {
    this.productService.downloadImportTemplate().subscribe({
      next: (blob) => this.downloadBlob(blob, 'product-import-template.xlsx'),
      error: () => this.toast.error('Could not download the import template.')
    });
  }

  protected triggerImportFilePicker(): void {
    this.importResult.set(null);
    this.importFileInputRef?.nativeElement.click();
  }

  protected onImportFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';

    if (!file) {
      return;
    }

    this.importing.set(true);
    this.productService.import(file).subscribe({
      next: (result) => {
        this.importing.set(false);
        this.importResult.set(result);
        if (result.errors.length === 0) {
          this.toast.success(`Imported ${result.created} new and ${result.updated} updated product(s).`);
        } else {
          this.toast.error(`Imported with ${result.errors.length} row error(s) - see details below.`);
        }
        this.load();
      },
      error: (error) => {
        this.importing.set(false);
        this.toast.error(error.error?.title ?? error.error ?? 'Could not import the file.');
      }
    });
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
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
