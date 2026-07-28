import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BetaAnalyticsSnapshotDto } from './beta-analytics.model';

@Injectable({ providedIn: 'root' })
export class BetaAnalyticsService {
  private readonly http = inject(HttpClient);

  getSnapshot(): Observable<BetaAnalyticsSnapshotDto> {
    return this.http.get<BetaAnalyticsSnapshotDto>('/beta-analytics');
  }
}
