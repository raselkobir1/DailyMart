import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { downloadCsv } from '../../../shared/utils/csv-export';
import { fetchAllPages } from '../../../shared/utils/fetch-all-pages';
import { PurchaseDto } from '../purchase.model';
import { PurchaseService } from '../purchase.service';

@Component({
  selector: 'app-purchase-list',
  standalone: true,
  imports: [DatePipe, PaginationComponent],
  templateUrl: './purchase-list.component.html',
  styleUrl: './purchase-list.component.scss'
})
export class PurchaseListComponent implements OnInit {
  private readonly purchaseService = inject(PurchaseService);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<PurchaseDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);

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

  protected create(): void {
    this.router.navigateByUrl('/purchases/new');
  }

  protected edit(purchase: PurchaseDto): void {
    this.router.navigateByUrl(`/purchases/${purchase.id}/edit`);
  }

  protected viewReturns(purchase: PurchaseDto): void {
    this.router.navigateByUrl(`/purchases/${purchase.id}/returns`);
  }

  protected delete(purchase: PurchaseDto): void {
    if (!confirm(`Delete purchase "${purchase.purchaseNumber}"? This reverses its stock and due effects.`)) {
      return;
    }

    this.purchaseService.delete(purchase.id).subscribe({
      next: () => {
        this.toast.success('Purchase deleted.');
        this.load();
      },
      error: () => this.toast.error('Could not delete purchase.')
    });
  }

  protected print(): void {
    window.print();
  }

  protected exportCsv(): void {
    fetchAllPages((pageNumber) => this.purchaseService.getPaged({ pageNumber, pageSize: 100 })).subscribe({
      next: (items) => {
        downloadCsv(
          `purchases-${new Date().toISOString().substring(0, 10)}.csv`,
          ['Purchase #', 'Supplier', 'Date', 'Payment', 'Total', 'Due'],
          items.map((p) => [p.purchaseNumber, p.supplierName, p.purchaseDate, p.paymentType, p.totalAmount, p.dueAmount])
        );
      },
      error: () => this.toast.error('Could not export purchases.')
    });
  }

  private load(): void {
    this.loading.set(true);

    this.purchaseService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load purchases.');
      }
    });
  }
}
