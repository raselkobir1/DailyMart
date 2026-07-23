import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Toast } from '../../../../core/toast';
import { PurchaseDto, PurchaseItemDto } from '../../purchase.model';
import { PurchaseService } from '../../purchase.service';
import { PurchaseReturnRequest } from '../purchase-return.model';
import { PurchaseReturnService } from '../purchase-return.service';

/** One row per original purchase item, defaulting to a return quantity of 0 - the backend has no
 * endpoint exposing "quantity already returned" per line, so over-returning is only caught (and
 * reported) when the request is submitted, not pre-validated here. */
@Component({
  selector: 'app-purchase-return-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './purchase-return-form.component.html',
  styleUrl: './purchase-return-form.component.scss'
})
export class PurchaseReturnFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly purchaseService = inject(PurchaseService);
  private readonly purchaseReturnService = inject(PurchaseReturnService);
  private readonly toast = inject(Toast);

  private readonly purchaseId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly purchase = signal<PurchaseDto | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    returnDate: [this.todayIso(), Validators.required],
    notes: ['', Validators.maxLength(500)],
    items: this.fb.array<ReturnType<typeof this.createItemGroup>>([])
  });

  protected get itemsArray() {
    return this.form.controls.items;
  }

  ngOnInit(): void {
    this.purchaseService.getById(this.purchaseId).subscribe({
      next: (purchase) => {
        this.purchase.set(purchase);
        purchase.items.forEach((item) => this.itemsArray.push(this.createItemGroup(item)));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load purchase.');
      }
    });
  }

  protected back(): void {
    this.router.navigateByUrl(`/purchases/${this.purchaseId}/returns`);
  }

  protected save(): void {
    const items = this.itemsArray
      .getRawValue()
      .filter((item) => item.quantity > 0)
      .map((item) => ({ purchaseItemId: item.purchaseItemId, quantity: item.quantity }));

    if (items.length === 0) {
      this.toast.error('Enter a return quantity for at least one item.');
      return;
    }

    this.saving.set(true);
    const raw = this.form.getRawValue();
    const request: PurchaseReturnRequest = {
      returnDate: `${raw.returnDate}T00:00:00.000Z`,
      notes: raw.notes || null,
      items
    };

    this.purchaseReturnService.create(this.purchaseId, request).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('Return recorded.');
        this.router.navigateByUrl(`/purchases/${this.purchaseId}/returns`);
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not record return.');
      }
    });
  }

  private createItemGroup(item: PurchaseItemDto) {
    return this.fb.nonNullable.group({
      purchaseItemId: [item.id],
      quantity: [0, Validators.min(0)]
    });
  }

  private todayIso(): string {
    return new Date().toISOString().substring(0, 10);
  }
}
