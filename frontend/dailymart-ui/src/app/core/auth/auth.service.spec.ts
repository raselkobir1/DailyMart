import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthResponse } from './auth.models';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const fakeResponse: AuthResponse = {
    accessToken: 'access-123',
    refreshToken: 'refresh-456',
    expiresAtUtc: new Date().toISOString(),
    username: 'admin',
    fullName: 'Administrator',
    role: 'Admin'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    service.clearSession();
    httpMock.verify();
  });

  it('starts unauthenticated when nothing is stored', () => {
    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('login() stores the session and marks the user authenticated', () => {
    service.login({ username: 'admin', password: 'secret' }).subscribe();

    httpMock.expectOne('/auth/login').flush(fakeResponse);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.accessToken).toBe('access-123');
    expect(service.currentUser()?.username).toBe('admin');
    expect(service.getRefreshToken()).toBe('refresh-456');
  });

  it('logout() clears the stored session', () => {
    service.login({ username: 'admin', password: 'secret' }).subscribe();
    httpMock.expectOne('/auth/login').flush(fakeResponse);

    service.logout().subscribe();
    httpMock.expectOne('/auth/logout').flush(null);

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
  });
});
