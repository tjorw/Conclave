import { Component, effect, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { DatePipe } from '@angular/common';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import {
  RegistrationService,
  VisitorRegistrationAdminDto,
  VISITOR_REGISTRATION_STATUS_LABEL,
  VISITOR_REGISTRATION_STATUS_CHIP,
} from 'shared';

@Component({
  selector: 'app-registrations',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTabsModule,
  ],
  templateUrl: './registrations.component.html',
  styleUrl: './registrations.component.scss',
})
export class RegistrationsComponent {
  private readonly svc = inject(RegistrationService);
  readonly editionCtx = inject(EditionContextService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);

  readonly visitorRegistrations = signal<VisitorRegistrationAdminDto[]>([]);

  constructor() {
    effect(() => {
      const edition = this.editionCtx.activeEdition();
      if (edition) this.load(edition.id);
    });
  }

  private load(editionId: string): void {
    this.loading.set(true);
    this.svc.listVisitorRegistrations(editionId).subscribe({
      next: vr => { this.visitorRegistrations.set(vr); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchRegistrations); this.loading.set(false); },
    });
  }

  private reload(): void {
    const edition = this.editionCtx.activeEdition();
    if (edition) this.load(edition.id);
  }

  private handleError(context: string, err: unknown): void {
    const detail = (err as { error?: { detail?: string } })?.error?.detail;
    this.error.set(detail ? `${context}: ${detail}` : context);
    this.saving.set(false);
  }

  confirmPayment(reg: VisitorRegistrationAdminDto): void {
    const ref = prompt('Ange betalningsreferens:');
    if (ref === null) return;
    this.saving.set(true);
    this.svc.confirmVisitorPayment(reg.id, ref).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.confirmPayment, err),
    });
  }

  cancelRegistration(reg: VisitorRegistrationAdminDto): void {
    this.saving.set(true);
    this.svc.cancelVisitorRegistration(reg.id).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.cancelRegistration, err),
    });
  }

  canCancelRegistration(reg: VisitorRegistrationAdminDto): boolean {
    return reg.status !== 'Cancelled';
  }

  statusLabel(status: string): string {
    return VISITOR_REGISTRATION_STATUS_LABEL[status] ?? status;
  }

  statusChip(status: string): string {
    return VISITOR_REGISTRATION_STATUS_CHIP[status] ?? 'chip-orange';
  }
}
