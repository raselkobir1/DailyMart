import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  function configureWith(isAuthenticated: boolean) {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        { provide: AuthService, useValue: { isAuthenticated: () => isAuthenticated } }
      ]
    });
  }

  it('allows navigation when authenticated', () => {
    configureWith(true);

    const result = TestBed.runInInjectionContext(() => authGuard(undefined as never, undefined as never));

    expect(result).toBe(true);
  });

  it('redirects to /login when not authenticated', () => {
    configureWith(false);

    const result = TestBed.runInInjectionContext(() => authGuard(undefined as never, undefined as never));
    const router = TestBed.inject(Router);

    expect(result).toEqual(router.createUrlTree(['/login']));
  });
});
