import { Injectable } from '@angular/core';
import { safeStorage } from './auth/safe-storage';

export type ThemeMode = 'light' | 'dark';
export type AccentName = 'indigo' | 'emerald' | 'rose' | 'amber' | 'sky';

const MODE_KEY = 'dailymart.themeMode';
const ACCENT_KEY = 'dailymart.themeAccent';

const ACCENTS: Record<AccentName, { brand: string; brand2: string }> = {
  indigo: { brand: '#4f46e5', brand2: '#6366f1' },
  emerald: { brand: '#059669', brand2: '#10b981' },
  rose: { brand: '#e11d48', brand2: '#f43f5e' },
  amber: { brand: '#d97706', brand2: '#f59e0b' },
  sky: { brand: '#0284c7', brand2: '#0ea5e9' }
};

/**
 * The whole theming engine: stores mode/accent in localStorage and applies them by setting a data-theme
 * attribute + --brand/--brand-2 CSS vars directly on the root element - no Angular Material theming API
 * involved (there is no Material in this app), no per-component theme inputs to thread through.
 */
@Injectable({ providedIn: 'root' })
export class Theme {
  private mode: ThemeMode = (safeStorage.getItem(MODE_KEY) as ThemeMode | null) ?? 'light';
  private accent: AccentName = (safeStorage.getItem(ACCENT_KEY) as AccentName | null) ?? 'indigo';

  constructor() {
    this.apply();
  }

  getMode(): ThemeMode {
    return this.mode;
  }

  getAccent(): AccentName {
    return this.accent;
  }

  toggleMode(): void {
    this.mode = this.mode === 'light' ? 'dark' : 'light';
    safeStorage.setItem(MODE_KEY, this.mode);
    this.apply();
  }

  setAccent(accent: AccentName): void {
    this.accent = accent;
    safeStorage.setItem(ACCENT_KEY, accent);
    this.apply();
  }

  private apply(): void {
    const root = document.documentElement;
    root.setAttribute('data-theme', this.mode);
    const { brand, brand2 } = ACCENTS[this.accent];
    root.style.setProperty('--brand', brand);
    root.style.setProperty('--brand-2', brand2);
  }
}

export const ACCENT_NAMES: AccentName[] = ['indigo', 'emerald', 'rose', 'amber', 'sky'];

/** Swatch preview colors for the accent picker - not all accent names are valid CSS color keywords
 * (e.g. "emerald"/"rose" aren't), so this can't just bind [style.background]="accentName" directly. */
export const ACCENT_PREVIEW: Record<AccentName, string> = {
  indigo: ACCENTS.indigo.brand,
  emerald: ACCENTS.emerald.brand,
  rose: ACCENTS.rose.brand,
  amber: ACCENTS.amber.brand,
  sky: ACCENTS.sky.brand
};
