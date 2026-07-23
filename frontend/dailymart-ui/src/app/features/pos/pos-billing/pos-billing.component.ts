import { Component, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CustomerDto } from '../../customers/customer.model';
import { CustomerService } from '../../customers/customer.service';
import { ProductService } from '../../products/product.service';
import { PAYMENT_TYPES, SaleItemRequest, SaleRequest } from '../sale.model';
import { SaleService } from '../sale.service';

/**
 * The POS billing screen - barcode scan-to-add workflow rather than Purchase's dropdown-per-line entry
 * (Module 9's whole point is fast per-scan lookup, not picking from a preloaded list). A USB/Bluetooth
 * barcode scanner behaves like a keyboard that types the code then presses Enter, so a plain text input
 * with (keyup.enter) is all the "scanner integration" needs - no special hardware API involved.
 */
@Component({
  selector: 'app-pos-billing',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './pos-billing.component.html',
  styleUrl: './pos-billing.component.scss'
})
export class PosBillingComponent implements OnInit {
  @ViewChild('barcodeInput') private barcodeInputRef?: ElementRef<HTMLInputElement>;

  private readonly fb = inject(FormBuilder);
  private readonly saleService = inject(SaleService);
  private readonly customerService = inject(CustomerService);
  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly paymentTypes = PAYMENT_TYPES;
  protected readonly customers = signal<CustomerDto[]>([]);
  protected readonly saving = signal(false);
  protected barcode = '';

  protected readonly form = this.fb.nonNullable.group({
    customerId: [0],
    paymentType: [0, Validators.required],
    discountAmount: [0, [Validators.min(0)]],
    vatAmount: [0, [Validators.min(0)]],
    paidAmount: [0, [Validators.min(0)]],
    notes: ['', Validators.maxLength(500)],
    items: this.fb.array<ReturnType<typeof this.createItemGroup>>([])
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

  /** True when the current payment type needs a customer on file - Cash (0) never does. */
  protected readonly customerRequired = computed(() => (this.formValue().paymentType ?? 0) !== 0);

  ngOnInit(): void {
    this.customerService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe((result) => this.customers.set(result.items));
  }

  protected onBarcodeEnter(): void {
    const code = this.barcode.trim();
    if (!code) {
      return;
    }

    this.productService.getByBarcode(code).subscribe({
      next: (product) => {
        this.addOrIncrementItem(product.id, product.name, product.code, product.sellingPrice);
        this.barcode = '';
        this.focusBarcodeInput();
      },
      error: () => {
        this.snackBar.open(`No product found for barcode "${code}".`, 'Dismiss', { duration: 3000 });
        this.barcode = '';
        this.focusBarcodeInput();
      }
    });
  }

  protected lineTotal(index: number): number {
    const item = this.formValue().items?.[index];
    if (!item) {
      return 0;
    }
    return (item.quantity ?? 0) * (item.unitPrice ?? 0) - (item.discountAmount ?? 0);
  }

  protected removeItem(index: number): void {
    this.itemsArray.removeAt(index);
  }

  protected save(): void {
    if (this.itemsArray.length === 0) {
      this.snackBar.open('Scan at least one product before completing the sale.', 'Dismiss');
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();

    if (raw.paymentType !== 0 && !raw.customerId) {
      this.snackBar.open('A customer is required for Credit or Partial sales.', 'Dismiss');
      return;
    }

    this.saving.set(true);
    const items: SaleItemRequest[] = raw.items.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
      unitPrice: item.unitPrice,
      discountAmount: item.discountAmount
    }));

    const request: SaleRequest = {
      customerId: raw.customerId || null,
      saleDate: new Date().toISOString(),
      paymentType: raw.paymentType,
      discountAmount: raw.discountAmount,
      vatAmount: raw.vatAmount,
      paidAmount: raw.paidAmount,
      notes: raw.notes || null,
      items
    };

    this.saleService.create(request).subscribe({
      next: (sale) => {
        this.saving.set(false);
        this.snackBar.open(`Sale ${sale.saleNumber} completed.`, 'Dismiss', { duration: 3000 });
        this.router.navigateByUrl(`/sales/${sale.id}`);
      },
      error: (error) => {
        this.saving.set(false);
        this.snackBar.open(error.error?.title ?? error.error ?? 'Could not complete sale.', 'Dismiss');
      }
    });
  }

  private addOrIncrementItem(productId: number, productName: string, productCode: string, sellingPrice: number): void {
    const existingIndex = this.itemsArray.controls.findIndex((row) => row.controls.productId.value === productId);
    if (existingIndex >= 0) {
      const row = this.itemsArray.at(existingIndex);
      row.controls.quantity.setValue(row.controls.quantity.value + 1);
      return;
    }

    this.itemsArray.push(this.createItemGroup(productId, productName, productCode, sellingPrice));
  }

  private createItemGroup(productId = 0, productName = '', productCode = '', unitPrice = 0) {
    return this.fb.nonNullable.group({
      productId: [productId, Validators.required],
      productName: [productName],
      productCode: [productCode],
      quantity: [1, [Validators.required, Validators.min(0.001)]],
      unitPrice: [unitPrice, [Validators.required, Validators.min(0)]],
      discountAmount: [0, [Validators.min(0)]]
    });
  }

  private focusBarcodeInput(): void {
    setTimeout(() => this.barcodeInputRef?.nativeElement.focus());
  }
}
