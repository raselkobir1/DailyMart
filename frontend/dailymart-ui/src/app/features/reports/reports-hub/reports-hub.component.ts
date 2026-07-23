import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Perms } from '../../../core/perms';

interface ReportLink {
  key: string;
  icon: string;
  title: string;
  description: string;
  route: string;
}

@Component({
  selector: 'app-reports-hub',
  standalone: true,
  imports: [],
  templateUrl: './reports-hub.component.html',
  styleUrl: './reports-hub.component.scss'
})
export class ReportsHubComponent {
  private readonly router = inject(Router);
  protected readonly perms = inject(Perms);

  protected readonly links: ReportLink[] = [
    { key: 'reports', icon: '📅', title: 'Closing Report', description: 'Daily, monthly, or yearly business summary.', route: '/reports/closing' },
    { key: 'sales', icon: '💰', title: 'Sales', description: 'Every sale, filterable and exportable.', route: '/sales' },
    { key: 'purchases', icon: '🧾', title: 'Purchases', description: 'Every purchase from suppliers.', route: '/purchases' },
    { key: 'inventory', icon: '📦', title: 'Inventory', description: 'Stock movement history.', route: '/inventory' },
    { key: 'customers', icon: '🧑‍🤝‍🧑', title: 'Customer Due', description: 'Outstanding receivables.', route: '/customers/due-report' },
    { key: 'suppliers', icon: '🚚', title: 'Supplier Due', description: 'Outstanding payables.', route: '/suppliers/due-report' },
    { key: 'expenses', icon: '🧮', title: 'Expenses', description: 'Rent, salary, utilities, and other costs.', route: '/expenses' },
    { key: 'profit-loss', icon: '📈', title: 'Profit & Loss', description: 'Revenue, COGS, and net profit for any range.', route: '/profit-loss' }
  ];

  protected visibleLinks(): ReportLink[] {
    return this.links.filter((link) => this.perms.canView(link.key));
  }

  protected open(route: string): void {
    this.router.navigateByUrl(route);
  }
}
