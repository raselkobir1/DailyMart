import { Component, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, of } from 'rxjs';
import { debounceTime, switchMap } from 'rxjs/operators';
import { Toast } from '../../../core/toast';
import { CustomerDto } from '../../customers/customer.model';
import { CustomerService } from '../../customers/customer.service';
import { ProductDto } from '../../products/product.model';
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
  imports: [ReactiveFormsModule, FormsModule],
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
  private readonly toast = inject(Toast);

  protected readonly paymentTypes = PAYMENT_TYPES;
  protected readonly customers = signal<CustomerDto[]>([]);
  protected readonly saving = signal(false);
  /** A signal, not a plain field: this app runs zoneless (no zone.js - see package.json), so a plain
   * property bound via ngModel and cleared inside an HttpClient .subscribe() callback never actually
   * reaches the DOM - nothing schedules a check for that async completion. Only signal writes (or a
   * genuine template-bound DOM event) reliably trigger one. Clearing it after addOrIncrementItem() used
   * to appear to work purely by accident, riding on Reactive Forms' own internal change-detection calls. */
  protected readonly barcode = signal('');

  /** Live product search (name/code/barcode) for when the cashier doesn't have a scanner handy -
   * debounced so every keystroke doesn't fire a request, switchMap so only the latest query's results
   * ever land (an in-flight earlier query resolving after a later one can't clobber the dropdown). */
  private readonly productQuery$ = new Subject<string>();
  /** Signal for the same reason as `barcode` above - selectProduct() clears this after a fetch/click, and
   * only a signal write is guaranteed to update the view in this zoneless app. */
  protected readonly productSearchTerm = signal('');
  protected readonly productResults = signal<ProductDto[]>([]);
  protected readonly showProductDropdown = signal(false);

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

  constructor() {
    this.productQuery$
      .pipe(
        debounceTime(250),
        switchMap((term) =>
          term.trim().length > 0
            ? this.productService.getPaged({ pageNumber: 1, pageSize: 10, searchTerm: term.trim() })
            : of(null)
        )
      )
      .subscribe((result) => this.productResults.set(result?.items ?? []));
  }

  ngOnInit(): void {
    this.customerService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe((result) => this.customers.set(result.items));
  }

  protected onBarcodeEnter(): void {
    const code = this.barcode().trim();
    if (!code) {
      return;
    }

    this.productService.getByBarcode(code).subscribe({
      next: (product) => {
        this.addOrIncrementItem(product.id, product.name, product.code, product.sellingPrice);
        this.barcode.set('');
        this.focusBarcodeInput();
      },
      error: () => {
        this.toast.error(`No product found for barcode "${code}".`);
        this.barcode.set('');
        this.focusBarcodeInput();
      }
    });
  }

  protected onProductSearchInput(): void {
    this.showProductDropdown.set(true);
    this.productQuery$.next(this.productSearchTerm());
  }

  /** Deferred so a click on a dropdown item still registers - a plain (blur) would hide the dropdown
   * (removing the button) before the (click) it's currently landing on ever fires. */
  protected hideProductDropdownDelayed(): void {
    setTimeout(() => this.showProductDropdown.set(false), 200);
  }

  protected selectProduct(product: ProductDto): void {
    this.addOrIncrementItem(product.id, product.name, product.code, product.sellingPrice);
    this.productSearchTerm.set('');
    this.productResults.set([]);
    this.showProductDropdown.set(false);
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
      this.toast.error('Scan at least one product before completing the sale.');
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();

    if (raw.paymentType !== 0 && !raw.customerId) {
      this.toast.error('A customer is required for Credit or Partial sales.');
      return;
    }

    if (raw.paymentType === 2) {
      if (raw.paidAmount <= 0) {
        this.toast.error('Paid amount must be greater than 0 for a partial payment.');
        return;
      }
      if (raw.paidAmount >= this.total()) {
        this.toast.error(`Paid amount must be less than the total (${this.total()}). Use Cash if paying in full.`);
        return;
      }
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
        this.toast.success(`Sale ${sale.saleNumber} completed.`);
        this.router.navigateByUrl(`/sales/${sale.id}`);
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? error.error ?? 'Could not complete sale.');
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
