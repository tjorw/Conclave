import { Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { EditionSessionDto, VenueDto } from 'shared';
import { SESSION_TIMELINE } from '../../labels/pages.labels';

export interface DraftBlock {
  start: string;
  end: string;
  sessionId?: string;
}

interface SessionBlock {
  session: EditionSessionDto;
  left: number;
  width: number;
  isOwn: boolean;
  hasConflict: boolean;
}

interface DraftVisualBlock {
  venueId: string;
  left: number;
  width: number;
  hasConflict: boolean;
}

const PX_PER_MIN        = 2;
const DAY_START_HOUR    = 8;
const DAY_END_HOUR      = 23;
const VENUE_LABEL_WIDTH = 140;
const ROW_HEIGHT        = 40;

@Component({
  selector: 'app-session-timeline',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './session-timeline.component.html',
  styleUrl: './session-timeline.component.scss',
})
export class SessionTimelineComponent {
  readonly sessions       = input.required<EditionSessionDto[]>();
  readonly currentEventId = input.required<string>();
  readonly editingVenueId = input<string | null>(null);
  readonly venues         = input<VenueDto[]>([]);
  readonly draft          = input<DraftBlock | null>(null);
  readonly editionStart   = input<string | null>(null);
  readonly editionEnd     = input<string | null>(null);

  readonly VENUE_LABEL_WIDTH = VENUE_LABEL_WIDTH;
  readonly ROW_HEIGHT        = ROW_HEIGHT;
  readonly TL                = SESSION_TIMELINE;

  // ── Time range ───────────────────────────────────────────────────────────

  private readonly timeRange = computed(() => {
    const start = this.editionStart();
    const end   = this.editionEnd();
    const from = start ? this.parseDateLocal(start) : new Date();
    const to   = end   ? this.parseDateLocal(end)   : new Date(from.getTime() + 86400000 * 2);
    from.setHours(DAY_START_HOUR, 0, 0, 0);
    to.setHours(DAY_END_HOUR, 0, 0, 0);
    return { from, to };
  });

  readonly totalWidth = computed(() =>
    (this.timeRange().to.getTime() - this.timeRange().from.getTime()) / 60000 * PX_PER_MIN
  );

  // Day header segments (one per calendar day)
  readonly daySegments = computed(() => {
    const { from, to } = this.timeRange();
    const segments: { label: string; left: number; width: number }[] = [];

    let dayCursor = new Date(from);
    dayCursor.setHours(0, 0, 0, 0);

    while (dayCursor < to) {
      const nextDay = new Date(dayCursor);
      nextDay.setDate(nextDay.getDate() + 1);

      const segStart = dayCursor < from ? from : dayCursor;
      const segEnd   = nextDay  > to   ? to   : nextDay;

      const left  = (segStart.getTime() - from.getTime()) / 60000 * PX_PER_MIN;
      const width = (segEnd.getTime() - segStart.getTime()) / 60000 * PX_PER_MIN;

      segments.push({
        label: dayCursor.toLocaleDateString('sv-SE', {
          weekday: 'long', day: 'numeric', month: 'short',
        }),
        left,
        width,
      });

      dayCursor = nextDay;
    }
    return segments;
  });

  // Hour tick marks
  readonly hourMarkers = computed(() => {
    const { from, to } = this.timeRange();
    const markers: { label: string; left: number }[] = [];

    const cursor = new Date(from);
    // Round up to next full hour
    if (cursor.getMinutes() !== 0) cursor.setHours(cursor.getHours() + 1, 0, 0, 0);

    while (cursor <= to) {
      const left = (cursor.getTime() - from.getTime()) / 60000 * PX_PER_MIN;
      markers.push({ label: `${cursor.getHours()}:00`, left });
      cursor.setHours(cursor.getHours() + 1);
    }
    return markers;
  });

  // ── Venues & sessions ────────────────────────────────────────────────────

  readonly visibleVenues = computed(() => {
    // Lokaler där det aktuella evenemanget har aktiva sessioner
    const ownVenueIds = new Set(
      this.sessions()
        .filter(s => s.eventId === this.currentEventId() && s.status === 'Active')
        .map(s => s.venueId)
    );
    // Plus lokalen som just nu redigeras/läggs till i formuläret
    const editingId = this.editingVenueId();
    if (editingId) ownVenueIds.add(editingId);

    return this.venues().filter(v => ownVenueIds.has(v.id));
  });

  // Conflicts: set of sessionIds that overlap with the draft block
  private readonly conflictingSessionIds = computed((): Set<string> => {
    const d = this.draft();
    const editVenueId = this.editingVenueId();
    if (!d?.start || !d?.end || !editVenueId) return new Set();

    const dStart = new Date(d.start).getTime();
    const dEnd   = new Date(d.end).getTime();
    const ids    = new Set<string>();

    this.sessions()
      .filter(s => s.venueId === editVenueId && s.status === 'Active' && s.sessionId !== d.sessionId)
      .forEach(s => {
        if (dStart < new Date(s.end).getTime() && dEnd > new Date(s.start).getTime()) {
          ids.add(s.sessionId);
        }
      });
    return ids;
  });

  readonly hasConflict = computed(() => this.conflictingSessionIds().size > 0);

  // All session blocks, grouped by venueId
  private readonly sessionBlocksByVenue = computed(() => {
    const { from } = this.timeRange();
    const conflicts = this.conflictingSessionIds();
    const map = new Map<string, SessionBlock[]>();

    for (const s of this.sessions()) {
      if (s.status !== 'Active') continue;
      const left  = (new Date(s.start).getTime() - from.getTime()) / 60000 * PX_PER_MIN;
      const width = Math.max(
        (new Date(s.end).getTime() - new Date(s.start).getTime()) / 60000 * PX_PER_MIN,
        20
      );
      const block: SessionBlock = {
        session: s,
        left,
        width,
        isOwn:       s.eventId === this.currentEventId(),
        hasConflict: conflicts.has(s.sessionId),
      };
      if (!map.has(s.venueId)) map.set(s.venueId, []);
      map.get(s.venueId)!.push(block);
    }
    return map;
  });

  // Draft block (if venue is selected and times are filled in)
  readonly draftBlock = computed((): DraftVisualBlock | null => {
    const d          = this.draft();
    const editVenueId = this.editingVenueId();
    if (!d?.start || !d?.end || !editVenueId) return null;

    const { from } = this.timeRange();
    const left  = (new Date(d.start).getTime() - from.getTime()) / 60000 * PX_PER_MIN;
    const width = Math.max(
      (new Date(d.end).getTime() - new Date(d.start).getTime()) / 60000 * PX_PER_MIN,
      20
    );
    return { venueId: editVenueId, left, width, hasConflict: this.hasConflict() };
  });

  // ── Helpers ─────────────────────────────────────────────────────────────

  getBlocksForVenue(venueId: string): SessionBlock[] {
    return this.sessionBlocksByVenue().get(venueId) ?? [];
  }

  formatTime(iso: string): string {
    const d = new Date(iso);
    return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
  }

  private parseDateLocal(s: string): Date {
    const parts = s.split('T')[0].split('-').map(Number);
    return new Date(parts[0], parts[1] - 1, parts[2]);
  }
}
