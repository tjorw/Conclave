import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal, WritableSignal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatOptionModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ConventionService, UploadService } from 'shared';

const HEX_COLOR = /^#[0-9a-fA-F]{6}$/;
const FONT_FAMILIES = ['Inter', 'Roboto', 'Open Sans', 'Lato', 'Merriweather'] as const;

const DEFAULT_BRANDING = {
  primaryColor: '#1b2a4a',
  accentColor: '#e8920a',
  logoUrl: '',
  faviconUrl: '',
  fontFamily: 'Roboto',
  customCss: '',
};

@Component({
  selector: 'app-convention-branding',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatOptionModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  template: `
    <div class="page-header">
      <div>
        <h1>Varumärke</h1>
        <p>Färger, logotyp och typsnitt för den publika webbplatsen.</p>
      </div>
    </div>

    @if (loading()) {
      <div class="spinner-container">
        <mat-spinner diameter="40" />
      </div>
    } @else {
      @if (error()) {
        <div class="error-banner">{{ error() }}</div>
      }

      <form [formGroup]="form" (ngSubmit)="save()" class="branding-layout">
        <section class="branding-form">
          <div class="field-row">
            <label class="color-field">
              <span>Primärfärg</span>
              <input type="color" formControlName="primaryColor" aria-label="Primärfärg" />
            </label>
            <mat-form-field appearance="outline">
              <mat-label>Primärfärg</mat-label>
              <input matInput formControlName="primaryColor" maxlength="7" />
              @if (form.controls.primaryColor.invalid) {
                <mat-error>Ange hex-format, t.ex. #1b2a4a.</mat-error>
              }
            </mat-form-field>
          </div>

          <div class="field-row">
            <label class="color-field">
              <span>Accentfärg</span>
              <input type="color" formControlName="accentColor" aria-label="Accentfärg" />
            </label>
            <mat-form-field appearance="outline">
              <mat-label>Accentfärg</mat-label>
              <input matInput formControlName="accentColor" maxlength="7" />
              @if (form.controls.accentColor.invalid) {
                <mat-error>Ange hex-format, t.ex. #e8920a.</mat-error>
              }
            </mat-form-field>
          </div>

          <mat-form-field appearance="outline">
            <mat-label>Typsnitt</mat-label>
            <mat-select formControlName="fontFamily">
              @for (font of fontFamilies; track font) {
                <mat-option [value]="font">{{ font }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <div class="upload-row">
            <mat-form-field appearance="outline">
              <mat-label>Logotyp-URL</mat-label>
              <input matInput formControlName="logoUrl" maxlength="1000" />
            </mat-form-field>
            <input #logoInput type="file" accept="image/jpeg,image/png,image/svg+xml,image/webp" hidden (change)="uploadLogo($event)" />
            <button mat-stroked-button type="button" (click)="logoInput.click()" [disabled]="uploadingLogo()">
              @if (uploadingLogo()) {
                <mat-spinner diameter="18" />
              } @else {
                <mat-icon>upload</mat-icon>
              }
              Logotyp
            </button>
          </div>

          <div class="upload-row">
            <mat-form-field appearance="outline">
              <mat-label>Favicon-URL</mat-label>
              <input matInput formControlName="faviconUrl" maxlength="1000" />
            </mat-form-field>
            <input #faviconInput type="file" accept="image/jpeg,image/png,image/svg+xml,image/webp" hidden (change)="uploadFavicon($event)" />
            <button mat-stroked-button type="button" (click)="faviconInput.click()" [disabled]="uploadingFavicon()">
              @if (uploadingFavicon()) {
                <mat-spinner diameter="18" />
              } @else {
                <mat-icon>upload</mat-icon>
              }
              Favicon
            </button>
          </div>

          <mat-form-field appearance="outline">
            <mat-label>CSS-variabler</mat-label>
            <textarea matInput formControlName="customCss" rows="5" maxlength="5000"></textarea>
            <mat-hint align="end">{{ form.controls.customCss.value.length }} / 5000</mat-hint>
            @if (form.controls.customCss.invalid) {
              <mat-error>Högst 5000 tecken.</mat-error>
            }
          </mat-form-field>

          @if (saved()) {
            <div class="success-inline">
              <mat-icon>check_circle</mat-icon>
              Varumärket sparades.
            </div>
          }

          <div class="action-bar">
            <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving()">
              @if (saving()) { <mat-spinner diameter="20" /> } @else { Spara }
            </button>
          </div>
        </section>

        <section class="preview" [style.font-family]="previewFont()">
          <div class="preview-header" [style.background]="form.controls.primaryColor.value">
            @if (form.controls.logoUrl.value) {
              <img [src]="form.controls.logoUrl.value" alt="" />
            } @else {
              <span class="preview-logo">Conclave</span>
            }
            <nav>
              <span>Program</span>
              <span>Biljetter</span>
              <span>Info</span>
            </nav>
          </div>
          <div class="preview-body">
            <p class="eyebrow" [style.color]="form.controls.accentColor.value">Publik startsida</p>
            <h2 [style.color]="form.controls.primaryColor.value">Välkommen till konventet</h2>
            <p>En snabb förhandsvisning av hur färger och typsnitt landar i sidhuvud och primära ytor.</p>
            <button type="button" [style.background]="form.controls.accentColor.value">Se programmet</button>
          </div>
        </section>
      </form>
    }
  `,
  styles: [`
    .page-header {
      margin-bottom: 1.5rem;
    }

    h1 {
      margin: 0;
      color: var(--brand-primary);
    }

    .page-header p {
      margin: 0.35rem 0 0;
      color: var(--brand-text-muted);
    }

    .branding-layout {
      display: grid;
      grid-template-columns: minmax(360px, 640px) minmax(320px, 1fr);
      gap: 2rem;
      align-items: start;
    }

    .branding-form {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .field-row,
    .upload-row {
      display: grid;
      grid-template-columns: 132px minmax(0, 1fr);
      gap: 1rem;
      align-items: start;
    }

    .upload-row {
      grid-template-columns: minmax(0, 1fr) 128px;
    }

    .color-field {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      color: var(--brand-text-muted);
      font-size: 0.85rem;
    }

    .color-field input {
      width: 100%;
      height: 56px;
      border: 1px solid var(--brand-border);
      border-radius: 4px;
      background: transparent;
      cursor: pointer;
    }

    .upload-row button {
      height: 56px;
    }

    .upload-row mat-spinner,
    .action-bar mat-spinner {
      display: inline-block;
      margin-right: 0.35rem;
      vertical-align: middle;
    }

    .action-bar {
      display: flex;
      gap: 1rem;
      padding-top: 0.5rem;
    }

    .preview {
      overflow: hidden;
      border: 1px solid var(--brand-border);
      border-radius: 8px;
      background: var(--brand-surface);
      min-height: 330px;
    }

    .preview-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      min-height: 72px;
      padding: 0 1.25rem;
      color: #fff;
    }

    .preview-header img {
      max-width: 150px;
      max-height: 46px;
      object-fit: contain;
    }

    .preview-logo {
      font-weight: 700;
      font-size: 1.1rem;
    }

    .preview-header nav {
      display: flex;
      gap: 1rem;
      font-size: 0.9rem;
      opacity: 0.9;
    }

    .preview-body {
      padding: 2rem;
    }

    .preview-body h2 {
      margin: 0 0 0.75rem;
      font-size: 1.8rem;
    }

    .preview-body p {
      max-width: 42ch;
      line-height: 1.55;
      color: var(--brand-text-muted);
    }

    .eyebrow {
      margin: 0 0 0.5rem;
      font-size: 0.8rem;
      font-weight: 700;
      text-transform: uppercase;
    }

    .preview-body button {
      border: 0;
      border-radius: 4px;
      color: #fff;
      cursor: default;
      font: inherit;
      font-weight: 600;
      min-height: 42px;
      padding: 0 1.2rem;
    }

    @media (max-width: 920px) {
      .branding-layout {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 620px) {
      .field-row,
      .upload-row {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class ConventionBrandingComponent {
  private readonly fb = inject(FormBuilder);
  private readonly conventionService = inject(ConventionService);
  private readonly uploadService = inject(UploadService);

  readonly fontFamilies = FONT_FAMILIES;
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);
  readonly uploadingLogo = signal(false);
  readonly uploadingFavicon = signal(false);

  readonly form = this.fb.nonNullable.group({
    primaryColor: [DEFAULT_BRANDING.primaryColor, [Validators.required, Validators.pattern(HEX_COLOR)]],
    accentColor: [DEFAULT_BRANDING.accentColor, [Validators.required, Validators.pattern(HEX_COLOR)]],
    logoUrl: [DEFAULT_BRANDING.logoUrl, Validators.maxLength(1000)],
    faviconUrl: [DEFAULT_BRANDING.faviconUrl, Validators.maxLength(1000)],
    fontFamily: [DEFAULT_BRANDING.fontFamily, Validators.required],
    customCss: [DEFAULT_BRANDING.customCss, Validators.maxLength(5000)],
  });
  readonly previewFont = computed(() => `${this.form.controls.fontFamily.value}, sans-serif`);

  constructor() {
    this.load();
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;

    this.saving.set(true);
    this.saved.set(false);
    this.error.set(null);

    const value = this.form.getRawValue();
    this.conventionService.setBranding({
      primaryColor: value.primaryColor,
      accentColor: value.accentColor,
      logoUrl: value.logoUrl || null,
      faviconUrl: value.faviconUrl || null,
      fontFamily: value.fontFamily,
      customCss: value.customCss || null,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Det gick inte att spara varumärket. Kontrollera värdena och försök igen.');
      },
    });
  }

  uploadLogo(event: Event): void {
    this.uploadBrandingImage(event, this.uploadingLogo, url => this.form.controls.logoUrl.setValue(url));
  }

  uploadFavicon(event: Event): void {
    this.uploadBrandingImage(event, this.uploadingFavicon, url => this.form.controls.faviconUrl.setValue(url));
  }

  private load(): void {
    this.loading.set(true);
    this.conventionService.getBranding().subscribe({
      next: branding => {
        this.form.patchValue({
          primaryColor: branding.primaryColor,
          accentColor: branding.accentColor,
          logoUrl: branding.logoUrl ?? '',
          faviconUrl: branding.faviconUrl ?? '',
          fontFamily: branding.fontFamily,
          customCss: branding.customCss ?? '',
        });
        this.loading.set(false);
      },
      error: error => {
        if (error instanceof HttpErrorResponse && error.status === 404) {
          this.form.patchValue(DEFAULT_BRANDING);
        } else {
          this.error.set('Det gick inte att hämta varumärkesinställningar.');
        }
        this.loading.set(false);
      },
    });
  }

  private uploadBrandingImage(event: Event, uploading: WritableSignal<boolean>, setUrl: (url: string) => void): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';

    if (!file || uploading()) return;

    uploading.set(true);
    this.error.set(null);
    this.uploadService.uploadBrandingImage(file).subscribe({
      next: url => {
        setUrl(url);
        uploading.set(false);
      },
      error: () => {
        this.error.set('Det gick inte att ladda upp bilden. Tillåtna format är JPEG, PNG, SVG och WebP, max 1 MB.');
        uploading.set(false);
      },
    });
  }
}
