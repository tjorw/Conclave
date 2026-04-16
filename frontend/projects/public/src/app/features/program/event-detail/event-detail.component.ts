import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  AuthService,
  FeedService,
  EventFeedDto,
  MySessionRegistrationSummaryDto,
  MyWatchedSessionSummaryDto,
  RegistrationService,
  REGISTRATION_KIND_LABEL,
  SessionFeedDto,
} from 'shared';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './event-detail.component.html',
  styleUrl: './event-detail.component.scss',
})
export class EventDetailComponent implements OnInit {
  private readonly route   = inject(ActivatedRoute);
  private readonly feedSvc = inject(FeedService);
  private readonly router = inject(Router);
  readonly authSvc = inject(AuthService);
  private readonly regSvc = inject(RegistrationService);

  readonly loading  = signal(true);
  readonly error    = signal<string | null>(null);
  readonly event    = signal<EventFeedDto | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly registrationLoading = signal(false);
  readonly submittingSessionId = signal<string | null>(null);
  readonly submittingWatchSessionId = signal<string | null>(null);
  readonly myTicketId = signal<string | null>(null);
  readonly mySessionRegistrations = signal<Record<string, string>>({});
  readonly myWatchedSessions = signal<Record<string, true>>({});

  // Expanderade sessioner
  readonly expandedSessions = signal<Set<string>>(new Set());

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.feedSvc.getEvent(id).subscribe({
      next: ev  => {
        this.event.set(ev);
        this.loading.set(false);
        this.loadRegistrationContext(ev.editionId);
      },
      error: () => { this.error.set('Evenemanget hittades inte.'); this.loading.set(false); },
    });
  }

  toggleSession(id: string): void {
    this.expandedSessions.update(set => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  sessionTimeLabel(s: SessionFeedDto): string {
    const start = new Date(s.start);
    const end   = new Date(s.end);
    return start.toLocaleDateString('sv-SE', { weekday: 'long', day: 'numeric', month: 'long' })
      + ', ' + start.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' })
      + '–' + end.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });
  }

  registrationLabel(type: string): string {
    return REGISTRATION_KIND_LABEL[type] ?? type;
  }

  capacityLevel(s: SessionFeedDto): 'green' | 'orange' | 'red' {
    if (s.maxSeats <= 0) {
      return 'red';
    }

    var ratio = s.bookedSeats / s.maxSeats;
    if (ratio >= 0.9) {
      return 'red';
    }

    if (ratio >= 0.6) {
      return 'orange';
    }

    return 'green';
  }

  capacityLabel(s: SessionFeedDto): string {
    const level = this.capacityLevel(s);
    if (level === 'red') {
      return 'Hög beläggning';
    }

    if (level === 'orange') {
      return 'Börjar bli fullt';
    }

    return 'Gott om plats';
  }

  isRegistered(sessionId: string): boolean {
    return !!this.mySessionRegistrations()[sessionId];
  }

  isWatched(sessionId: string): boolean {
    return !!this.myWatchedSessions()[sessionId];
  }

  onWatchToggle(session: SessionFeedDto, event: MouseEvent): void {
    event.stopPropagation();

    if (!this.authSvc.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    if (this.registrationLoading() || this.submittingWatchSessionId()) {
      return;
    }

    if (this.isWatched(session.id)) {
      this.unwatchSession(session.id);
      return;
    }

    this.watchSession(session.id);
  }

  onSessionAction(session: SessionFeedDto): void {
    if (!this.authSvc.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    if (this.registrationLoading() || this.submittingSessionId()) {
      return;
    }

    const existingRegistrationId = this.mySessionRegistrations()[session.id];
    if (existingRegistrationId) {
      this.cancelRegistration(session.id, existingRegistrationId);
      return;
    }

    this.registerForSession(session.id);
  }

  private loadRegistrationContext(editionId: string): void {
    if (!this.authSvc.isLoggedIn()) {
      return;
    }

    this.registrationLoading.set(true);

    forkJoin({
      ticket: this.regSvc.getMyVisitorRegistration(editionId).pipe(catchError(() => of(null))),
      sessions: this.regSvc.getMySessionRegistrations(editionId).pipe(catchError(() => of([] as MySessionRegistrationSummaryDto[]))),
      watched: this.regSvc.getMyWatchedSessions(editionId).pipe(catchError(() => of([] as MyWatchedSessionSummaryDto[]))),
    }).subscribe({
      next: result => {
        this.myTicketId.set(result.ticket?.ticketId ?? null);
        this.mySessionRegistrations.set(result.sessions.reduce<Record<string, string>>((map, item) => {
          map[item.sessionId] = item.id;
          return map;
        }, {}));
        this.myWatchedSessions.set(result.watched.reduce<Record<string, true>>((map, item) => {
          map[item.sessionId] = true;
          return map;
        }, {}));
        this.registrationLoading.set(false);
      },
      error: () => {
        this.registrationLoading.set(false);
      },
    });
  }

  private registerForSession(sessionId: string): void {
    const personId = this.authSvc.personId();
    const ticketId = this.myTicketId();

    if (!personId) {
      this.actionError.set('Du behöver vara inloggad för att anmäla dig.');
      return;
    }

    if (!ticketId) {
      this.actionError.set('Du behöver en betald biljett innan du kan anmäla dig till en session.');
      return;
    }

    this.submittingSessionId.set(sessionId);
    this.actionError.set(null);

    this.regSvc.registerForSession(sessionId, personId, ticketId).subscribe({
      next: result => {
        this.mySessionRegistrations.update(current => ({ ...current, [sessionId]: result.id }));
        this.updateBookedSeats(sessionId, 1);
        this.submittingSessionId.set(null);
      },
      error: () => {
        this.actionError.set('Kunde inte anmäla dig till sessionen just nu.');
        this.submittingSessionId.set(null);
      },
    });
  }

  private cancelRegistration(sessionId: string, registrationId: string): void {
    this.submittingSessionId.set(sessionId);
    this.actionError.set(null);

    this.regSvc.cancelSessionRegistration(registrationId).subscribe({
      next: () => {
        this.mySessionRegistrations.update(current => {
          const next = { ...current };
          delete next[sessionId];
          return next;
        });
        this.updateBookedSeats(sessionId, -1);
        this.submittingSessionId.set(null);
      },
      error: () => {
        this.actionError.set('Kunde inte avboka sessionen just nu.');
        this.submittingSessionId.set(null);
      },
    });
  }

  private watchSession(sessionId: string): void {
    this.submittingWatchSessionId.set(sessionId);
    this.actionError.set(null);

    this.regSvc.watchSession(sessionId).subscribe({
      next: () => {
        this.myWatchedSessions.update(current => ({ ...current, [sessionId]: true }));
        this.submittingWatchSessionId.set(null);
      },
      error: () => {
        this.actionError.set('Kunde inte bevaka sessionen just nu.');
        this.submittingWatchSessionId.set(null);
      },
    });
  }

  private unwatchSession(sessionId: string): void {
    this.submittingWatchSessionId.set(sessionId);
    this.actionError.set(null);

    this.regSvc.unwatchSession(sessionId).subscribe({
      next: () => {
        this.myWatchedSessions.update(current => {
          const next = { ...current };
          delete next[sessionId];
          return next;
        });
        this.submittingWatchSessionId.set(null);
      },
      error: () => {
        this.actionError.set('Kunde inte ta bort bevakningen just nu.');
        this.submittingWatchSessionId.set(null);
      },
    });
  }

  private updateBookedSeats(sessionId: string, delta: number): void {
    this.event.update(current => {
      if (!current) {
        return current;
      }

      return {
        ...current,
        sessions: current.sessions.map(s => {
          if (s.id !== sessionId) {
            return s;
          }

          return {
            ...s,
            bookedSeats: Math.max(0, s.bookedSeats + delta),
          };
        }),
      };
    });
  }
}
