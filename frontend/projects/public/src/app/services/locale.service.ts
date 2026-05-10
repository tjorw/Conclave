import { Injectable, signal } from '@angular/core';

export const SUPPORTED_LOCALES = ['sv', 'en'] as const;
export type SupportedLocale = typeof SUPPORTED_LOCALES[number];

@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly _locale = signal<SupportedLocale>(this.initLocale());
  readonly locale = this._locale.asReadonly();

  setLocale(locale: SupportedLocale): void {
    this._locale.set(locale);
    localStorage.setItem('preferred_locale', locale);
  }

  private initLocale(): SupportedLocale {
    const stored = localStorage.getItem('preferred_locale');
    if (stored && (SUPPORTED_LOCALES as readonly string[]).includes(stored)) {
      return stored as SupportedLocale;
    }
    const browser = navigator.language.split('-')[0];
    if ((SUPPORTED_LOCALES as readonly string[]).includes(browser)) {
      return browser as SupportedLocale;
    }
    return 'sv';
  }
}
