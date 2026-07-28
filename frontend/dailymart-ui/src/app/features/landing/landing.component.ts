import { LowerCasePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LandingPlanDto } from './landing-plan.model';
import { LandingPlanService } from './landing-plan.service';

interface LandingFeature {
  icon: string;
  title: string;
  description: string;
}

/**
 * The public marketing/landing page - the only page in this app meant for someone who has never signed
 * in, doesn't have an account yet, and isn't a tenant or platform admin. Lives at the root path ('/', see
 * app.routes.ts); guestGuard sends an already-authenticated visitor straight to /dashboard instead of
 * showing this. Pricing is a live call to the one anonymous endpoint in the app
 * (PublicPlansController) so it can never drift out of sync with what a platform admin has actually
 * configured.
 */
@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, LowerCasePipe],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent implements OnInit {
  private readonly landingPlanService = inject(LandingPlanService);

  protected readonly plans = signal<LandingPlanDto[]>([]);
  protected readonly loadingPlans = signal(true);

  protected readonly features: LandingFeature[] = [
    {
      icon: '🖥️',
      title: 'Fast Barcode POS Billing',
      description: 'Scan and bill in seconds - cash, credit, or partial payment, with instant stock deduction.'
    },
    {
      icon: '📦',
      title: 'Inventory & Stock Alerts',
      description: "Every purchase, sale, and adjustment is tracked - stock can never go below zero unnoticed."
    },
    {
      icon: '🚚',
      title: 'Suppliers & Purchases',
      description: 'Purchase entry, returns, and full supplier due tracking, all in one place.'
    },
    {
      icon: '🧑‍🤝‍🧑',
      title: 'Customer Due Tracking',
      description: 'A complete ledger per customer - who owes what, and since when.'
    },
    {
      icon: '🧮',
      title: 'Expenses & Profit/Loss',
      description: 'Rent, salary, utilities - plus a real P&L computed straight from your sales data.'
    },
    {
      icon: '📑',
      title: 'Printable, Exportable Reports',
      description: 'Sales, purchases, inventory, dues, and closing reports whenever you need them.'
    },
    {
      icon: '🔐',
      title: 'Role-Based Access',
      description: 'Give your cashier POS-only access - keep Purchases and Reports for yourself.'
    },
    {
      icon: '🏢',
      title: 'Your Own Isolated Account',
      description: "Every shop gets its own private data - nothing shared with any other business."
    }
  ];

  ngOnInit(): void {
    this.landingPlanService.getActive().subscribe({
      next: (plans) => {
        this.plans.set(plans);
        this.loadingPlans.set(false);
      },
      error: () => this.loadingPlans.set(false)
    });
  }
}
