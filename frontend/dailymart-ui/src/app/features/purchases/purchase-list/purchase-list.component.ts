import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { PurchaseDto } from '../purchase.model';
import { PurchaseService } from '../purchase.service';

@Component({
  selector: 'app-purchase-list',
  standalone: true,
  imports: [MatTableModule, MatPaginatorModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, DatePipe],
  templateUrl: './purchase-list.component.html',
  styleUrl: './purchase-list.component.scss'
})
export class PurchaseListComponent implements OnInit {
  private readonly purchaseService = inject(PurchaseService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly displayedColumns = [
    'purchaseNumber',
    'supplierName',
    'purchaseDate',
    'paymentType',
    'totalAmount',
    'dueAmount',
    'actions'
  ];
  protected readonly items = signal<PurchaseDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);

  ngOnInit(): void {
    this.load();
  }

  protected onPageChange(event: PageEvent): void {
    this.pageNumber.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
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
        this.snackBar.open('Purchase deleted.', 'Dismiss', { duration: 3000 });
        this.load();
      },
      error: () => this.snackBar.open('Could not delete purchase.', 'Dismiss')
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
        this.snackBar.open('Could not load purchases.', 'Dismiss');
      }
    });
  }
}
