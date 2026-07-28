import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { Perms } from '../../../core/perms';
import { ShopBranding } from '../../../core/shop-branding';

/** Self-service tenant signup - the only way a new company gets an account (see
 * ITenantProvisioningService on the backend). Auto-logs in on success, same "load permissions, then
 * land on the first permitted menu" flow as LoginComponent. */
@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: '../login/login.component.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly perms = inject(Perms);
  private readonly shopBranding = inject(ShopBranding);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    companyName: ['', Validators.required],
    fullName: ['', Validators.required],
    username: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    email: ['', [Validators.required, Validators.email]]
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.authService.register(this.form.getRawValue()).subscribe({
      next: () => this.afterRegister(),
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.title ?? 'Could not create your account.');
      }
    });
  }

  private afterRegister(): void {
    // See LoginComponent.afterLogin() - the same fire-and-forget load is needed here for the same reason.
    this.shopBranding.load().subscribe();

    this.perms.load().subscribe((menus) => {
      this.loading.set(false);

      if (this.perms.lastLoadFailed() || menus.length === 0) {
        this.router.navigateByUrl('/dashboard');
        return;
      }

      this.router.navigateByUrl(menus[0].route);
    });
  }
}
