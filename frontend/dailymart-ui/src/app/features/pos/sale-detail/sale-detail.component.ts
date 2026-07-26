import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NavHistory } from '../../../core/nav-history';
import { Toast } from '../../../core/toast';
import { CustomerDto } from '../../customers/customer.model';
import { CustomerService } from '../../customers/customer.service';
import { SaleDto } from '../sale.model';
import { SaleService } from '../sale.service';

/** The invoice/receipt view - print uses the browser's own print dialog (window.print()) rather than a
 * generated PDF, same "keep it simple" approach as barcode-print.ts's printBarcode helper. */
@Component({
  selector: 'app-sale-detail',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './sale-detail.component.html',
  styleUrl: './sale-detail.component.scss'
})
export class SaleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly navHistory = inject(NavHistory);
  private readonly saleService = inject(SaleService);
  private readonly customerService = inject(CustomerService);
  private readonly toast = inject(Toast);

  private readonly saleId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly sale = signal<SaleDto | null>(null);
  protected readonly customer = signal<CustomerDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly sendingEmail = signal(false);
  protected readonly sendingSms = signal(false);

  ngOnInit(): void {
    this.saleService.getById(this.saleId).subscribe({
      next: (sale) => {
        this.sale.set(sale);
        this.loading.set(false);
        if (sale.customerId) {
          this.loadCustomer(sale.customerId);
        }
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load sale.');
      }
    });
  }

  private loadCustomer(customerId: number): void {
    this.customerService.getById(customerId).subscribe({
      next: (customer) => this.customer.set(customer),
      // Non-fatal - the invoice itself already loaded; this only gates the email/SMS reminder buttons.
      error: () => {}
    });
  }

  protected print(): void {
    window.print();
  }

  /** Gates the Email/SMS reminder buttons - this feature exists to chase an outstanding due, not to
   * resend every receipt, matching the backend's own SendInvoiceEmailAsync/SmsAsync business rule. */
  protected customerHasDue(): boolean {
    return (this.customer()?.currentDue ?? 0) > 0;
  }

  protected sendInvoiceEmail(): void {
    this.sendingEmail.set(true);
    this.saleService.sendInvoiceEmail(this.saleId).subscribe({
      next: () => {
        this.sendingEmail.set(false);
        this.toast.success('Invoice emailed to the customer.');
      },
      error: (error) => {
        this.sendingEmail.set(false);
        this.toast.error(error.error?.title ?? 'Could not email the invoice.');
      }
    });
  }

  protected sendInvoiceSms(): void {
    this.sendingSms.set(true);
    this.saleService.sendInvoiceSms(this.saleId).subscribe({
      next: () => {
        this.sendingSms.set(false);
        this.toast.success('Invoice sent to the customer by SMS.');
      },
      error: (error) => {
        this.sendingSms.set(false);
        this.toast.error(error.error?.title ?? 'Could not SMS the invoice.');
      }
    });
  }

  protected viewReturns(): void {
    this.router.navigateByUrl(`/sales/${this.saleId}/returns`);
  }

  protected newSale(): void {
    this.router.navigateByUrl('/pos');
  }

  protected back(): void {
    this.navHistory.back('/sales');
  }
}
