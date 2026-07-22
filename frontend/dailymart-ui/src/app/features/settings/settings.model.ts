export type BackupFrequency = 'Daily' | 'Weekly' | 'Monthly';

export interface ShopSettingsDto {
  id: number;
  shopName: string;
  shopAddress: string | null;
  shopPhone: string | null;
  shopEmail: string | null;
  shopLogoUrl: string | null;
  invoicePrefix: string;
  invoiceFooterText: string | null;
  currencyCode: string;
  currencySymbol: string;
  defaultVatPercentage: number;
  defaultDiscountPercentage: number;
  backupEnabled: boolean;
  backupFrequency: BackupFrequency;
  dateFormat: string;
  timeZone: string;
}

export interface UpdateShopSettingsRequest {
  shopName: string;
  shopAddress: string | null;
  shopPhone: string | null;
  shopEmail: string | null;
  invoicePrefix: string;
  invoiceFooterText: string | null;
  currencyCode: string;
  currencySymbol: string;
  defaultVatPercentage: number;
  defaultDiscountPercentage: number;
  backupEnabled: boolean;
  backupFrequency: BackupFrequency;
  dateFormat: string;
  timeZone: string;
}
