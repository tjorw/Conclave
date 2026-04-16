import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EditionService } from '../../../services/edition.service';
import { MySessionRegistrationSummaryDto, RegistrationService } from 'shared';

@Component({
  selector: 'app-my-program',
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './my-program.component.html',
  styleUrl: './my-program.component.scss',
})
export class MyProgramComponent implements OnInit {
  private readonly editionSvc = inject(EditionService);
  private readonly regSvc = inject(RegistrationService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly sessions = signal<MySessionRegistrationSummaryDto[]>([]);
  readonly cancellingId = signal<string | null>(null);

  ngOnInit(): void {
    this.loadSessions();
  }

  cancelSession(registrationId: string): void {
    if (this.cancellingId()) {
      return;
    }

    this.cancellingId.set(registrationId);
    this.error.set(null);

    this.regSvc.cancelSessionRegistration(registrationId).subscribe({
      next: () => {
        this.sessions.update(items => items.filter(item => item.id !== registrationId));
        this.cancellingId.set(null);
      },
      error: () => {
        this.error.set('Kunde inte avboka sessionen just nu. Försök igen.');
        this.cancellingId.set(null);
      },
    });
  }

  sessionStatusLabel(status: string): string {
    if (status === 'Confirmed') {
      return 'Bekräftad';
    }

    if (status === 'Cancelled') {
      return 'Avbokad';
    }

    return status;
  }

  private loadSessions(): void {
    const editionId = this.editionSvc.editionId();
    if (!editionId) {
      this.loading.set(false);
      return;
    }

    this.regSvc.getMySessionRegistrations(editionId).subscribe({
      next: sessions => {
        this.sessions.set([...sessions].sort((a, b) =>
          new Date(a.start).getTime() - new Date(b.start).getTime()
        ));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Kunde inte läsa ditt program just nu.');
        this.loading.set(false);
      },
    });
  }
}
