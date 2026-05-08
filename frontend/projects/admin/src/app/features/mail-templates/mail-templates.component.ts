import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MailTemplateSummaryDto, MailTemplateService } from 'shared';

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
  selector: 'app-mail-templates',
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <div class="page-header">
      <div>
        <h1 class="page-title">E-postmallar</h1>
        <p class="page-meta">Anpassa de e-postmeddelanden som systemet skickar ut.</p>
      </div>
    </div>

    @if (loading()) {
      <div class="spinner-center"><mat-spinner diameter="42" /></div>
    } @else if (error()) {
      <div class="error-banner">{{ error() }}</div>
    } @else {
      <table class="data-table">
        <thead>
          <tr>
            <th>Malltyp</th>
            <th>Status</th>
            <th>Senast ändrad</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (t of templates(); track t.templateType) {
            <tr class="clickable-row" [routerLink]="['/mail-templates', t.templateType]">
              <td>{{ label(t.templateType) }}</td>
              <td>
                <span class="chip" [class.chip-green]="t.isCustomized" [class.chip-grey]="!t.isCustomized">
                  {{ t.isCustomized ? 'Anpassad' : 'Standard' }}
                </span>
              </td>
              <td>{{ t.updatedAt ? (t.updatedAt | date:'yyyy-MM-dd HH:mm') : '–' }}</td>
              <td>
                <a mat-button [routerLink]="['/mail-templates', t.templateType]" (click)="$event.stopPropagation()">
                  <mat-icon>edit</mat-icon>
                  Redigera
                </a>
              </td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
})
export class MailTemplatesComponent {
  private readonly svc = inject(MailTemplateService);

  readonly templates = signal<MailTemplateSummaryDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.svc.listTemplates().subscribe({
      next: ts => { this.templates.set(ts); this.loading.set(false); },
      error: () => { this.error.set('Kunde inte ladda mallarna.'); this.loading.set(false); },
    });
  }

  label(type: string): string {
    return TEMPLATE_LABELS[type] ?? type;
  }
}
