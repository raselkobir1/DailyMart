import { EMPTY, Observable, expand, reduce } from 'rxjs';
import { PagedResult } from '../models/paged-result.model';

/**
 * "Export all matching rows" needs every page, but every list endpoint caps pageSize at 100
 * (PagedRequestValidator) - a large pageSize like 10000 gets a 400, not a bigger page. This walks
 * page-by-page (via the same request the caller already built, just varying pageNumber) and
 * concatenates the results, so CSV export reflects the whole filtered report, not just page 1.
 */
export function fetchAllPages<T>(fetchPage: (pageNumber: number) => Observable<PagedResult<T>>): Observable<T[]> {
  return fetchPage(1).pipe(
    expand((result) => (result.pageNumber < result.totalPages ? fetchPage(result.pageNumber + 1) : EMPTY)),
    reduce((all: T[], result) => [...all, ...result.items], [])
  );
}
