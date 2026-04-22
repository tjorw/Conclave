import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { EditionService } from '../../../services/edition.service';
import {
  MyAssignedShiftSummaryDto,
  MyOrganiserSessionSummaryDto,
  MyScheduleItemDto,
  MySessionRegistrationSummaryDto,
  MyWatchedSessionSummaryDto,
  RegistrationService,
  SessionRegistrationStatus,
  SESSION_REGISTRATION_STATUS_LABEL,
} from 'shared';

const SCHEDULE_TYPE_LABEL: Record<string, string> = {
  Booked: 'Bokat',
  Watching: 'Vill se',
  Organiser: 'Arrangör',
  Shift: 'Pass',
};

const SESSION_REGISTRATION_STATUSES: readonly SessionRegistrationStatus[] = ['Confirmed', 'Cancelled'];

@Component({
  selector: 'app-my-program',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTabsModule],
  templateUrl: './my-program.component.html',
  styleUrl: './my-program.component.scss',
})
export class MyProgramComponent implements OnInit {
  private readonly editionSvc = inject(EditionService);
  private readonly regSvc     = inject(RegistrationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly bookedSessions = signal<MySessionRegistrationSummaryDto[]>([]);
  readonly watchedSessions = signal<MyWatchedSessionSummaryDto[]>([]);
  readonly organiserSessions = signal<MyOrganiserSessionSummaryDto[]>([]);
  readonly assignedShifts = signal<MyAssignedShiftSummaryDto[]>([]);
  readonly cancellingId = signal<string | null>(null);
  readonly unwatchingSessionId = signal<string | null>(null);
  readonly showWatchedInTimeline = signal(false);

  readonly scheduleItems = computed<MyScheduleItemDto[]>(() => {
    const booked = this.bookedSessions().map<MyScheduleItemDto>(session => ({
      sessionId: session.sessionId,
      shiftId: null,
      title: session.eventTitle,
      start: session.start,
      end: session.end,
      locationName: session.venueName,
      type: 'Booked',
      isPrimary: true,
    }));

    const bookedSessionIds = new Set(booked.map(item => item.sessionId).filter((id): id is string => !!id));

    const organiser = this.organiserSessions()
      .filter(session => !bookedSessionIds.has(session.sessionId))
      .map<MyScheduleItemDto>(session => ({
        sessionId: session.sessionId,
        shiftId: null,
        title: session.eventTitle,
        start: session.start,
        end: session.end,
        locationName: session.venueName,
        type: 'Organiser',
        isPrimary: false,
      }));

    const watched = this.watchedSessions()
      .filter(session => !!session.sessionId)
      .map<MyScheduleItemDto>(session => ({
        sessionId: session.sessionId,
        shiftId: null,
        title: session.eventTitle,
        start: session.start,
        end: session.end,
        locationName: session.venueName,
        type: 'Watching',
        isPrimary: false,
      }));

    // Keep a single engagement type per session id in priority order.
    const sessionItemsBySessionId = new Map<string, MyScheduleItemDto>();
    for (const item of [...booked, ...organiser, ...watched]) {
      if (!item.sessionId) continue;
      if (!sessionItemsBySessionId.has(item.sessionId)) {
        sessionItemsBySessionId.set(item.sessionId, item);
      }
    }

    const shifts = this.assignedShifts().map<MyScheduleItemDto>(shift => ({
      sessionId: null,
      shiftId: shift.shiftId,
      title: shift.stationName,
      start: shift.start,
      end: shift.end,
      locationName: shift.stationName,
      type: 'Shift',
      isPrimary: true,
    }));

    return [...sessionItemsBySessionId.values(), ...shifts]
      .sort((a, b) => Date.parse(a.start) - Date.parse(b.start));
  });

  readonly timelineItems = computed<MyScheduleItemDto[]>(() => {
    const visibleTypes = this.showWatchedInTimeline()
      ? new Set<MyScheduleItemDto['type']>(['Booked', 'Organiser', 'Shift', 'Watching'])
      : new Set<MyScheduleItemDto['type']>(['Booked', 'Organiser', 'Shift']);

    return this.scheduleItems()
      .filter(item => visibleTypes.has(item.type))
      .sort((a, b) => Date.parse(a.start) - Date.parse(b.start));
  });

  readonly conflictIds = computed<Set<string>>(() => {
    const items = this.timelineItems();
    const ids = new Set<string>();

    for (let i = 0; i < items.length; i++) {
      for (let j = i + 1; j < items.length; j++) {
        const a = items[i];
        const b = items[j];
        if (new Date(a.start) < new Date(b.end) && new Date(b.start) < new Date(a.end)) {
          ids.add(this.timelineTrackId(a));
          ids.add(this.timelineTrackId(b));
        }
      }
    }

    return ids;
  });

  ngOnInit(): void {
    this.loadAll();
  }

  toggleWatchedInTimeline(): void {
    this.showWatchedInTimeline.update(value => !value);
  }

  cancelSession(registrationId: string): void {
    if (this.cancellingId()) return;
    this.cancellingId.set(registrationId);
    this.error.set(null);
    this.regSvc.cancelSessionRegistration(registrationId).subscribe({
      next: () => {
        this.bookedSessions.update(items => items.filter(i => i.id !== registrationId));
        this.cancellingId.set(null);
      },
      error: () => {
        this.error.set('Kunde inte avboka sessionen just nu. Försök igen.');
        this.cancellingId.set(null);
      },
    });
  }

  removeWatch(sessionId: string): void {
    if (this.unwatchingSessionId()) return;
    this.unwatchingSessionId.set(sessionId);
    this.error.set(null);
    this.regSvc.unwatchSession(sessionId).subscribe({
      next: () => {
        this.watchedSessions.update(items => items.filter(i => i.sessionId !== sessionId));
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

  scheduleTypeLabel(type: string): string {
    return SCHEDULE_TYPE_LABEL[type] ?? type;
  }

  hasConflict(item: MyScheduleItemDto): boolean {
    return this.conflictIds().has(this.timelineTrackId(item));
  }

  timelineTrackId(item: MyScheduleItemDto): string {
    return `${item.type}:${item.sessionId ?? item.shiftId ?? ''}:${item.start}:${item.end}`;
  }

  private toArray(value: unknown): unknown[] {
    return Array.isArray(value) ? value : [];
  }

  private byStart<T extends { start?: string | null }>(a: T, b: T): number {
    const aTime = Date.parse(a.start ?? '');
    const bTime = Date.parse(b.start ?? '');
    if (Number.isNaN(aTime) && Number.isNaN(bTime)) return 0;
    if (Number.isNaN(aTime)) return 1;
    if (Number.isNaN(bTime)) return -1;
    return aTime - bTime;
  }

  private hasValidRange(value: { start?: string | null; end?: string | null }): boolean {
    return !Number.isNaN(Date.parse(value.start ?? '')) && !Number.isNaN(Date.parse(value.end ?? ''));
  }

  private formatDate(value: string | null | undefined): string {
    if (!value || Number.isNaN(Date.parse(value))) {
      return 'Okänd tid';
    }

    return new Intl.DateTimeFormat('sv-SE', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  private formatTime(value: string | null | undefined): string {
    if (!value || Number.isNaN(Date.parse(value))) {
      return '--:--';
    }

    return new Intl.DateTimeFormat('sv-SE', {
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  displayDate(value: string | null | undefined): string {
    return this.formatDate(value);
  }

  displayTime(value: string | null | undefined): string {
    return this.formatTime(value);
  }

  private normalizeBooked(value: unknown): MySessionRegistrationSummaryDto[] {
    return this.toArray(value)
      .filter((item): item is Record<string, unknown> => item !== null && typeof item === 'object')
      .map(item => ({
        id: typeof item['id'] === 'string' ? item['id'] : '',
        sessionId: typeof item['sessionId'] === 'string' ? item['sessionId'] : '',
        eventTitle: typeof item['eventTitle'] === 'string' ? item['eventTitle'] : '',
        start: typeof item['start'] === 'string' ? item['start'] : '',
        end: typeof item['end'] === 'string' ? item['end'] : '',
        venueName: typeof item['venueName'] === 'string' ? item['venueName'] : '',
        status: this.toSessionRegistrationStatus(item['status']),
      }))
      .filter((item): item is MySessionRegistrationSummaryDto =>
        !!item.id &&
        !!item.sessionId &&
        !!item.status &&
        this.hasValidRange(item));
  }

  private toSessionRegistrationStatus(value: unknown): SessionRegistrationStatus | null {
    return typeof value === 'string' && SESSION_REGISTRATION_STATUSES.includes(value as SessionRegistrationStatus)
      ? (value as SessionRegistrationStatus)
      : null;
  }

  private normalizeWatched(value: unknown): MyWatchedSessionSummaryDto[] {
    return this.toArray(value)
      .filter((item): item is Record<string, unknown> => item !== null && typeof item === 'object')
      .map(item => ({
        sessionId: typeof item['sessionId'] === 'string' ? item['sessionId'] : '',
        eventTitle: typeof item['eventTitle'] === 'string' ? item['eventTitle'] : '',
        start: typeof item['start'] === 'string' ? item['start'] : '',
        end: typeof item['end'] === 'string' ? item['end'] : '',
        venueName: typeof item['venueName'] === 'string' ? item['venueName'] : '',
        createdAt: typeof item['createdAt'] === 'string' ? item['createdAt'] : '',
      }))
      .filter(item => !!item.sessionId && this.hasValidRange(item));
  }

  private normalizeOrganiser(value: unknown): MyOrganiserSessionSummaryDto[] {
    return this.toArray(value)
      .filter((item): item is Record<string, unknown> => item !== null && typeof item === 'object')
      .map(item => ({
        sessionId: typeof item['sessionId'] === 'string' ? item['sessionId'] : '',
        eventTitle: typeof item['eventTitle'] === 'string' ? item['eventTitle'] : '',
        start: typeof item['start'] === 'string' ? item['start'] : '',
        end: typeof item['end'] === 'string' ? item['end'] : '',
        venueName: typeof item['venueName'] === 'string' ? item['venueName'] : '',
      }))
      .filter(item => !!item.sessionId && this.hasValidRange(item));
  }

  private normalizeShifts(value: unknown): MyAssignedShiftSummaryDto[] {
    return this.toArray(value)
      .filter((item): item is Record<string, unknown> => item !== null && typeof item === 'object')
      .map(item => ({
        shiftId: typeof item['shiftId'] === 'string' ? item['shiftId'] : '',
        stationName: typeof item['stationName'] === 'string' ? item['stationName'] : '',
        start: typeof item['start'] === 'string' ? item['start'] : '',
        end: typeof item['end'] === 'string' ? item['end'] : '',
      }))
      .filter(item => !!item.shiftId && this.hasValidRange(item));
  }

  private loadAll(): void {
    const editionId = this.editionSvc.editionId();
    if (!editionId) {
      this.loading.set(false);
      return;
    }

    forkJoin({
      booked: this.regSvc.getMySessionRegistrations(editionId).pipe(catchError(() => of([] as MySessionRegistrationSummaryDto[]))),
      watched: this.regSvc.getMyWatchedSessions(editionId).pipe(catchError(() => of([] as MyWatchedSessionSummaryDto[]))),
      organiser: this.regSvc.getMyOrganiserSessions(editionId).pipe(catchError(() => of([] as MyOrganiserSessionSummaryDto[]))),
      shifts: this.regSvc.getMyAssignedShifts(editionId).pipe(catchError(() => of([] as MyAssignedShiftSummaryDto[]))),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        try {
          const booked = this.normalizeBooked(result.booked)
            .sort((a, b) => this.byStart(a, b));

          const watched = this.normalizeWatched(result.watched)
            .sort((a, b) => this.byStart(a, b));

          const organiser = this.normalizeOrganiser(result.organiser)
            .sort((a, b) => this.byStart(a, b));

          const shifts = this.normalizeShifts(result.shifts)
            .sort((a, b) => this.byStart(a, b));

          this.bookedSessions.set(booked);
          this.watchedSessions.set(watched);
          this.organiserSessions.set(organiser);
          this.assignedShifts.set(shifts);
          this.showWatchedInTimeline.set(false);
        } catch {
          this.error.set('Kunde inte tolka programdata just nu. Försök igen.');
        } finally {
          this.loading.set(false);
        }
      },
      error: () => {
        this.error.set('Kunde inte läsa ditt program just nu.');
        this.loading.set(false);
      },
    });
  }
}
