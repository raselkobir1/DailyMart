import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Perms } from '../../../../core/perms';
import { Toast } from '../../../../core/toast';
import { PaginationComponent } from '../../../../shared/pagination/pagination.component';
import { SaleDto } from '../../sale.model';
import { SaleService } from '../../sale.service';
import { SaleReturnDto } from '../sale-return.model';
import { SaleReturnService } from '../sale-return.service';

/** Read-only - no edit/delete, since SaleReturn is create+read only (ISaleReturnService). */
@Component({
  selector: 'app-sale-return-list',
  standalone: true,
  imports: [DatePipe, PaginationComponent],
  templateUrl: './sale-return-list.component.html',
  styleUrl: './sale-return-list.component.scss'
})
export class SaleReturnListComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly saleService = inject(SaleService);
  private readonly saleReturnService = inject(SaleReturnService);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  private readonly saleId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly sale = signal<SaleDto | null>(null);
  protected readonly items = signal<SaleReturnDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.saleService.getById(this.saleId).subscribe({
      next: (sale) => this.sale.set(sale),
      error: () => this.toast.error('Could not load sale.')
    });

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
          this.toast.error('Could not load returns.');
        }
      });
  }
}
