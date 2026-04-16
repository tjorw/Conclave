import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { EditionService } from '../../../services/edition.service';
import {
  MyScheduleItemDto,
  MySessionRegistrationSummaryDto,
  MyWatchedSessionSummaryDto,
  RegistrationService,
  SESSION_REGISTRATION_STATUS_LABEL,
} from 'shared';

type DayGroup = { day: string; items: MyScheduleItemDto[] };

const SCHEDULE_TYPE_LABEL: Record<string, string> = {
  Booked: 'Bokat',
  Watching: 'Vill se',
  Organiser: 'Arrangör',
  Shift: 'Pass',
};

@Component({
  selector: 'app-my-program',
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTabsModule],
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
  readonly scheduleItems = signal<MyScheduleItemDto[]>([]);
  readonly cancellingId = signal<string | null>(null);
  readonly unwatchingSessionId = signal<string | null>(null);

  readonly scheduleByDay = computed<DayGroup[]>(() => {
    const grouped = new Map<string, MyScheduleItemDto[]>();
    for (const item of this.scheduleItems()) {
      const raw = new Date(item.start).toLocaleDateString('sv-SE', {
        weekday: 'long', day: 'numeric', month: 'long',
      });
      const label = raw.charAt(0).toUpperCase() + raw.slice(1);
      grouped.set(label, [...(grouped.get(label) ?? []), item]);
    }
    return Array.from(grouped.entries()).map(([day, items]) => ({ day, items }));
  });

  readonly conflictIds = computed<Set<string>>(() => {
    const primaries = this.scheduleItems().filter(i => i.isPrimary);
    const ids = new Set<string>();
    for (let i = 0; i < primaries.length; i++) {
      for (let j = i + 1; j < primaries.length; j++) {
        const a = primaries[i];
        const b = primaries[j];
        if (new Date(a.start) < new Date(b.end) && new Date(b.start) < new Date(a.end)) {
          ids.add(this.itemId(a));
          ids.add(this.itemId(b));
        }
      }
    }
    return ids;
  });

  ngOnInit(): void {
    this.loadAll();
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
    return this.conflictIds().has(this.itemId(item));
  }

  private itemId(item: MyScheduleItemDto): string {
    return item.sessionId ?? item.shiftId ?? '';
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
      schedule: this.regSvc.getMySchedule(editionId).pipe(
        map(items => items ?? []),
        catchError(() => of([] as MyScheduleItemDto[])),
      ),
    }).subscribe({
      next: result => {
        this.bookedSessions.set(
          [...result.booked].sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime())
        );
        this.watchedSessions.set(
          [...result.watched].sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime())
        );
        this.scheduleItems.set(result.schedule ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Kunde inte läsa ditt program just nu.');
        this.loading.set(false);
      },
    });
  }
}
