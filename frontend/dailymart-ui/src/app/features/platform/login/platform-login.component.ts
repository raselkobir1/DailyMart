import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { PlatformAuthService } from '../../../core/auth/platform-auth.service';

/** Platform-operator login - a wholly separate identity from the tenant-scoped LoginComponent, see
 * PlatformAuthService's doc comment. */
@Component({
  selector: 'app-platform-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './platform-login.component.html',
  styleUrl: '../../auth/login/login.component.scss'
})
export class PlatformLoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly platformAuthService = inject(PlatformAuthService);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  // Pre-filled with the seeded default platform-admin credentials (see PlatformAdminSeeder /
  // docker-compose.yml's PlatformAdmin__DefaultUsername/DefaultPassword) - same convenience as the
  // tenant LoginComponent. Clear/overwrite these first if logging in as an account whose password has
  // since been changed.
  protected readonly form = this.fb.nonNullable.group({
    username: ['platform', Validators.required],
    password: ['Platform@123456', Validators.required]
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.platformAuthService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigateByUrl('/platform/tenants');
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Invalid username or password.');
      }
    });
  }
}
