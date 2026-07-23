import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Toast } from '../../../core/toast';
import { ProductDto } from '../../products/product.model';
import { ProductService } from '../../products/product.service';
import { SupplierDto } from '../../suppliers/supplier.model';
import { SupplierService } from '../../suppliers/supplier.service';
import { PAYMENT_TYPES, PAYMENT_TYPE_VALUES, PurchaseDto, PurchaseItemDto, PurchaseRequest } from '../purchase.model';
import { PurchaseService } from '../purchase.service';

/** Supplier/Product dropdowns are populated via a single pageSize=100 fetch, same pragmatic MVP limit
 * as ProductFormComponent's Category/Brand/Unit dropdowns. */
@Component({
  selector: 'app-purchase-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './purchase-form.component.html',
  styleUrl: './purchase-form.component.scss'
})
export class PurchaseFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly purchaseService = inject(PurchaseService);
  private readonly supplierService = inject(SupplierService);
  private readonly productService = inject(ProductService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);

  protected readonly paymentTypes = PAYMENT_TYPES;
  /** Exposed so the template can convert a <select>'s string value back to a number - Angular template
   * expressions only resolve against the component instance, not the global scope. */
  protected readonly Number = Number;

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly purchaseId = signal<number | null>(null);
  protected readonly suppliers = signal<SupplierDto[]>([]);
  protected readonly products = signal<ProductDto[]>([]);

  protected readonly isEditMode = () => this.purchaseId() !== null;

  protected readonly form = this.fb.nonNullable.group({
    supplierId: [0, Validators.required],
    purchaseDate: [this.todayIso(), Validators.required],
    paymentType: [0, Validators.required],
    discountAmount: [0, [Validators.min(0)]],
    vatAmount: [0, [Validators.min(0)]],
    paidAmount: [0, [Validators.min(0)]],
    notes: ['', Validators.maxLength(500)],
    items: this.fb.array([this.createItemGroup()])
  });

  protected get itemsArray() {
    return this.form.controls.items;
  }

  protected readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  protected readonly subtotal = computed(() =>
    (this.formValue().items ?? []).reduce(
      (sum, item) => sum + (item.quantity ?? 0) * (item.unitPrice ?? 0) - (item.discountAmount ?? 0),
      0
    )
  );

  protected readonly total = computed(
    () => this.subtotal() - (this.formValue().discountAmount ?? 0) + (this.formValue().vatAmount ?? 0)
  );

  protected readonly due = computed(() => {
    const paymentType = this.formValue().paymentType ?? 0;
    if (paymentType === 0) {
      return 0;
    }
    if (paymentType === 1) {
      return this.total();
    }
    return this.total() - (this.formValue().paidAmount ?? 0);
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? Number(idParam) : null;
    this.purchaseId.set(id);

    this.loadDropdownData();

    if (id !== null) {
      this.loadPurchase(id);
    } else {
      this.loading.set(false);
    }
  }

  protected lineTotal(index: number): number {
    const item = this.formValue().items?.[index];
    if (!item) {
      return 0;
    }
    return (item.quantity ?? 0) * (item.unitPrice ?? 0) - (item.discountAmount ?? 0);
  }

  protected addItem(): void {
    this.itemsArray.push(this.createItemGroup());
  }

  protected removeItem(index: number): void {
    this.itemsArray.removeAt(index);
  }

  protected onProductChange(index: number, productId: number): void {
    const product = this.products().find((p) => p.id === productId);
    const row = this.itemsArray.at(index);
    if (product && row.controls.unitPrice.value === 0) {
      row.controls.unitPrice.setValue(product.purchasePrice);
    }
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const raw = this.form.getRawValue();
    const request: PurchaseRequest = {
      supplierId: raw.supplierId,
      purchaseDate: `${raw.purchaseDate}T00:00:00.000Z`,
      paymentType: raw.paymentType,
      discountAmount: raw.discountAmount,
      vatAmount: raw.vatAmount,
      paidAmount: raw.paidAmount,
      notes: raw.notes || null,
      items: raw.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        discountAmount: item.discountAmount
      }))
    };

    const id = this.purchaseId();
    const result$ = id === null ? this.purchaseService.create(request) : this.purchaseService.update(id, request);

    result$.subscribe({
      next: (purchase) => {
        this.saving.set(false);
        this.toast.success('Purchase saved.');
        this.router.navigateByUrl(`/purchases/${purchase.id}/edit`);
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save purchase.');
      }
    });
  }

  private createItemGroup(item?: PurchaseItemDto) {
    return this.fb.nonNullable.group({
      productId: [item?.productId ?? 0, Validators.required],
      quantity: [item?.quantity ?? 1, [Validators.required, Validators.min(0.001)]],
      unitPrice: [item?.unitPrice ?? 0, [Validators.required, Validators.min(0)]],
      discountAmount: [item?.discountAmount ?? 0, [Validators.min(0)]]
    });
  }

  private loadDropdownData(): void {
    this.supplierService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe((result) => this.suppliers.set(result.items));
    this.productService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe((result) => this.products.set(result.items));
  }

  private loadPurchase(id: number): void {
    this.purchaseService.getById(id).subscribe({
      next: (purchase) => {
        this.applyPurchase(purchase);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load purchase.');
      }
    });
  }

  private applyPurchase(purchase: PurchaseDto): void {
    this.form.patchValue({
      supplierId: purchase.supplierId,
      purchaseDate: purchase.purchaseDate.substring(0, 10),
      paymentType: PAYMENT_TYPE_VALUES[purchase.paymentType] ?? 0,
      discountAmount: purchase.discountAmount,
      vatAmount: purchase.vatAmount,
      paidAmount: purchase.paidAmount,
      notes: purchase.notes ?? ''
    });

    this.itemsArray.clear();
    purchase.items.forEach((item) => this.itemsArray.push(this.createItemGroup(item)));
  }

  private todayIso(): string {
    return new Date().toISOString().substring(0, 10);
  }
}
