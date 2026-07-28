import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { LandingPlanDto } from './landing-plan.model';

/** The one API call the public landing page makes with no login at all - see PublicPlansController's
 * doc comment on the backend for why this specific endpoint is the sole [AllowAnonymous] exception. */
@Injectable({ providedIn: 'root' })
export class LandingPlanService {
  private readonly http = inject(HttpClient);

  getActive(): Observable<LandingPlanDto[]> {
    return this.http.get<LandingPlanDto[]>('/public/plans');
  }
}
