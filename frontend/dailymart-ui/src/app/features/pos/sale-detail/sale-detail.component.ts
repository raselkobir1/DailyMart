import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { SaleDto } from '../sale.model';
import { SaleService } from '../sale.service';

/** The invoice/receipt view - print uses the browser's own print dialog (window.print()) rather than a
 * generated PDF, same "keep it simple" approach as barcode-print.ts's printBarcode helper. */
@Component({
  selector: 'app-sale-detail',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTableModule, DatePipe],
  templateUrl: './sale-detail.component.html',
  styleUrl: './sale-detail.component.scss'
})
export class SaleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly saleService = inject(SaleService);
  private readonly snackBar = inject(MatSnackBar);

  private readonly saleId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly displayedColumns = ['productName', 'quantity', 'unitPrice', 'discountAmount', 'lineTotal'];
  protected readonly sale = signal<SaleDto | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.saleService.getById(this.saleId).subscribe({
      next: (sale) => {
        this.sale.set(sale);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Could not load sale.', 'Dismiss');
      }
    });
  }

  protected print(): void {
    window.print();
  }

  protected viewReturns(): void {
    this.router.navigateByUrl(`/sales/${this.saleId}/returns`);
  }

  protected newSale(): void {
    this.router.navigateByUrl('/pos');
  }

  protected back(): void {
    this.router.navigateByUrl('/sales');
  }
}
