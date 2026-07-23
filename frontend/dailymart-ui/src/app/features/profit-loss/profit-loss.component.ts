import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Toast } from '../../core/toast';
import { downloadCsv } from '../../shared/utils/csv-export';
import { ProfitLossSummary } from './profit-loss.model';
import { ProfitLossService } from './profit-loss.service';

type Period = 'today' | 'week' | 'month' | 'year' | 'custom';

/** Satisfies the BRD's "Daily/Weekly/Monthly/Yearly" requirement via period-preset buttons that compute
 * a date range client-side against one summary endpoint, rather than separate per-granularity backend
 * endpoints. Week/Month/Year presets run "to date" (start of period through today), matching how a shop
 * owner would ask "how am I doing this month so far", not a completed prior period. */
@Component({
  selector: 'app-profit-loss',
  standalone: true,
  imports: [FormsModule, DecimalPipe, DatePipe],
  templateUrl: './profit-loss.component.html',
  styleUrl: './profit-loss.component.scss'
})
export class ProfitLossComponent implements OnInit {
  private readonly profitLossService = inject(ProfitLossService);
  private readonly toast = inject(Toast);

  protected readonly summary = signal<ProfitLossSummary | null>(null);
  protected readonly loading = signal(false);
  protected readonly activePeriod = signal<Period>('today');

  protected fromDate = this.todayIso();
  protected toDate = this.todayIso();

  ngOnInit(): void {
    this.selectPeriod('today');
  }

  protected selectPeriod(period: 'today' | 'week' | 'month' | 'year'): void {
    this.activePeriod.set(period);
    const today = this.todayIso();

    if (period === 'today') {
      this.fromDate = today;
    } else if (period === 'week') {
      this.fromDate = this.startOfWeekIso();
    } else if (period === 'month') {
      this.fromDate = this.startOfMonthIso();
    } else {
      this.fromDate = this.startOfYearIso();
    }
    this.toDate = today;

    this.load();
  }

  protected applyCustomRange(): void {
    this.activePeriod.set('custom');
    this.load();
  }

  protected print(): void {
    window.print();
  }

  protected exportCsv(): void {
    const s = this.summary();
    if (!s) {
      return;
    }

    downloadCsv(`profit-loss-${this.fromDate}-to-${this.toDate}.csv`, ['Metric', 'Value'], [
      ['From', s.fromDate],
      ['To', s.toDate],
      ['Revenue', s.revenue],
      ['COGS', s.cogs],
      ['Gross Profit', s.grossProfit],
      ['Operating Expense', s.operatingExpense],
      ['Net Profit', s.netProfit]
    ]);
  }

  private load(): void {
    this.loading.set(true);

    this.profitLossService.getSummary(this.fromDate, this.toDate).subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.toast.error(error.error?.title ?? 'Could not load the profit & loss report.');
      }
    });
  }

  private todayIso(): string {
    return new Date().toISOString().substring(0, 10);
  }

  private startOfWeekIso(): string {
    const now = new Date();
    const day = now.getUTCDay();
    const diff = day === 0 ? 6 : day - 1;
    const monday = new Date(now);
    monday.setUTCDate(now.getUTCDate() - diff);
    return monday.toISOString().substring(0, 10);
  }

  private startOfMonthIso(): string {
    const now = new Date();
    return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1)).toISOString().substring(0, 10);
  }

  private startOfYearIso(): string {
    const now = new Date();
    return new Date(Date.UTC(now.getUTCFullYear(), 0, 1)).toISOString().substring(0, 10);
  }
}
