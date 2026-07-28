import { Routes } from '@angular/router';
import { authGuard, canView, guestGuard } from './core/auth/auth.guard';
import { platformAuthGuard } from './core/auth/platform-auth.guard';

export const routes: Routes = [
  // The public marketing/landing page - listed first and matched with pathMatch: 'full', so it only ever
  // claims the bare '/' URL, never '/dashboard' etc. (those fall through to the authGuard-wrapped block
  // below). guestGuard bounces an already-authenticated visitor straight to /dashboard instead of
  // showing this - see LandingComponent's own doc comment.
  {
    path: '',
    pathMatch: 'full',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/landing/landing.component').then((m) => m.LandingComponent)
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register.component').then((m) => m.RegisterComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      // No pathMatch: 'full' redirect for '' here - the top-level landing route above already claims the
      // bare '/' URL for every visitor (guest or authenticated, via guestGuard's redirect), so this
      // child would never be reached.
      {
        path: 'dashboard',
        canActivate: [canView('dashboard')],
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'beta-analytics',
        canActivate: [canView('beta-analytics')],
        loadComponent: () =>
          import('./features/beta-analytics/beta-analytics-list/beta-analytics-list.component').then(
            (m) => m.BetaAnalyticsListComponent
          )
      },
      {
        path: 'audit-log',
        canActivate: [canView('audit-log')],
        loadComponent: () =>
          import('./features/audit-log/audit-log-list/audit-log-list.component').then(
            (m) => m.AuditLogListComponent
          )
      },
      {
        path: 'settings',
        canActivate: [canView('settings')],
        loadComponent: () =>
          import('./features/settings/settings-form/settings-form.component').then(
            (m) => m.SettingsFormComponent
          )
      },
      {
        path: 'categories',
        canActivate: [canView('categories')],
        loadComponent: () =>
          import('./features/master-data/category/category-list/category-list.component').then(
            (m) => m.CategoryListComponent
          )
      },
      {
        path: 'brands',
        canActivate: [canView('brands')],
        loadComponent: () =>
          import('./features/master-data/brand/brand-list/brand-list.component').then(
            (m) => m.BrandListComponent
          )
      },
      {
        path: 'units',
        canActivate: [canView('units')],
        loadComponent: () =>
          import('./features/master-data/unit/unit-list/unit-list.component').then(
            (m) => m.UnitListComponent
          )
      },
      {
        path: 'products',
        canActivate: [canView('products')],
        children: [
          {
            path: '',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/products/product-list/product-list.component').then(
                (m) => m.ProductListComponent
              )
          },
          {
            path: 'new',
            loadComponent: () =>
              import('./features/products/product-form/product-form.component').then(
                (m) => m.ProductFormComponent
              )
          },
          {
            path: ':id/edit',
            loadComponent: () =>
              import('./features/products/product-form/product-form.component').then(
                (m) => m.ProductFormComponent
              )
          }
        ]
      },
      {
        path: 'suppliers',
        canActivate: [canView('suppliers')],
        children: [
          {
            path: '',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/suppliers/supplier-list/supplier-list.component').then(
                (m) => m.SupplierListComponent
              )
          },
          {
            path: 'due-report',
            loadComponent: () =>
              import('./features/suppliers/supplier-due-report/supplier-due-report.component').then(
                (m) => m.SupplierDueReportComponent
              )
          },
          {
            path: ':id/ledger',
            loadComponent: () =>
              import('./features/suppliers/supplier-ledger/supplier-ledger.component').then(
                (m) => m.SupplierLedgerComponent
              )
          }
        ]
      },
      {
        path: 'customers',
        canActivate: [canView('customers')],
        children: [
          {
            path: '',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/customers/customer-list/customer-list.component').then(
                (m) => m.CustomerListComponent
              )
          },
          {
            path: 'due-report',
            loadComponent: () =>
              import('./features/customers/customer-due-report/customer-due-report.component').then(
                (m) => m.CustomerDueReportComponent
              )
          },
          {
            path: ':id/ledger',
            loadComponent: () =>
              import('./features/customers/customer-ledger/customer-ledger.component').then(
                (m) => m.CustomerLedgerComponent
              )
          }
        ]
      },
      {
        path: 'purchases',
        canActivate: [canView('purchases')],
        children: [
          {
            path: '',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/purchases/purchase-list/purchase-list.component').then(
                (m) => m.PurchaseListComponent
              )
          },
          {
            path: 'new',
            loadComponent: () =>
              import('./features/purchases/purchase-form/purchase-form.component').then(
                (m) => m.PurchaseFormComponent
              )
          },
          {
            path: ':id/edit',
            loadComponent: () =>
              import('./features/purchases/purchase-form/purchase-form.component').then(
                (m) => m.PurchaseFormComponent
              )
          },
          {
            path: ':id/returns',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/purchases/returns/purchase-return-list/purchase-return-list.component').then(
                (m) => m.PurchaseReturnListComponent
              )
          },
          {
            path: ':id/returns/new',
            loadComponent: () =>
              import('./features/purchases/returns/purchase-return-form/purchase-return-form.component').then(
                (m) => m.PurchaseReturnFormComponent
              )
          }
        ]
      },
      {
        path: 'pos',
        canActivate: [canView('pos')],
        loadComponent: () =>
          import('./features/pos/pos-billing/pos-billing.component').then((m) => m.PosBillingComponent)
      },
      {
        path: 'sales',
        canActivate: [canView('sales')],
        children: [
          {
            path: '',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/pos/sale-list/sale-list.component').then((m) => m.SaleListComponent)
          },
          {
            path: ':id',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/pos/sale-detail/sale-detail.component').then((m) => m.SaleDetailComponent)
          },
          {
            path: ':id/returns',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/pos/returns/sale-return-list/sale-return-list.component').then(
                (m) => m.SaleReturnListComponent
              )
          },
          {
            path: ':id/returns/new',
            loadComponent: () =>
              import('./features/pos/returns/sale-return-form/sale-return-form.component').then(
                (m) => m.SaleReturnFormComponent
              )
          }
        ]
      },
      {
        path: 'expenses',
        canActivate: [canView('expenses')],
        loadComponent: () =>
          import('./features/expenses/expense-list/expense-list.component').then((m) => m.ExpenseListComponent)
      },
      {
        path: 'profit-loss',
        canActivate: [canView('profit-loss')],
        loadComponent: () =>
          import('./features/profit-loss/profit-loss.component').then((m) => m.ProfitLossComponent)
      },
      {
        path: 'reports',
        canActivate: [canView('reports')],
        children: [
          {
            path: '',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/reports/reports-hub/reports-hub.component').then((m) => m.ReportsHubComponent)
          },
          {
            path: 'closing',
            loadComponent: () =>
              import('./features/reports/closing-report/closing-report.component').then(
                (m) => m.ClosingReportComponent
              )
          }
        ]
      },
      {
        path: 'inventory',
        canActivate: [canView('inventory')],
        children: [
          {
            path: '',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/inventory/inventory-list/inventory-list.component').then(
                (m) => m.InventoryListComponent
              )
          },
          {
            path: 'low-stock',
            loadComponent: () =>
              import('./features/inventory/low-stock/low-stock-list.component').then(
                (m) => m.LowStockListComponent
              )
          },
          {
            path: 'history/:productId',
            loadComponent: () =>
              import('./features/inventory/inventory-history/inventory-history.component').then(
                (m) => m.InventoryHistoryComponent
              )
          }
        ]
      },
      {
        path: 'users',
        canActivate: [canView('users')],
        loadComponent: () => import('./features/users/user-list/user-list.component').then((m) => m.UserListComponent)
      },
      {
        path: 'roles',
        canActivate: [canView('roles')],
        loadComponent: () => import('./features/roles/role-list/role-list.component').then((m) => m.RoleListComponent)
      },
      {
        path: 'menus',
        canActivate: [canView('menus')],
        loadComponent: () => import('./features/menus/menu-list/menu-list.component').then((m) => m.MenuListComponent)
      },
      {
        path: 'permissions',
        canActivate: [canView('permissions')],
        loadComponent: () =>
          import('./features/permissions/permissions.component').then((m) => m.PermissionsComponent)
      }
    ]
  },
  // A separate top-level branch, not nested under the tenant-scoped '' block above - a platform admin
  // isn't tenant-scoped RBAC at all (see platformAuthGuard's doc comment), so it gets its own guard and
  // its own shell (PlatformShellComponent - reuses the tenant app's sidebar/topbar CSS classes, but a
  // flat 2-item nav with no RBAC) rather than the main app shell, which only renders when the tenant
  // AuthService reports authenticated - a platform-only session never does.
  {
    path: 'platform/login',
    loadComponent: () =>
      import('./features/platform/login/platform-login.component').then((m) => m.PlatformLoginComponent)
  },
  {
    path: 'platform',
    canActivate: [platformAuthGuard],
    loadComponent: () =>
      import('./features/platform/shell/platform-shell.component').then((m) => m.PlatformShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'tenants' },
      {
        path: 'tenants',
        loadComponent: () =>
          import('./features/platform/tenants/platform-tenant-list.component').then(
            (m) => m.PlatformTenantListComponent
          )
      },
      {
        path: 'tenants/:id',
        loadComponent: () =>
          import('./features/platform/tenants/platform-tenant-detail/platform-tenant-detail.component').then(
            (m) => m.PlatformTenantDetailComponent
          )
      },
      {
        path: 'plans',
        loadComponent: () =>
          import('./features/platform/plans/plan-list/plan-list.component').then((m) => m.PlanListComponent)
      }
    ]
  }
];
