import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable, forkJoin, map, of, switchMap } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  ConventionService,
  EditionDto,
  EditionSessionDto,
  EventDto,
  EventService,
  START_TYPE_LABEL,
  VenueDto,
  DateTimeRangeComponent,
  toErrorMessage,
} from 'shared';
import { ERROR } from '../../labels/errors.labels';
import { SESSIONS_OVERVIEW } from '../../labels/pages.labels';
import { ACTION, FIELD, TOOLTIP } from '../../labels/ui.labels';
import { EditionContextService } from '../../services/edition-context.service';
import { EventTimelineComponent } from '../../shared/event-timeline/event-timeline.component';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../shared/confirm-dialog/confirm-dialog.component';
import { DraftBlock, SessionTimelineComponent } from '../../shared/session-timeline/session-timeline.component';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type SessionSortKey = 'event' | 'start' | 'end' | 'venue' | 'seats' | 'startType';

@Component({
  selector: 'app-sessions-overview',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
    EventTimelineComponent,
    SessionTimelineComponent,
    DateTimeRangeComponent,
  ],
  templateUrl: './sessions-overview.component.html',
  styleUrl: './sessions-overview.component.scss',
})
export class SessionsOverviewComponent {
  private readonly eventSvc = inject(EventService);
  private readonly conventionSvc = inject(ConventionService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  readonly editionContext = inject(EditionContextService);

  readonly PAGE = SESSIONS_OVERVIEW;
  readonly ACTION = ACTION;
  readonly TOOLTIP = TOOLTIP;
  readonly FIELD = FIELD;

  readonly edition = signal<EditionDto | null>(null);
  readonly events = signal<EventDto[]>([]);
  readonly sessions = signal<EditionSessionDto[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly editingSessionId = signal<string | null>(null);

  readonly day = signal<string | null>(null);
  readonly schedulePerspective = signal<'venue' | 'event'>('venue');
  readonly viewMode = signal<'timeline' | 'table'>('timeline');
  readonly buildingFilter = signal<string>('all');
  readonly categoryFilter = signal<string>('all');
  readonly searchText = signal('');
  readonly sort = signal<SortState<SessionSortKey>>({ key: 'start', direction: 'asc' });

  readonly form = this.fb.group({
    eventId: ['', Validators.required],
    venueId: ['', Validators.required],
    startTime: ['', Validators.required],
    endTime: ['', Validators.required],
    maxSeats: [20, [Validators.required, Validators.min(1)]],
    startType: ['FixedTime', Validators.required],
    note: [''],
  });

  private readonly formValues = toSignal(this.form.valueChanges, {
    initialValue: this.form.value,
  });

  readonly startTypes = (Object.entries(START_TYPE_LABEL) as [string, string][])
    .map(([value, label]) => ({ value, label }));

  readonly dayOptions = computed(() => {
    const ed = this.edition();
    if (!ed) return [] as string[];

    const options: string[] = [];
    const start = this.parseDateLocal(ed.start);
    const end = this.parseDateLocal(ed.end);

    for (const d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      options.push(this.formatDateOnly(d));
    }

    return options;
  });

  readonly scheduleRequestForSelectedEvent = computed(() => {
    const eventId = this.formValues()?.eventId ?? '';
    const event = this.events().find(e => e.id === eventId);
    return event?.scheduleRequestText?.trim() ?? '';
  });

  readonly sortedSessions = computed(() =>
    [...this.sessions()].sort((a, b) =>
      new Date(a.start).getTime() - new Date(b.start).getTime()
    )
  );

  readonly buildingOptions = computed(() => {
    const buildings = new Set(
      (this.edition()?.venues ?? [])
        .map(v => (v.building ?? '').trim())
        .filter(v => v.length > 0)
    );

    return [...buildings].sort((a, b) => a.localeCompare(b, 'sv-SE'));
  });

  readonly categoryOptions = computed(() => {
    const categories = new Map<string, string>();
    for (const event of this.events()) {
      const name = event.categoryName?.trim() || event.categoryId;
      categories.set(event.categoryId, name);
    }
    return [...categories.entries()]
      .map(([id, name]) => ({ id, name }))
      .sort((a, b) => a.name.localeCompare(b.name, 'sv-SE'));
  });

  private readonly eventCategoryMap = computed(() => {
    const map = new Map<string, string>();
    for (const event of this.events()) {
      map.set(event.id, event.categoryId);
    }
    return map;
  });

  readonly filteredSessions = computed(() => {
    const selectedDay = this.day();
    const perspective = this.schedulePerspective();
    const selectedBuilding = this.buildingFilter();
    const selectedCategory = this.categoryFilter();
    const search = this.searchText().trim().toLowerCase();

    return this.sortedSessions().filter(s => {
      if (selectedDay && !s.start.startsWith(selectedDay)) return false;

      const venue = this.venueById(s.venueId);

      if (perspective === 'venue' && selectedBuilding !== 'all') {
        if ((venue?.building ?? '') !== selectedBuilding) return false;
      }

      if (perspective === 'event' && selectedCategory !== 'all') {
        if (this.eventCategoryMap().get(s.eventId) !== selectedCategory) return false;
      }

      if (search.length > 0) {
        const haystack = `${s.eventTitle} ${venue?.name ?? ''} ${venue?.building ?? ''}`.toLowerCase();
        if (!haystack.includes(search)) return false;
      }

      return true;
    });
  });

  readonly sortedFilteredSessions = computed(() =>
    sortBy(this.filteredSessions(), this.sort(), {
      event: s => s.eventTitle,
      start: s => s.start,
      end: s => s.end,
      venue: s => this.venueById(s.venueId)?.name ?? s.venueId,
      seats: s => s.maxSeats,
      startType: s => this.startTypeLabel(s.startType),
    })
  );

  readonly filteredVenues = computed(() => {
    const venues = this.edition()?.venues ?? [];
    if (this.schedulePerspective() === 'venue' && this.buildingFilter() !== 'all') {
      return venues.filter(v => v.building === this.buildingFilter());
    }
    return venues;
  });

  readonly timelineDraft = computed<DraftBlock | null>(() => {
    const values = this.formValues();
    if (!values?.startTime || !values?.endTime) return null;

    return {
      start: values.startTime,
      end: values.endTime,
      sessionId: this.editingSessionId() ?? undefined,
      eventTitle: values.eventId ? this.eventTitle(values.eventId) : undefined,
      venueName: values.venueId ? this.venueById(values.venueId)?.name ?? values.venueId : undefined,
    };
  });

  readonly hasConflict = computed(() => {
    const values = this.formValues();
    if (!values?.venueId || !values?.startTime || !values?.endTime) return false;

    const start = new Date(values.startTime).getTime();
    const end = new Date(values.endTime).getTime();
    const editingId = this.editingSessionId();

    return this.filteredSessions().some(s => {
      if (s.venueId !== values.venueId) return false;
      if (editingId && s.sessionId === editingId) return false;

      const sStart = new Date(s.start).getTime();
      const sEnd = new Date(s.end).getTime();
      return start < sEnd && end > sStart;
    });
  });

  constructor() {
    effect(() => {
      const activeEdition = this.editionContext.activeEdition();
      if (!activeEdition) return;
      this.loadData(activeEdition.id);
    });
  }

  private openConfirm(data: ConfirmDialogData) {
    return this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, { data, width: '400px' })
      .afterClosed()
      .pipe(map(result => result === true));
  }

  setDay(value: string): void {
    this.day.set(value);
  }

  setSchedulePerspective(value: 'venue' | 'event'): void {
    this.schedulePerspective.set(value);
    this.buildingFilter.set('all');
    this.categoryFilter.set('all');
    this.searchText.set('');
  }

  setViewMode(value: 'timeline' | 'table'): void {
    this.viewMode.set(value);
  }

  setSort(key: SessionSortKey): void {
    this.sort.set(nextSort(this.sort(), key));
  }

  sortIcon(key: SessionSortKey): string {
    return sortIcon(this.sort(), key);
  }

  onTimelineSessionSelected(sessionId: string): void {
    const session = this.filteredSessions().find(s => s.sessionId === sessionId)
      ?? this.sessions().find(s => s.sessionId === sessionId);
    if (!session) return;
    this.startEdit(session);
  }

  startEdit(session: EditionSessionDto): void {
    this.editingSessionId.set(session.sessionId);
    this.form.patchValue({
      eventId: session.eventId,
      venueId: session.venueId,
      startTime: this.toLocalDateTimeInput(session.start),
      endTime: this.toLocalDateTimeInput(session.end),
      maxSeats: session.maxSeats,
      startType: session.startType,
      note: '',
    });
  }

  resetForm(): void {
    this.editingSessionId.set(null);
    this.form.reset({
      eventId: '',
      venueId: '',
      startTime: '',
      endTime: '',
      maxSeats: 20,
      startType: 'FixedTime',
      note: '',
    });
  }

  saveSession(): void {
    const values = this.form.getRawValue();
    if (this.form.invalid || this.saving()) return;

    const sessionId = this.editingSessionId();
    this.saving.set(true);

    const action$: Observable<unknown> = sessionId
      ? this.eventSvc.updateSession(
        values.eventId!,
        sessionId,
        values.venueId!,
        values.startTime!,
        values.endTime!,
        values.maxSeats!,
        values.startType!
      )
      : this.eventSvc.scheduleSession(
        values.eventId!,
        values.venueId!,
        values.startTime!,
        values.endTime!,
        values.maxSeats!,
        values.startType!
      );

    action$.subscribe({
      next: () => {
        this.saving.set(false);
        const editionId = this.editionContext.activeEdition()?.id;
        if (editionId) this.refreshSessions(editionId);
        this.resetForm();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, sessionId ? ERROR.saveSession : ERROR.scheduleSession));
      },
    });
  }

  deactivateEditingSession(): void {
    const sessionId = this.editingSessionId();
    const eventId = this.form.getRawValue().eventId;
    if (!sessionId || !eventId || this.saving()) return;

    this.openConfirm({
      title: this.PAGE.deleteSessionTitle,
      message: this.PAGE.deleteSessionMessage,
      confirmLabel: ACTION.delete,
    }).subscribe(confirmed => {
      if (!confirmed) return;
      this.saving.set(true);
      this.eventSvc.deactivateSession(eventId, sessionId).subscribe({
        next: () => {
          this.saving.set(false);
          const editionId = this.editionContext.activeEdition()?.id;
          if (editionId) this.refreshSessions(editionId);
          this.resetForm();
        },
        error: (err: unknown) => {
          this.saving.set(false);
          this.error.set(toErrorMessage(err, ERROR.deactivateSession));
        },
      });
    });
  }

  eventTitle(eventId: string): string {
    return this.events().find(e => e.id === eventId)?.title ?? eventId;
  }

  venueById(venueId: string): VenueDto | null {
    return this.edition()?.venues.find(v => v.id === venueId) ?? null;
  }

  startTypeLabel(value: string): string {
    return START_TYPE_LABEL[value] ?? value;
  }

  timelineTitle(): string {
    return this.schedulePerspective() === 'venue'
      ? this.PAGE.timelineTitleVenue
      : this.PAGE.timelineTitleEvent;
  }

  formatDayLabel(day: string): string {
    const date = this.parseDateLocal(day);
    return date.toLocaleDateString('sv-SE', {
      weekday: 'long',
      day: 'numeric',
      month: 'short',
    });
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      edition: this.conventionSvc.getEdition(editionId),
      sessions: this.eventSvc.getEditionSessions(editionId),
      eventSummaries: this.eventSvc.listEvents(editionId),
    }).pipe(
      switchMap(({ edition, sessions, eventSummaries }) => {
        if (!eventSummaries.length) {
          return of({ edition, sessions, events: [] as EventDto[] });
        }

        return forkJoin(eventSummaries.map(e => this.eventSvc.getEvent(e.id))).pipe(
          switchMap(events => of({ edition, sessions, events }))
        );
      })
    ).subscribe({
      next: ({ edition, sessions, events }) => {
        this.edition.set(edition);
        this.sessions.set(sessions);
        this.events.set(events);

        const dayOptions = this.dayOptions();
        const currentDay = this.day();
        if (!currentDay || !dayOptions.includes(currentDay)) {
          this.day.set(dayOptions[0] ?? null);
        }

        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set(ERROR.fetchEvents);
      },
    });
  }

  private refreshSessions(editionId: string): void {
    this.eventSvc.getEditionSessions(editionId).subscribe({
      next: sessions => this.sessions.set(sessions),
    });
  }

  private formatDateOnly(date: Date): string {
    const y = date.getFullYear();
    const m = `${date.getMonth() + 1}`.padStart(2, '0');
    const d = `${date.getDate()}`.padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  private parseDateLocal(value: string): Date {
    const [year, month, day] = value.split('T')[0].split('-').map(Number);
    return new Date(year, month - 1, day);
  }

  private addMinutesLocal(start: string, minutes: number): string {
    const date = new Date(start);
    date.setMinutes(date.getMinutes() + minutes);
    return this.toLocalDateTimeInput(date.toISOString());
  }

  private toLocalDateTimeInput(value: string): string {
    const date = new Date(value);
    const offset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
  }
}
