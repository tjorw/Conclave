import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import {
  ConventionService,
  EditionDto,
  EditionSessionDto,
  EventService,
  EventSummaryDto,
  VenueDto,
} from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { EventTimelineComponent } from '../../shared/event-timeline/event-timeline.component';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type SessionSortKey = 'event' | 'venue' | 'start' | 'end' | 'seats';

interface SessionRow {
  session: EditionSessionDto;
  eventSummary: EventSummaryDto | null;
  venueName: string;
}

@Component({
  selector: 'app-events',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    EventTimelineComponent,
  ],
  templateUrl: './events.component.html',
  styleUrl: './events.component.scss',
})
export class EventsComponent {
  private readonly eventSvc = inject(EventService);
  private readonly conventionSvc = inject(ConventionService);
  readonly editionContext = inject(EditionContextService);

  readonly edition = signal<EditionDto | null>(null);
  readonly sessions = signal<EditionSessionDto[]>([]);
  readonly eventSummaries = signal<EventSummaryDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly viewMode = signal<'timeline' | 'table'>('timeline');
  readonly day = signal<string | null>(null);
  readonly categoryFilter = signal<string>('all');
  readonly searchText = signal('');
  readonly sort = signal<SortState<SessionSortKey>>({ key: 'start', direction: 'asc' });
  readonly selectedSessionId = signal<string | null>(null);

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

  readonly categoryOptions = computed(() => {
    const cats = new Map<string, string>();
    for (const e of this.eventSummaries()) {
      const name = e.categoryName?.trim() || e.categoryId;
      cats.set(e.categoryId, name);
    }
    return [...cats.entries()]
      .map(([id, name]) => ({ id, name }))
      .sort((a, b) => a.name.localeCompare(b.name, 'sv-SE'));
  });

  private readonly eventMap = computed(() => {
    const map = new Map<string, EventSummaryDto>();
    for (const e of this.eventSummaries()) map.set(e.id, e);
    return map;
  });

  private readonly venueMap = computed(() => {
    const map = new Map<string, VenueDto>();
    for (const v of (this.edition()?.venues ?? [])) map.set(v.id, v);
    return map;
  });

  readonly filteredSessions = computed(() => {
    const selectedDay = this.day();
    const cat = this.categoryFilter();
    const search = this.searchText().trim().toLowerCase();

    return this.sessions().filter(s => {
      if (s.status !== 'Active') return false;
      if (selectedDay && !s.start.startsWith(selectedDay)) return false;
      if (cat !== 'all' && this.eventMap().get(s.eventId)?.categoryId !== cat) return false;
      if (search) {
        const venue = this.venueMap().get(s.venueId);
        const haystack = `${s.eventTitle} ${venue?.name ?? ''} ${venue?.building ?? ''}`.toLowerCase();
        if (!haystack.includes(search)) return false;
      }
      return true;
    });
  });

  readonly sessionRows = computed<SessionRow[]>(() =>
    sortBy(
      this.filteredSessions().map(s => ({
        session: s,
        eventSummary: this.eventMap().get(s.eventId) ?? null,
        venueName: this.venueMap().get(s.venueId)?.name ?? s.venueId,
      })),
      this.sort(),
      {
        event: r => r.session.eventTitle,
        venue: r => r.venueName,
        start: r => r.session.start,
        end: r => r.session.end,
        seats: r => r.session.maxSeats,
      }
    )
  );

  readonly selectedSession = computed(() => {
    const id = this.selectedSessionId();
    if (!id) return null;
    const s = this.sessions().find(x => x.sessionId === id) ?? null;
    if (!s) return null;
    return {
      session: s,
      eventSummary: this.eventMap().get(s.eventId) ?? null,
      venue: this.venueMap().get(s.venueId) ?? null,
    };
  });

  constructor() {
    effect(() => {
      const activeEdition = this.editionContext.activeEdition();
      if (!activeEdition) return;
      this.loadData(activeEdition.id);
    });
  }

  setViewMode(value: 'timeline' | 'table'): void {
    this.viewMode.set(value);
    this.selectedSessionId.set(null);
  }

  setDay(value: string): void {
    this.day.set(value);
    this.selectedSessionId.set(null);
  }

  onTimelineSessionSelected(sessionId: string): void {
    this.selectedSessionId.set(this.selectedSessionId() === sessionId ? null : sessionId);
  }

  onTableRowSelected(sessionId: string): void {
    this.selectedSessionId.set(this.selectedSessionId() === sessionId ? null : sessionId);
  }

  setSort(key: SessionSortKey): void {
    this.sort.set(nextSort(this.sort(), key));
  }

  sortIcon(key: SessionSortKey): string {
    return sortIcon(this.sort(), key);
  }

  onSearch(event: Event): void {
    this.searchText.set((event.target as HTMLInputElement).value);
    this.selectedSessionId.set(null);
  }

  formatDayLabel(day: string): string {
    return this.parseDateLocal(day).toLocaleDateString('sv-SE', {
      weekday: 'long', day: 'numeric', month: 'short',
    });
  }

  formatTime(iso: string): string {
    const d = new Date(iso);
    return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.selectedSessionId.set(null);

    forkJoin({
      edition: this.conventionSvc.getEdition(editionId),
      sessions: this.eventSvc.getEditionSessions(editionId),
      summaries: this.eventSvc.listEvents(editionId),
    }).subscribe({
      next: ({ edition, sessions, summaries }) => {
        this.edition.set(edition);
        this.sessions.set(sessions);
        this.eventSummaries.set(summaries);
        const dayOptions = this.dayOptions();
        if (!this.day() || !dayOptions.includes(this.day()!)) {
          this.day.set(dayOptions[0] ?? null);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Kunde inte hämta evenemang.');
        this.loading.set(false);
      },
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
}
