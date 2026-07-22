import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule, MatSelectChange } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { ProductDto } from '../../products/product.model';
import { ProductService } from '../../products/product.service';
import { InventoryTransactionDto } from '../inventory.model';
import { InventoryService } from '../inventory.service';

type ActiveForm = 'adjustment' | 'damaged' | null;

/** Product dropdowns populated via a single pageSize=100 fetch, same pragmatic MVP limit as
 * ProductFormComponent/PurchaseFormComponent's dropdowns. */
@Component({
  selector: 'app-inventory-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    DatePipe
  ],
  templateUrl: './inventory-list.component.html',
  styleUrl: './inventory-list.component.scss'
})
export class InventoryListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly inventoryService = inject(InventoryService);
  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly displayedColumns = [
    'transactionDate',
    'productName',
    'transactionType',
    'quantityChange',
    'balanceAfter',
    'notes'
  ];
  protected readonly products = signal<ProductDto[]>([]);
  protected readonly items = signal<InventoryTransactionDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly activeForm = signal<ActiveForm>(null);
  protected readonly filterProductId = signal<number | null>(null);

  protected readonly adjustmentForm = this.fb.nonNullable.group({
    productId: [0, Validators.required],
    newStockCount: [0, [Validators.required, Validators.min(0)]],
    reason: ['', [Validators.required, Validators.maxLength(500)]]
  });

  protected readonly damagedForm = this.fb.nonNullable.group({
    productId: [0, Validators.required],
    quantity: [0, [Validators.required, Validators.min(0.001)]],
    reason: ['', [Validators.required, Validators.maxLength(500)]]
  });

  ngOnInit(): void {
    this.productService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe((result) => this.products.set(result.items));
    this.load();
  }

  protected onPageChange(event: PageEvent): void {
    this.pageNumber.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  protected onFilterChange(event: MatSelectChange): void {
    this.filterProductId.set(event.value);
    this.pageNumber.set(1);
    this.load();
  }

  protected showAdjustmentForm(): void {
    this.adjustmentForm.reset({ productId: 0, newStockCount: 0, reason: '' });
    this.activeForm.set('adjustment');
  }

  protected showDamagedForm(): void {
    this.damagedForm.reset({ productId: 0, quantity: 0, reason: '' });
    this.activeForm.set('damaged');
  }

  protected cancelForm(): void {
    this.activeForm.set(null);
  }

  protected viewLowStock(): void {
    this.router.navigateByUrl('/inventory/low-stock');
  }

  protected saveAdjustment(): void {
    if (this.adjustmentForm.invalid) {
      this.adjustmentForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.inventoryService.recordAdjustment(this.adjustmentForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.activeForm.set(null);
        this.snackBar.open('Stock adjustment recorded.', 'Dismiss', { duration: 3000 });
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.snackBar.open(error.error?.title ?? 'Could not record adjustment.', 'Dismiss');
      }
    });
  }

  protected saveDamaged(): void {
    if (this.damagedForm.invalid) {
      this.damagedForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.inventoryService.recordDamaged(this.damagedForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.activeForm.set(null);
        this.snackBar.open('Damaged stock recorded.', 'Dismiss', { duration: 3000 });
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.snackBar.open(error.error?.title ?? 'Could not record damaged stock.', 'Dismiss');
      }
    });
  }

  private load(): void {
    this.loading.set(true);

    this.inventoryService
      .getTransactionHistory({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }, this.filterProductId())
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.snackBar.open('Could not load transaction history.', 'Dismiss');
        }
      });
  }
}
