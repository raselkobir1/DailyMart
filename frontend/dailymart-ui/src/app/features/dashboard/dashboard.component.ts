import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DashboardSummary } from './dashboard.model';
import { DashboardService } from './dashboard.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DecimalPipe, DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly router = inject(Router);

  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly trendMax = computed(() => {
    const points = this.summary()?.salesTrend ?? [];
    const values = points.flatMap((p) => [p.sales, p.purchases]);
    return Math.max(1, ...values);
  });

  ngOnInit(): void {
    this.load();
  }

  protected barHeight(value: number): number {
    return Math.round((value / this.trendMax()) * 100);
  }

  protected goTo(route: string): void {
    this.router.navigateByUrl(route);
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.dashboardService.getSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load dashboard data. Is the API/database running?');
        this.loading.set(false);
      }
    });
  }
}
