import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ShopSettingsDto, UpdateShopSettingsRequest } from './settings.model';
import { SettingsService } from './settings.service';

describe('SettingsService', () => {
  let service: SettingsService;
  let httpMock: HttpTestingController;

  const fakeSettings: ShopSettingsDto = {
    id: 1,
    shopName: 'DailyMart',
    shopAddress: null,
    shopPhone: null,
    shopEmail: null,
    shopLogoUrl: null,
    invoicePrefix: 'INV-',
    invoiceFooterText: null,
    currencyCode: 'BDT',
    currencySymbol: '৳',
    defaultVatPercentage: 0,
    defaultDiscountPercentage: 0,
    backupEnabled: false,
    backupFrequency: 'Daily',
    dateFormat: 'dd/MM/yyyy',
    timeZone: 'UTC'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(SettingsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('get() issues a GET to /settings', () => {
    service.get().subscribe((result) => expect(result).toEqual(fakeSettings));

    const req = httpMock.expectOne('/settings');
    expect(req.request.method).toBe('GET');
    req.flush(fakeSettings);
  });

  it('update() issues a PUT to /settings with the request body', () => {
    const request: UpdateShopSettingsRequest = {
      shopName: 'Renamed Shop',
      shopAddress: null,
      shopPhone: null,
      shopEmail: null,
      invoicePrefix: 'INV-',
      invoiceFooterText: null,
      currencyCode: 'BDT',
      currencySymbol: '৳',
      defaultVatPercentage: 0,
      defaultDiscountPercentage: 0,
      backupEnabled: false,
      backupFrequency: 'Daily',
      dateFormat: 'dd/MM/yyyy',
      timeZone: 'UTC'
    };

    service.update(request).subscribe();

    const req = httpMock.expectOne('/settings');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush({ ...fakeSettings, shopName: 'Renamed Shop' });
  });

  it('uploadLogo() posts a FormData body to /settings/logo', () => {
    const file = new File(['fake-image-bytes'], 'logo.png', { type: 'image/png' });

    service.uploadLogo(file).subscribe();

    const req = httpMock.expectOne('/settings/logo');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeInstanceOf(FormData);
    req.flush({ ...fakeSettings, shopLogoUrl: '/uploads/logos/new.png' });
  });
});
