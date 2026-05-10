import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { LocaleService, SUPPORTED_LOCALES, SupportedLocale } from '../services/locale.service';

const LOCALE_DISPLAY: Record<SupportedLocale, string> = {
  sv: 'SV',
  en: 'EN',
};

@Component({
  selector: 'app-language-selector',
  standalone: true,
  imports: [MatButtonModule, MatMenuModule, MatIconModule],
  template: `
    <button mat-button [matMenuTriggerFor]="langMenu" class="lang-btn">
      <mat-icon>language</mat-icon>
      {{ currentLabel() }}
    </button>
    <mat-menu #langMenu>
      @for (locale of locales; track locale) {
        <button mat-menu-item (click)="select(locale)"
                [class.active-locale]="localeSvc.locale() === locale">
          {{ LOCALE_DISPLAY[locale] }}
        </button>
      }
    </mat-menu>
  `,
  styles: [`
    .lang-btn {
      min-width: 0;
      color: #fff !important;
      background: rgba(255, 255, 255, .12);
    }

    .lang-btn:hover {
      background: rgba(255, 255, 255, .2);
    }

    .lang-btn mat-icon {
      color: #fff;
    }

    .active-locale { font-weight: 600; }
  `],
})
export class LanguageSelectorComponent {
  readonly localeSvc = inject(LocaleService);
  readonly locales = [...SUPPORTED_LOCALES];
  readonly LOCALE_DISPLAY = LOCALE_DISPLAY;

  currentLabel(): string {
    return LOCALE_DISPLAY[this.localeSvc.locale()];
  }

  select(locale: SupportedLocale): void {
    this.localeSvc.setLocale(locale);
  }
}
