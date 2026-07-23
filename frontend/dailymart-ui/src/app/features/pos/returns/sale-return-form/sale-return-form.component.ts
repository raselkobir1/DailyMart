import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Toast } from '../../../../core/toast';
import { SaleDto, SaleItemDto } from '../../sale.model';
import { SaleService } from '../../sale.service';
import { SaleReturnRequest } from '../sale-return.model';
import { SaleReturnService } from '../sale-return.service';

/** One row per original sale item, defaulting to a return quantity of 0 - mirrors
 * PurchaseReturnFormComponent: the backend has no endpoint exposing "quantity already returned" per line,
 * so over-returning is only caught (and reported) when the request is submitted. */
@Component({
  selector: 'app-sale-return-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './sale-return-form.component.html',
  styleUrl: './sale-return-form.component.scss'
})
export class SaleReturnFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly saleService = inject(SaleService);
  private readonly saleReturnService = inject(SaleReturnService);
  private readonly toast = inject(Toast);

  private readonly saleId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly sale = signal<SaleDto | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    returnDate: [this.todayIso(), Validators.required],
    notes: ['', Validators.maxLength(500)],
    items: this.fb.array<ReturnType<typeof this.createItemGroup>>([])
  });

  protected get itemsArray() {
    return this.form.controls.items;
  }

  ngOnInit(): void {
    this.saleService.getById(this.saleId).subscribe({
      next: (sale) => {
        this.sale.set(sale);
        sale.items.forEach((item) => this.itemsArray.push(this.createItemGroup(item)));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load sale.');
      }
    });
  }

  protected back(): void {
    this.router.navigateByUrl(`/sales/${this.saleId}/returns`);
  }

  protected save(): void {
    const items = this.itemsArray
      .getRawValue()
      .filter((item) => item.quantity > 0)
      .map((item) => ({ saleItemId: item.saleItemId, quantity: item.quantity }));

    if (items.length === 0) {
      this.toast.error('Enter a return quantity for at least one item.');
      return;
    }

    this.saving.set(true);
    const raw = this.form.getRawValue();
    const request: SaleReturnRequest = {
      returnDate: `${raw.returnDate}T00:00:00.000Z`,
      notes: raw.notes || null,
      items
    };

    this.saleReturnService.create(this.saleId, request).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('Return recorded.');
        this.router.navigateByUrl(`/sales/${this.saleId}/returns`);
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not record return.');
      }
    });
  }

  private createItemGroup(item: SaleItemDto) {
    return this.fb.nonNullable.group({
      saleItemId: [item.id],
      quantity: [0, Validators.min(0)]
    });
  }

  private todayIso(): string {
    return new Date().toISOString().substring(0, 10);
  }
}
