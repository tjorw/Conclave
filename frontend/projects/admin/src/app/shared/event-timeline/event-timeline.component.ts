import { Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { EditionSessionDto, VenueDto } from 'shared';
import { DraftBlock } from '../session-timeline/session-timeline.component';
import { SESSION_TIMELINE } from '../../labels/pages.labels';

interface EventRow {
  eventId: string;
  eventTitle: string;
}

interface EventBlock {
  session: EditionSessionDto;
  left: number;
  width: number;
  isOwn: boolean;
  hasConflict: boolean;
}

interface DraftVisualBlock {
  eventId: string;
  start: string;
  end: string;
  left: number;
  width: number;
  hasConflict: boolean;
}

const PX_PER_MIN = 2;
const DAY_START_HOUR = 8;
const DAY_END_HOUR = 23;
const EVENT_LABEL_WIDTH = 180;
const ROW_HEIGHT = 40;

@Component({
  selector: 'app-event-timeline',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './event-timeline.component.html',
  styleUrl: './event-timeline.component.scss',
})
export class EventTimelineComponent {
  readonly sessions = input.required<EditionSessionDto[]>();
  readonly currentEventId = input<string | null>(null);
  readonly editingVenueId = input<string | null>(null);
  readonly venues = input<VenueDto[]>([]);
  readonly draft = input<DraftBlock | null>(null);
  readonly editionStart = input<string | null>(null);
  readonly editionEnd = input<string | null>(null);
  readonly sessionSelected = output<string>();

  readonly EVENT_LABEL_WIDTH = EVENT_LABEL_WIDTH;
  readonly ROW_HEIGHT = ROW_HEIGHT;
  readonly TL = SESSION_TIMELINE;

  private readonly venueNameMap = computed(() => {
    const map = new Map<string, string>();
    for (const venue of this.venues()) {
      map.set(venue.id, venue.name);
    }
    return map;
  });

  readonly activeSession = computed(() => {
    const d = this.draft();
    if (!d?.sessionId) return null;
    return this.sessions().find(s => s.sessionId === d.sessionId) ?? null;
  });

  readonly activeDraftTitle = computed(() => {
    const d = this.draft();
    const draftTitle = d?.venueName?.trim();
    if (draftTitle) return draftTitle;

    const activeVenueId = this.activeSession()?.venueId ?? this.editingVenueId();
    if (!activeVenueId) return null;

    return this.venueName(activeVenueId).trim() || null;
  });

  readonly draftLabel = computed(() => {
    const title = this.activeDraftTitle();
    const prefix = this.hasConflict() ? this.TL.conflict : this.TL.inProgress;
    return title ? title : prefix;
  });

  readonly draftTooltip = computed(() => {
    const d = this.draft();
    if (!d) return this.TL.draftTitle;

    const title = this.activeDraftTitle();
    const time = `${this.formatTime(d.start)}-${this.formatTime(d.end)}`;
    return title ? `${title} ${time}` : time;
  });

  private readonly timeRange = computed(() => {
    const start = this.editionStart();
    const end = this.editionEnd();
    const from = start ? this.parseDateLocal(start) : new Date();
    const to = end ? this.parseDateLocal(end) : new Date(from.getTime() + 86400000 * 2);
    from.setHours(DAY_START_HOUR, 0, 0, 0);
    to.setHours(DAY_END_HOUR, 0, 0, 0);
    return { from, to };
  });

  readonly totalWidth = computed(() =>
    (this.timeRange().to.getTime() - this.timeRange().from.getTime()) / 60000 * PX_PER_MIN
  );

  readonly daySegments = computed(() => {
    const { from, to } = this.timeRange();
    const segments: { label: string; left: number; width: number }[] = [];

    let dayCursor = new Date(from);
    dayCursor.setHours(0, 0, 0, 0);

    while (dayCursor < to) {
      const nextDay = new Date(dayCursor);
      nextDay.setDate(nextDay.getDate() + 1);

      const segStart = dayCursor < from ? from : dayCursor;
      const segEnd = nextDay > to ? to : nextDay;

      const left = (segStart.getTime() - from.getTime()) / 60000 * PX_PER_MIN;
      const width = (segEnd.getTime() - segStart.getTime()) / 60000 * PX_PER_MIN;

      segments.push({
        label: dayCursor.toLocaleDateString('sv-SE', {
          weekday: 'long',
          day: 'numeric',
          month: 'short',
        }),
        left,
        width,
      });

      dayCursor = nextDay;
    }

    return segments;
  });

  readonly hourMarkers = computed(() => {
    const { from, to } = this.timeRange();
    const markers: { label: string; left: number }[] = [];

    const cursor = new Date(from);
    if (cursor.getMinutes() !== 0) cursor.setHours(cursor.getHours() + 1, 0, 0, 0);

    while (cursor <= to) {
      const left = (cursor.getTime() - from.getTime()) / 60000 * PX_PER_MIN;
      markers.push({ label: `${cursor.getHours()}:00`, left });
      cursor.setHours(cursor.getHours() + 1);
    }

    return markers;
  });

  readonly visibleEvents = computed(() => {
    const rows = new Map<string, EventRow>();
    for (const s of this.sessions()) {
      if (s.status !== 'Active') continue;
      rows.set(s.eventId, { eventId: s.eventId, eventTitle: s.eventTitle });
    }
    return [...rows.values()].sort((a, b) => a.eventTitle.localeCompare(b.eventTitle, 'sv-SE'));
  });


  // Alla sessioner som överlappar med någon annan session i samma lokal
  private readonly allConflictingSessionIds = computed((): Set<string> => {
    const draftSessionId = this.draft()?.sessionId;
    const sessions = this.sessions().filter(s => s.status === 'Active' && s.sessionId !== draftSessionId);
    const conflicts = new Set<string>();
    // Grupp per lokal
    const byVenue = new Map<string, EditionSessionDto[]>();
    for (const s of sessions) {
      if (!byVenue.has(s.venueId)) byVenue.set(s.venueId, []);
      byVenue.get(s.venueId)!.push(s);
    }
    // För varje lokal, hitta överlappande sessioner
    for (const venueSessions of byVenue.values()) {
      for (let i = 0; i < venueSessions.length; i++) {
        const a = venueSessions[i];
        const aStart = new Date(a.start).getTime();
        const aEnd = new Date(a.end).getTime();
        for (let j = i + 1; j < venueSessions.length; j++) {
          const b = venueSessions[j];
          const bStart = new Date(b.start).getTime();
          const bEnd = new Date(b.end).getTime();
          if (aStart < bEnd && aEnd > bStart) {
            conflicts.add(a.sessionId);
            conflicts.add(b.sessionId);
          }
        }
      }
    }
    return conflicts;
  });

  // Sessioner som överlappar med draft-blocket (för att markera draft-konflikt separat)
  private readonly draftConflictingSessionIds = computed((): Set<string> => {
    const d = this.draft();
    const editVenueId = this.editingVenueId();
    if (!d?.start || !d?.end || !editVenueId) return new Set();

    const dStart = new Date(d.start).getTime();
    const dEnd = new Date(d.end).getTime();
    const ids = new Set<string>();

    this.sessions()
      .filter(s => s.venueId === editVenueId && s.status === 'Active' && s.sessionId !== d.sessionId)
      .forEach(s => {
        if (dStart < new Date(s.end).getTime() && dEnd > new Date(s.start).getTime()) {
          ids.add(s.sessionId);
        }
      });

    return ids;
  });

  readonly hasConflict = computed(() => this.draftConflictingSessionIds().size > 0);

  private readonly blocksByEvent = computed(() => {
    const { from } = this.timeRange();
    const allConflicts = this.allConflictingSessionIds();
    const draftConflicts = this.draftConflictingSessionIds();
    const selectedVenueId = this.editingVenueId();
    const draftSessionId = this.draft()?.sessionId;
    const map = new Map<string, EventBlock[]>();

    for (const s of this.sessions()) {
      if (s.status !== 'Active') continue;
      if (draftSessionId && s.sessionId === draftSessionId) continue;

      const left = (new Date(s.start).getTime() - from.getTime()) / 60000 * PX_PER_MIN;
      const width = Math.max(
        (new Date(s.end).getTime() - new Date(s.start).getTime()) / 60000 * PX_PER_MIN,
        20
      );

      const block: EventBlock = {
        session: s,
        left,
        width,
        isOwn: !!selectedVenueId && s.venueId === selectedVenueId,
        hasConflict: allConflicts.has(s.sessionId) || draftConflicts.has(s.sessionId),
      };

      if (!map.has(s.eventId)) map.set(s.eventId, []);
      map.get(s.eventId)!.push(block);
    }

    return map;
  });

  readonly draftBlock = computed((): DraftVisualBlock | null => {
    const d = this.draft();
    const currentEventId = this.currentEventId();
    if (!d?.start || !d?.end || !currentEventId) return null;

    const { from } = this.timeRange();
    const left = (new Date(d.start).getTime() - from.getTime()) / 60000 * PX_PER_MIN;
    const width = Math.max(
      (new Date(d.end).getTime() - new Date(d.start).getTime()) / 60000 * PX_PER_MIN,
      20
    );

    return { eventId: currentEventId, start: d.start, end: d.end, left, width, hasConflict: this.hasConflict() };
  });

  getBlocksForEvent(eventId: string): EventBlock[] {
    return this.blocksByEvent().get(eventId) ?? [];
  }

  formatTime(iso: string): string {
    const d = new Date(iso);
    return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
  }

  venueName(venueId: string): string {
    return this.venueNameMap().get(venueId) ?? venueId;
  }

  selectSession(sessionId: string): void {
    this.sessionSelected.emit(sessionId);
  }

  private parseDateLocal(s: string): Date {
    const parts = s.split('T')[0].split('-').map(Number);
    return new Date(parts[0], parts[1] - 1, parts[2]);
  }
}
