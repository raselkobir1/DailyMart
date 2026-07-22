import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BackupFrequency, ShopSettingsDto } from '../settings.model';
import { SettingsService } from '../settings.service';

@Component({
  selector: 'app-settings-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './settings-form.component.html',
  styleUrl: './settings-form.component.scss'
})
export class SettingsFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly settingsService = inject(SettingsService);
  private readonly snackBar = inject(MatSnackBar);

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
        this.snackBar.open('Could not load settings.', 'Dismiss');
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
        this.snackBar.open('Settings saved.', 'Dismiss', { duration: 3000 });
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Could not save settings.', 'Dismiss');
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
        this.snackBar.open('Logo updated.', 'Dismiss', { duration: 3000 });
      },
      error: () => {
        this.uploadingLogo.set(false);
        this.snackBar.open('Could not upload logo.', 'Dismiss');
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
