import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ClosingReport } from './closing-report.model';

export type ClosingReportPeriod = 'Day' | 'Month' | 'Year';

@Injectable({ providedIn: 'root' })
export class ClosingReportService {
  private readonly http = inject(HttpClient);

  getReport(period: ClosingReportPeriod, date: string): Observable<ClosingReport> {
    const params = new HttpParams().set('period', period).set('date', date);
    return this.http.get<ClosingReport>('/reports/closing', { params });
  }
}
