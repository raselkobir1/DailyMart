import { Location } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

/** Tracks in-app navigation so every "Back"/"Cancel" button in the app returns to whichever page the
 * user actually came from, instead of a hardcoded parent route (e.g. Inventory History's Back should
 * land wherever linked to History, not always /inventory). Counting NavigationEnd events - rather than
 * trusting native browser history directly - lets `back()` fall back to a caller-supplied route when
 * there's nothing to go back to in this session (a direct link, bookmark, or page refresh landed the
 * user straight on the current page), where a raw browser back could otherwise leave the app entirely.
 * Constructed eagerly from App's constructor so it's already observing before the first "Back" click. */
@Injectable({ providedIn: 'root' })
export class NavHistory {
  private readonly location = inject(Location);
  private readonly router = inject(Router);
  private visited = 0;

  constructor() {
    this.router.events.pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd)).subscribe(() => {
      this.visited++;
    });
  }

  back(fallbackUrl: string): void {
    if (this.visited > 1) {
      this.location.back();
    } else {
      this.router.navigateByUrl(fallbackUrl);
    }
  }
}
