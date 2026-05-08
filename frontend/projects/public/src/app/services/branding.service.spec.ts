import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ConventionService } from 'shared';
import { BrandingService } from './branding.service';

describe('BrandingService', () => {
  let getBranding: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    document.documentElement.removeAttribute('style');
    document.querySelectorAll('link[rel="icon"]').forEach(link => link.remove());
    getBranding = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        BrandingService,
        {
          provide: ConventionService,
          useValue: { getBranding },
        },
      ],
    });
  });

  it('applies branding values as CSS variables and exposes logo url', async () => {
    getBranding.mockReturnValue(of({
      conventionId: 'convention-1',
      primaryColor: '#112233',
      accentColor: '#aabbcc',
      logoUrl: '/uploads/logo.svg',
      faviconUrl: '/uploads/favicon.png',
      fontFamily: 'Open Sans',
      customCss: null,
    }));

    const service = TestBed.inject(BrandingService);
    await service.load();

    const style = document.documentElement.style;
    expect(style.getPropertyValue('--brand-primary')).toBe('#112233');
    expect(style.getPropertyValue('--brand-primary-light')).toBe('#293a4b');
    expect(style.getPropertyValue('--brand-primary-dark')).toBe('#000a1b');
    expect(style.getPropertyValue('--brand-accent')).toBe('#aabbcc');
    expect(style.getPropertyValue('--brand-font-family')).toBe("'Open Sans', sans-serif");
    expect(service.logoUrl()).toBe('/uploads/logo.svg');
    expect(document.querySelector<HTMLLinkElement>('link[rel="icon"]')?.href).toContain('/uploads/favicon.png');
  });

  it('only applies whitelisted custom CSS variables', async () => {
    getBranding.mockReturnValue(of({
      conventionId: 'convention-1',
      primaryColor: '#112233',
      accentColor: '#aabbcc',
      logoUrl: null,
      faviconUrl: null,
      fontFamily: 'Inter',
      customCss: '--brand-bg: #fafafa; color: red; --brand-border: #eeeeee; --brand-text: {bad}',
    }));

    const service = TestBed.inject(BrandingService);
    await service.load();

    const style = document.documentElement.style;
    expect(style.getPropertyValue('--brand-bg')).toBe('#fafafa');
    expect(style.getPropertyValue('--brand-border')).toBe('#eeeeee');
    expect(style.getPropertyValue('color')).toBe('');
    expect(style.getPropertyValue('--brand-text')).toBe('');
  });

  it('keeps system fallbacks when branding fetch fails', async () => {
    getBranding.mockReturnValue(throwError(() => new Error('missing')));

    const service = TestBed.inject(BrandingService);
    await service.load();

    const style = document.documentElement.style;
    expect(style.getPropertyValue('--brand-primary')).toBe('#1b2a4a');
    expect(style.getPropertyValue('--brand-accent')).toBe('#e8920a');
    expect(style.getPropertyValue('--brand-font-family')).toBe("Roboto, 'Helvetica Neue', sans-serif");
    expect(service.logoUrl()).toBeNull();
  });
});
