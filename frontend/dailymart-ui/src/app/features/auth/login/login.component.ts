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
    // Perms.load() never errors out to the subscriber - it catches internally and resolves to an empty
    // list, setting lastLoadFailed() so this distinguishes "couldn't check permissions" (a transient
    // failure right after a valid login - stay signed in, let them retry) from "checked, and this role
    // genuinely has zero visible menus" (the reference app's real "no admin access" outcome).
    this.perms.load().subscribe((menus) => {
      this.loading.set(false);

      if (this.perms.lastLoadFailed()) {
        this.error.set('Signed in, but could not load your account permissions. Please try again.');
        return;
      }

      if (menus.length === 0) {
        this.authService.clearSession();
        this.error.set('This account has no admin access.');
        return;
      }

      this.router.navigateByUrl(menus[0].route);
    });
  }
}
