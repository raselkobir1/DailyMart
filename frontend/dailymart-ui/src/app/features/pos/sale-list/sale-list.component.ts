import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { SaleDto } from '../sale.model';
import { SaleService } from '../sale.service';

/** Read-only history - no edit/delete, since Sale is create+read only (ISaleService). */
@Component({
  selector: 'app-sale-list',
  standalone: true,
  imports: [MatTableModule, MatPaginatorModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, DatePipe],
  templateUrl: './sale-list.component.html',
  styleUrl: './sale-list.component.scss'
})
export class SaleListComponent implements OnInit {
  private readonly saleService = inject(SaleService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly displayedColumns = [
    'saleNumber',
    'customerName',
    'saleDate',
    'paymentType',
    'totalAmount',
    'dueAmount',
    'profitAmount',
    'actions'
  ];
  protected readonly items = signal<SaleDto[]>([]);
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

  protected newSale(): void {
    this.router.navigateByUrl('/pos');
  }

  protected view(sale: SaleDto): void {
    this.router.navigateByUrl(`/sales/${sale.id}`);
  }

  protected viewReturns(sale: SaleDto): void {
    this.router.navigateByUrl(`/sales/${sale.id}/returns`);
  }

  private load(): void {
    this.loading.set(true);

    this.saleService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Could not load sales.', 'Dismiss');
      }
    });
  }
}
