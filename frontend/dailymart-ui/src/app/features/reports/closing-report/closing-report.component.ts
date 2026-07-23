import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Toast } from '../../../core/toast';
import { downloadCsv } from '../../../shared/utils/csv-export';
import { ClosingReport } from './closing-report.model';
import { ClosingReportPeriod, ClosingReportService } from './closing-report.service';

@Component({
  selector: 'app-closing-report',
  standalone: true,
  imports: [FormsModule, DecimalPipe, DatePipe],
  templateUrl: './closing-report.component.html',
  styleUrl: './closing-report.component.scss'
})
export class ClosingReportComponent implements OnInit {
  private readonly closingReportService = inject(ClosingReportService);
  private readonly toast = inject(Toast);

  protected readonly report = signal<ClosingReport | null>(null);
  protected readonly loading = signal(false);
  protected readonly period = signal<ClosingReportPeriod>('Day');

  protected date = this.todayIso();

  ngOnInit(): void {
    this.load();
  }

  protected selectPeriod(period: ClosingReportPeriod): void {
    this.period.set(period);
    this.load();
  }

  protected onDateChange(): void {
    this.load();
  }

  protected print(): void {
    window.print();
  }

  protected exportCsv(): void {
    const r = this.report();
    if (!r) {
      return;
    }

    downloadCsv(`closing-report-${r.periodType.toLowerCase()}-${this.date}.csv`, ['Metric', 'Value'], [
      ['Period', r.periodType],
      ['From', r.fromDate],
      ['To', r.toDate],
      ['Revenue', r.revenue],
      ['Sales Count', r.salesCount],
      ['COGS', r.cogs],
      ['Gross Profit', r.grossProfit],
      ['Total Purchases', r.totalPurchases],
      ['Purchases Count', r.purchasesCount],
      ['Total Expenses', r.totalExpenses],
      ['Net Profit', r.netProfit],
      ['Cash In', r.cashIn],
      ['Cash Out', r.cashOut],
      ['Net Cash Flow', r.netCashFlow]
    ]);
  }

  private load(): void {
    this.loading.set(true);

    this.closingReportService.getReport(this.period(), this.date).subscribe({
      next: (report) => {
        this.report.set(report);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.toast.error(error.error?.title ?? 'Could not load the closing report.');
      }
    });
  }

  private todayIso(): string {
    return new Date().toISOString().substring(0, 10);
  }
}
