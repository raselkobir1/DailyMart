import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { SaleDto } from '../../sale.model';
import { SaleService } from '../../sale.service';
import { SaleReturnDto } from '../sale-return.model';
import { SaleReturnService } from '../sale-return.service';

/** Read-only - no edit/delete, since SaleReturn is create+read only (ISaleReturnService). */
@Component({
  selector: 'app-sale-return-list',
  standalone: true,
  imports: [MatTableModule, MatPaginatorModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, DatePipe],
  templateUrl: './sale-return-list.component.html',
  styleUrl: './sale-return-list.component.scss'
})
export class SaleReturnListComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly saleService = inject(SaleService);
  private readonly saleReturnService = inject(SaleReturnService);
  private readonly snackBar = inject(MatSnackBar);

  private readonly saleId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly displayedColumns = ['returnNumber', 'returnDate', 'totalAmount', 'notes'];
  protected readonly sale = signal<SaleDto | null>(null);
  protected readonly items = signal<SaleReturnDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.saleService.getById(this.saleId).subscribe({
      next: (sale) => this.sale.set(sale),
      error: () => this.snackBar.open('Could not load sale.', 'Dismiss')
    });

    this.load();
  }

  protected onPageChange(event: PageEvent): void {
    this.pageNumber.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  protected back(): void {
    this.router.navigateByUrl('/sales');
  }

  protected createReturn(): void {
    this.router.navigateByUrl(`/sales/${this.saleId}/returns/new`);
  }

  private load(): void {
    this.loading.set(true);

    this.saleReturnService
      .getPaged(this.saleId, { pageNumber: this.pageNumber(), pageSize: this.pageSize() })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.snackBar.open('Could not load returns.', 'Dismiss');
        }
      });
  }
}
