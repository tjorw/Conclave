import { Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { EditionScheduleDayDto, EditionSessionDto, VenueDto } from 'shared';

interface EventRow {
  eventId: string;
  eventTitle: string;
}

interface SessionBlock {
  session: EditionSessionDto;
  left: number;
  width: number;
}

const PX_PER_MIN = 2;
const DEFAULT_DAY_START = '00:00';
const DEFAULT_DAY_END = '23:59';
const EVENT_LABEL_WIDTH = 200;
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
  readonly venues = input<VenueDto[]>([]);
  readonly editionStart = input<string | null>(null);
  readonly editionEnd = input<string | null>(null);
  readonly scheduleDays = input<EditionScheduleDayDto[]>([]);
  readonly sessionSelected = output<string>();

  readonly EVENT_LABEL_WIDTH = EVENT_LABEL_WIDTH;
  readonly ROW_HEIGHT = ROW_HEIGHT;

  private readonly venueNameMap = computed(() => {
    const map = new Map<string, string>();
    for (const venue of this.venues()) {
      map.set(venue.id, venue.name);
    }
    return map;
  });

  private readonly timeRange = computed(() => {
    const start = this.editionStart();
    const end = this.editionEnd();
    const from = start ? this.dateTimeForScheduleBoundary(start, 'start') : new Date();
    const to = end ? this.dateTimeForScheduleBoundary(end, 'end') : new Date(from.getTime() + 86400000 * 2);
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
      segments.push({
        label: dayCursor.toLocaleDateString('sv-SE', { weekday: 'long', day: 'numeric', month: 'short' }),
        left: (segStart.getTime() - from.getTime()) / 60000 * PX_PER_MIN,
        width: (segEnd.getTime() - segStart.getTime()) / 60000 * PX_PER_MIN,
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
      markers.push({
        label: `${cursor.getHours().toString().padStart(2, '0')}:00`,
        left: (cursor.getTime() - from.getTime()) / 60000 * PX_PER_MIN,
      });
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

  private readonly blocksByEvent = computed(() => {
    const { from } = this.timeRange();
    const map = new Map<string, SessionBlock[]>();
    for (const s of this.sessions()) {
      if (s.status !== 'Active') continue;
      const left = (new Date(s.start).getTime() - from.getTime()) / 60000 * PX_PER_MIN;
      const width = Math.max(
        (new Date(s.end).getTime() - new Date(s.start).getTime()) / 60000 * PX_PER_MIN,
        20
      );
      if (!map.has(s.eventId)) map.set(s.eventId, []);
      map.get(s.eventId)!.push({ session: s, left, width });
    }
    return map;
  });

  getBlocksForEvent(eventId: string): SessionBlock[] {
    return this.blocksByEvent().get(eventId) ?? [];
  }

  venueName(venueId: string): string {
    return this.venueNameMap().get(venueId) ?? venueId;
  }

  formatTime(iso: string): string {
    const d = new Date(iso);
    return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
  }

  selectSession(sessionId: string): void {
    this.sessionSelected.emit(sessionId);
  }

  private parseDateLocal(s: string): Date {
    const parts = s.split('T')[0].split('-').map(Number);
    return new Date(parts[0], parts[1] - 1, parts[2]);
  }

  private dateTimeForScheduleBoundary(value: string, boundary: 'start' | 'end'): Date {
    const datePart = value.split('T')[0];
    const date = this.parseDateLocal(datePart);
    const scheduleDay = this.scheduleDays().find(d => d.date.startsWith(datePart));
    const time = boundary === 'start'
      ? scheduleDay?.startTime ?? DEFAULT_DAY_START
      : scheduleDay?.endTime ?? DEFAULT_DAY_END;
    const [hours, minutes] = time.split(':').map(Number);
    date.setHours(hours, minutes ?? 0, 0, 0);
    return date;
  }
}
