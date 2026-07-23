import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { AuditLogDto } from '../audit-log.model';
import { AuditLogService } from '../audit-log.service';

/**
 * Thin end-to-end smoke-test page for Module 0: proves the whole stack (Controller -> Service ->
 * Repository -> DbContext) is reachable from a real Angular page. Module 15 (Audit Log UI) replaces
 * this with proper filtering/search UX once the rest of the modules exist to generate audit data.
 */
@Component({
  selector: 'app-audit-log-list',
  standalone: true,
  imports: [DatePipe, PaginationComponent],
  templateUrl: './audit-log-list.component.html',
  styleUrl: './audit-log-list.component.scss'
})
export class AuditLogListComponent implements OnInit {
  private readonly auditLogService = inject(AuditLogService);

  protected readonly items = signal<AuditLogDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
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

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.auditLogService
      .getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() })
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
