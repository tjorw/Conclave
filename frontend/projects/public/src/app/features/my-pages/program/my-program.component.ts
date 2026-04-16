import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { EditionService } from '../../../services/edition.service';
import { MySessionRegistrationSummaryDto, MyWatchedSessionSummaryDto, RegistrationService, SESSION_REGISTRATION_STATUS_LABEL } from 'shared';

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
  readonly bookedSessions = signal<MySessionRegistrationSummaryDto[]>([]);
  readonly watchedSessions = signal<MyWatchedSessionSummaryDto[]>([]);
  readonly cancellingId = signal<string | null>(null);
  readonly unwatchingSessionId = signal<string | null>(null);

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
        this.bookedSessions.update(items => items.filter(item => item.id !== registrationId));
        this.cancellingId.set(null);
      },
      error: () => {
        this.error.set('Kunde inte avboka sessionen just nu. Försök igen.');
        this.cancellingId.set(null);
      },
    });
  }

  removeWatch(sessionId: string): void {
    if (this.unwatchingSessionId()) {
      return;
    }

    this.unwatchingSessionId.set(sessionId);
    this.error.set(null);

    this.regSvc.unwatchSession(sessionId).subscribe({
      next: () => {
        this.watchedSessions.update(items => items.filter(item => item.sessionId !== sessionId));
        this.unwatchingSessionId.set(null);
      },
      error: () => {
        this.error.set('Kunde inte ta bort bevakningen just nu. Försök igen.');
        this.unwatchingSessionId.set(null);
      },
    });
  }

  sessionStatusLabel(status: string): string {
    return SESSION_REGISTRATION_STATUS_LABEL[status] ?? status;
  }

  private loadSessions(): void {
    const editionId = this.editionSvc.editionId();
    if (!editionId) {
      this.loading.set(false);
      return;
    }

    forkJoin({
      booked: this.regSvc.getMySessionRegistrations(editionId).pipe(catchError(() => of([] as MySessionRegistrationSummaryDto[]))),
      watched: this.regSvc.getMyWatchedSessions(editionId).pipe(catchError(() => of([] as MyWatchedSessionSummaryDto[]))),
    }).subscribe({
      next: result => {
        this.bookedSessions.set([...result.booked].sort((a, b) =>
          new Date(a.start).getTime() - new Date(b.start).getTime()
        ));
        this.watchedSessions.set([...result.watched].sort((a, b) =>
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
