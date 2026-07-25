import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { downloadCsv } from '../../../shared/utils/csv-export';
import { fetchAllPages } from '../../../shared/utils/fetch-all-pages';
import { AUDIT_ACTIONS, AuditLogDto } from '../audit-log.model';
import { AuditLogService } from '../audit-log.service';

/** Module 15's real browsing/filtering UI over the audit trail Module 0's SaveChanges interceptor
 * captures for every module - entity type, action, date range, and a free-text search over who made the
 * change or which record it touched. */
@Component({
  selector: 'app-audit-log-list',
  standalone: true,
  imports: [DatePipe, FormsModule, PaginationComponent],
  templateUrl: './audit-log-list.component.html',
  styleUrl: './audit-log-list.component.scss'
})
export class AuditLogListComponent implements OnInit {
  private readonly auditLogService = inject(AuditLogService);

  protected readonly actions = AUDIT_ACTIONS;
  protected readonly entityNames = signal<string[]>([]);

  protected readonly items = signal<AuditLogDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected searchTerm = '';
  protected filterEntityName = '';
  protected filterAction = '';
  protected filterFromDate = '';
  protected filterToDate = '';

  ngOnInit(): void {
    this.auditLogService.getEntityNames().subscribe((names) => this.entityNames.set(names));
    this.load();
  }

  protected onPageChange(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
    this.load();
  }

  protected onPageSizeChange(pageSize: number): void {
    this.pageSize.set(pageSize);
    this.pageNumber.set(1);
    this.load();
  }

  protected applyFilters(): void {
    this.pageNumber.set(1);
    this.load();
  }

  protected clearFilters(): void {
    this.searchTerm = '';
    this.filterEntityName = '';
    this.filterAction = '';
    this.filterFromDate = '';
    this.filterToDate = '';
    this.pageNumber.set(1);
    this.load();
  }

  protected exportCsv(): void {
    fetchAllPages((pageNumber) =>
      this.auditLogService.getPaged({ pageNumber, pageSize: 100, searchTerm: this.searchTerm || undefined }, this.currentFilter())
    ).subscribe({
      next: (items) => {
        downloadCsv(
          `audit-log-${new Date().toISOString().substring(0, 10)}.csv`,
          ['When', 'Action', 'Entity', 'Entity Id', 'By'],
          items.map((log) => [log.performedAt, log.action, log.entityName, log.entityId, log.performedBy])
        );
      },
      error: () => this.error.set('Could not export the audit log.')
    });
  }

  private currentFilter() {
    return {
      entityName: this.filterEntityName || null,
      action: this.filterAction || null,
      fromDate: this.filterFromDate ? `${this.filterFromDate}T00:00:00.000Z` : null,
      toDate: this.filterToDate ? `${this.filterToDate}T23:59:59.999Z` : null
    };
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.auditLogService
      .getPaged(
        { pageNumber: this.pageNumber(), pageSize: this.pageSize(), searchTerm: this.searchTerm || undefined },
        this.currentFilter()
      )
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Could not load audit logs. Is the API/database running?');
          this.loading.set(false);
        }
      });
  }
}
