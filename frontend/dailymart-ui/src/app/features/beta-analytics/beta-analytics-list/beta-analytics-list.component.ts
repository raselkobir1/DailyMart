import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { BetaAnalyticsSnapshotDto } from '../beta-analytics.model';
import { BetaAnalyticsService } from '../beta-analytics.service';

/** Demo page for the per-tenant feature entitlement mechanism (CLAUDE.md §4) - this route/menu is only
 * reachable at all if the platform admin has granted this tenant the "beta-analytics" feature; see
 * BetaAnalyticsController's doc comment on the backend. Not a real feature. */
@Component({
  selector: 'app-beta-analytics-list',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './beta-analytics-list.component.html',
  styleUrl: './beta-analytics-list.component.scss'
})
export class BetaAnalyticsListComponent implements OnInit {
  private readonly betaAnalyticsService = inject(BetaAnalyticsService);

  protected readonly snapshot = signal<BetaAnalyticsSnapshotDto | null>(null);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.betaAnalyticsService.getSnapshot().subscribe({
      next: (snapshot) => this.snapshot.set(snapshot),
      error: () => this.error.set('Could not load your Beta Analytics snapshot.')
    });
  }
}
