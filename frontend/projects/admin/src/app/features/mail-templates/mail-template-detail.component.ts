import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MarkdownEditorComponent, MailTemplateDto, MailTemplateService } from 'shared';

const TEMPLATE_LABELS: Record<string, string> = {
  VisitorRegistrationConfirmed: 'Besöksregistrering bekräftad',
  StaffApplicationReceived: 'Funktionärsansökan mottagen',
  StaffApplicationAccepted: 'Funktionärsansökan godkänd',
  StaffApplicationRejected: 'Funktionärsansökan nekad',
  EventApproved: 'Evenemang godkänt',
  EventRejected: 'Evenemang behövde justeras',
  CoOrganiserInvitation: 'Inbjudan som medarrangör',
};

@Component({
  selector: 'app-mail-template-detail',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MarkdownEditorComponent,
  ],
  template: `
    <div class="page-header">
      <div>
        <a mat-button routerLink="/mail-templates">
          <mat-icon>arrow_back</mat-icon>
          E-postmallar
        </a>
        <h1 class="page-title">{{ templateLabel() }}</h1>
      </div>
    </div>

    @if (loading()) {
      <div class="spinner-center"><mat-spinner diameter="42" /></div>
    } @else if (error()) {
      <div class="error-banner">{{ error() }}</div>
    } @else {
      <div class="form-section">

        @if (template()?.availableVariables?.length) {
          <div class="info-box" style="margin-bottom: 1rem;">
            <strong>Tillgängliga variabler:</strong>
            @for (v of template()!.availableVariables; track v) {
              <code style="margin-left: 0.5rem; background: #f0f0f0; padding: 2px 6px; border-radius: 3px;">{{ '{{' + v + '}}' }}</code>
            }
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="save()">
          <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 1rem;">
            <mat-label>Ämnesrad</mat-label>
            <input matInput formControlName="subject" placeholder="Ämnesrad" />
            <mat-error>Ämnesrad är obligatorisk (max 500 tecken).</mat-error>
          </mat-form-field>

          <div style="margin-bottom: 1.5rem;">
            <label style="display: block; margin-bottom: 0.5rem; font-weight: 500;">Brödtext (markdown)</label>
            <lib-markdown-editor formControlName="bodyMarkdown" [maxLength]="20000" />
          </div>

          @if (saveError()) {
            <div class="error-banner">{{ saveError() }}</div>
          }
          @if (saved()) {
            <div class="success-banner">Mallen har sparats.</div>
          }

          <div class="form-actions">
            <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving()">
              @if (saving()) { <mat-spinner diameter="18" /> } @else { Spara }
            </button>

            @if (template()?.isCustomized) {
              <button
                mat-button
                type="button"
                [disabled]="resetting()"
                (click)="reset()">
                <mat-icon>restore</mat-icon>
                Återställ till standard
              </button>
            }
          </div>
        </form>
      </div>
    }
  `,
})
export class MailTemplateDetailComponent {
  private readonly svc = inject(MailTemplateService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly templateType = this.route.snapshot.paramMap.get('type') ?? '';
  readonly templateLabel = signal(TEMPLATE_LABELS[this.templateType] ?? this.templateType);

  readonly template = signal<MailTemplateDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly resetting = signal(false);
  readonly saved = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    subject: ['', [Validators.required, Validators.maxLength(500)]],
    bodyMarkdown: ['', [Validators.maxLength(20000)]],
  });

  constructor() {
    this.svc.getTemplate(this.templateType).subscribe({
      next: t => {
        this.template.set(t);
        this.form.setValue({ subject: t.subject, bodyMarkdown: t.bodyMarkdown });
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Kunde inte ladda mallen.');
        this.loading.set(false);
      },
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.saved.set(false);
    this.saveError.set(null);

    const { subject, bodyMarkdown } = this.form.getRawValue();
    this.svc.updateTemplate(this.templateType, { subject, bodyMarkdown }).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
        this.template.update(t => t ? { ...t, isCustomized: true } : t);
      },
      error: () => {
        this.saving.set(false);
        this.saveError.set('Kunde inte spara mallen. Försök igen.');
      },
    });
  }

  reset(): void {
    if (!confirm('Är du säker på att du vill återställa mallen till standardtexten?')) return;
    this.resetting.set(true);
    this.saveError.set(null);
    this.saved.set(false);

    this.svc.resetTemplate(this.templateType).subscribe({
      next: () => {
        this.svc.getTemplate(this.templateType).subscribe({
          next: t => {
            this.template.set(t);
            this.form.setValue({ subject: t.subject, bodyMarkdown: t.bodyMarkdown });
            this.resetting.set(false);
          },
          error: () => this.resetting.set(false),
        });
      },
      error: () => {
        this.resetting.set(false);
        this.saveError.set('Kunde inte återställa mallen.');
      },
    });
  }
}
