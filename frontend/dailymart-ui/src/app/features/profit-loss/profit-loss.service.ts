import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ProfitLossSummary } from './profit-loss.model';

@Injectable({ providedIn: 'root' })
export class ProfitLossService {
  private readonly http = inject(HttpClient);

  getSummary(fromDate: string, toDate: string): Observable<ProfitLossSummary> {
    const params = new HttpParams()
      .set('fromDate', `${fromDate}T00:00:00.000Z`)
      .set('toDate', `${toDate}T23:59:59.999Z`);

    return this.http.get<ProfitLossSummary>('/profit-loss/summary', { params });
  }
}
