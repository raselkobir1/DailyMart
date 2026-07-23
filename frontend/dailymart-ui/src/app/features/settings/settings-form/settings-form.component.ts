import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Toast } from '../../../core/toast';
import { BackupFrequency, ShopSettingsDto } from '../settings.model';
import { SettingsService } from '../settings.service';

@Component({
  selector: 'app-settings-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './settings-form.component.html',
  styleUrl: './settings-form.component.scss'
})
export class SettingsFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly settingsService = inject(SettingsService);
  private readonly toast = inject(Toast);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly uploadingLogo = signal(false);
  protected readonly logoUrl = signal<string | null>(null);

  protected readonly backupFrequencies: BackupFrequency[] = ['Daily', 'Weekly', 'Monthly'];

  protected readonly form = this.fb.nonNullable.group({
    shopName: ['', Validators.required],
    shopAddress: [''],
    shopPhone: [''],
    shopEmail: ['', Validators.email],
    invoicePrefix: ['', Validators.required],
    invoiceFooterText: [''],
    currencyCode: ['', Validators.required],
    currencySymbol: ['', Validators.required],
    defaultVatPercentage: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    defaultDiscountPercentage: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    backupEnabled: [false],
    backupFrequency: ['Daily' as BackupFrequency, Validators.required],
    dateFormat: ['', Validators.required],
    timeZone: ['', Validators.required]
  });

  ngOnInit(): void {
    this.settingsService.get().subscribe({
      next: (settings) => {
        this.applySettings(settings);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load settings.');
      }
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.settingsService.update(this.form.getRawValue()).subscribe({
      next: (settings) => {
        this.applySettings(settings);
        this.saving.set(false);
        this.toast.success('Settings saved.');
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Could not save settings.');
      }
    });
  }

  protected onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.uploadingLogo.set(true);
    this.settingsService.uploadLogo(file).subscribe({
      next: (settings) => {
        this.applySettings(settings);
        this.uploadingLogo.set(false);
        this.toast.success('Logo updated.');
      },
      error: () => {
        this.uploadingLogo.set(false);
        this.toast.error('Could not upload logo.');
      }
    });

    input.value = '';
  }

  private applySettings(settings: ShopSettingsDto): void {
    this.logoUrl.set(settings.shopLogoUrl);
    this.form.patchValue({
      shopName: settings.shopName,
      shopAddress: settings.shopAddress ?? '',
      shopPhone: settings.shopPhone ?? '',
      shopEmail: settings.shopEmail ?? '',
      invoicePrefix: settings.invoicePrefix,
      invoiceFooterText: settings.invoiceFooterText ?? '',
      currencyCode: settings.currencyCode,
      currencySymbol: settings.currencySymbol,
      defaultVatPercentage: settings.defaultVatPercentage,
      defaultDiscountPercentage: settings.defaultDiscountPercentage,
      backupEnabled: settings.backupEnabled,
      backupFrequency: settings.backupFrequency,
      dateFormat: settings.dateFormat,
      timeZone: settings.timeZone
    });
  }
}
