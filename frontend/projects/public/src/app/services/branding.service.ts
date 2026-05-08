import { DOCUMENT } from '@angular/common';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ConventionBrandingDto, ConventionService } from 'shared';

const DEFAULT_BRANDING = {
  primaryColor: '#1b2a4a',
  primaryLight: '#2a3d6b',
  primaryDark: '#0f1a2e',
  accentColor: '#e8920a',
  accentLight: '#f0a832',
  fontFamily: "Roboto, 'Helvetica Neue', sans-serif",
};

const VARIABLE_WHITELIST = new Set([
  '--brand-primary',
  '--brand-primary-light',
  '--brand-primary-dark',
  '--brand-accent',
  '--brand-accent-light',
  '--brand-bg',
  '--brand-surface',
  '--brand-text',
  '--brand-text-muted',
  '--brand-text-secondary',
  '--brand-border',
]);

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private readonly conventionService = inject(ConventionService);
  private readonly document = inject(DOCUMENT);
  private loadPromise: Promise<void> | null = null;

  readonly logoUrl = signal<string | null>(null);

  async load(): Promise<void> {
    if (this.loadPromise) return this.loadPromise;

    this.loadPromise = this.fetchAndApplyBranding();
    return this.loadPromise;
  }

  private async fetchAndApplyBranding(): Promise<void> {
    try {
      const branding = await firstValueFrom(this.conventionService.getBranding());
      this.apply(branding);
    } catch {
      this.applyFallbacks();
    } finally {
      this.loadPromise = null;
    }
  }

  private apply(branding: ConventionBrandingDto): void {
    this.setVariable('--brand-primary', branding.primaryColor);
    this.setVariable('--brand-primary-light', adjustHexColor(branding.primaryColor, 24));
    this.setVariable('--brand-primary-dark', adjustHexColor(branding.primaryColor, -24));
    this.setVariable('--brand-accent', branding.accentColor);
    this.setVariable('--brand-accent-light', adjustHexColor(branding.accentColor, 24));
    this.setVariable('--brand-font-family', fontStack(branding.fontFamily));
    this.applyCustomCssVariables(branding.customCss);
    this.logoUrl.set(branding.logoUrl);
    this.applyFavicon(branding.faviconUrl);
  }

  private applyFallbacks(): void {
    this.setVariable('--brand-primary', DEFAULT_BRANDING.primaryColor);
    this.setVariable('--brand-primary-light', DEFAULT_BRANDING.primaryLight);
    this.setVariable('--brand-primary-dark', DEFAULT_BRANDING.primaryDark);
    this.setVariable('--brand-accent', DEFAULT_BRANDING.accentColor);
    this.setVariable('--brand-accent-light', DEFAULT_BRANDING.accentLight);
    this.setVariable('--brand-font-family', DEFAULT_BRANDING.fontFamily);
    this.logoUrl.set(null);
  }

  private applyCustomCssVariables(customCss: string | null): void {
    if (!customCss) return;

    for (const declaration of customCss.split(';')) {
      const separatorIndex = declaration.indexOf(':');
      if (separatorIndex < 0) continue;

      const name = declaration.slice(0, separatorIndex).trim();
      const value = declaration.slice(separatorIndex + 1).trim();

      if (!VARIABLE_WHITELIST.has(name) || !value || value.includes('{') || value.includes('}')) {
        continue;
      }

      this.setVariable(name, value);
    }
  }

  private applyFavicon(faviconUrl: string | null): void {
    if (!faviconUrl) return;

    let link = this.document.querySelector<HTMLLinkElement>('link[rel="icon"]');
    if (!link) {
      link = this.document.createElement('link');
      link.rel = 'icon';
      this.document.head.appendChild(link);
    }

    link.href = faviconUrl;
  }

  private setVariable(name: string, value: string): void {
    this.document.documentElement.style.setProperty(name, value);
  }
}

function fontStack(fontFamily: string): string {
  return fontFamily.includes(' ')
    ? `'${fontFamily}', sans-serif`
    : `${fontFamily}, sans-serif`;
}

function adjustHexColor(hex: string, amount: number): string {
  const match = /^#([0-9a-fA-F]{6})$/.exec(hex);
  if (!match) return hex;

  const value = match[1];
  const red = clamp(parseInt(value.slice(0, 2), 16) + amount);
  const green = clamp(parseInt(value.slice(2, 4), 16) + amount);
  const blue = clamp(parseInt(value.slice(4, 6), 16) + amount);

  return `#${toHex(red)}${toHex(green)}${toHex(blue)}`;
}

function clamp(value: number): number {
  return Math.max(0, Math.min(255, value));
}

function toHex(value: number): string {
  return value.toString(16).padStart(2, '0');
}
