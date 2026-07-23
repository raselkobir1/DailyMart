import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { Perms } from '../../../core/perms';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly perms = inject(Perms);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  // Pre-filled with the seeded default admin credentials (see AdminSeeder /
  // appsettings.Development.json's Admin:DefaultUsername/DefaultPassword) so a first-time user on a
  // fresh install can just click "Sign in" with no typing - or clear/overwrite these fields first if
  // they're logging in as an account whose password has since been changed.
  protected readonly form = this.fb.nonNullable.group({
    username: ['admin', Validators.required],
    password: ['Admin@123456', Validators.required]
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => this.afterLogin(),
      error: () => {
        this.loading.set(false);
        this.error.set('Invalid username or password.');
      }
    });
  }

  private afterLogin(): void {
    this.perms.load().subscribe({
      next: (menus) => {
        this.loading.set(false);

        if (menus.length === 0) {
          // Mirrors the reference app: a role with no visible menus is barred from the admin app
          // entirely, even though the credentials themselves were valid.
          this.authService.clearSession();
          this.error.set('This account has no admin access.');
          return;
        }

        this.router.navigateByUrl(menus[0].route);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigateByUrl('/audit-log');
      }
    });
  }
}
