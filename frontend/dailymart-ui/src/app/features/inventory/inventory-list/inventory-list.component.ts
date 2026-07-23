import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
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
  imports: [ReactiveFormsModule, DatePipe, PaginationComponent],
  templateUrl: './inventory-list.component.html',
  styleUrl: './inventory-list.component.scss'
})
export class InventoryListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly inventoryService = inject(InventoryService);
  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

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

  protected onPageChange(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
    this.load();
  }

  protected onPageSizeChange(pageSize: number): void {
    this.pageSize.set(pageSize);
    this.pageNumber.set(1);
    this.load();
  }

  protected onFilterChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.filterProductId.set(value ? Number(value) : null);
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
        this.toast.success('Stock adjustment recorded.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not record adjustment.');
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
        this.toast.success('Damaged stock recorded.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not record damaged stock.');
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
          this.toast.error('Could not load transaction history.');
        }
      });
  }
}
