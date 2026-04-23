import { Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import {
  EditionScheduleDayDto,
  STAFFING_STATUS_LABEL,
  StaffScheduleAreaDto,
  StaffScheduleDto,
  StaffScheduleShiftDto,
} from 'shared';
import { STAFF_TIMELINE } from '../../labels/pages.labels';

interface StaffShiftBlock {
  shift: StaffScheduleShiftDto;
  left: number;
  width: number;
  staffingClass: string;
  staffingLabel: string;
}

interface StaffStationRow {
  areaId: string;
  areaName: string;
  responsibleName: string | null;
  stationId: string;
  stationName: string;
  stationDescription: string | null;
  shifts: StaffShiftBlock[];
}

const PX_PER_MIN = 2;
const DEFAULT_DAY_START = '00:00';
const DEFAULT_DAY_END = '23:59';
const STATION_LABEL_WIDTH = 220;
const ROW_HEIGHT = 56;

@Component({
  selector: 'app-staff-timeline',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './staff-timeline.component.html',
  styleUrl: './staff-timeline.component.scss',
})
export class StaffTimelineComponent {
  readonly schedule = input<StaffScheduleDto | null>(null);
  readonly editionStart = input<string | null>(null);
  readonly editionEnd = input<string | null>(null);
  readonly selectedShiftId = input<string | null>(null);
  readonly shiftSelected = output<string>();

  readonly STATION_LABEL_WIDTH = STATION_LABEL_WIDTH;
  readonly ROW_HEIGHT = ROW_HEIGHT;
  readonly TL = STAFF_TIMELINE;

  private readonly scheduleDays = computed<EditionScheduleDayDto[]>(() =>
    this.schedule()?.scheduleDays ?? []
  );

  private readonly areas = computed<StaffScheduleAreaDto[]>(() =>
    this.schedule()?.staffAreas ?? []
  );

  private readonly allShifts = computed<StaffScheduleShiftDto[]>(() =>
    this.areas().flatMap(area => area.stations.flatMap(station => station.shifts))
  );

  private readonly timeRange = computed(() => {
    const fallbackStart = this.allShifts()[0]?.start ?? new Date().toISOString();
    const fallbackEnd = this.allShifts().at(-1)?.end ?? fallbackStart;

    const startValue = this.editionStart() ?? fallbackStart;
    const endValue = this.editionEnd() ?? fallbackEnd;

    const from = this.dateTimeForScheduleBoundary(startValue, 'start');
    const to = this.dateTimeForScheduleBoundary(endValue, 'end');
    return from <= to ? { from, to } : { from: to, to: from };
  });

  readonly totalWidth = computed(() =>
    Math.max(
      (this.timeRange().to.getTime() - this.timeRange().from.getTime()) / 60000 * PX_PER_MIN,
      240
    )
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
        label: dayCursor.toLocaleDateString('sv-SE', {
          weekday: 'long',
          day: 'numeric',
          month: 'short',
        }),
        left: (segStart.getTime() - from.getTime()) / 60000 * PX_PER_MIN,
        width: Math.max((segEnd.getTime() - segStart.getTime()) / 60000 * PX_PER_MIN, 1),
      });

      dayCursor = nextDay;
    }

    return segments;
  });

  readonly hourMarkers = computed(() => {
    const { from, to } = this.timeRange();
    const markers: { label: string; left: number }[] = [];
    const cursor = new Date(from);

    if (cursor.getMinutes() !== 0) {
      cursor.setHours(cursor.getHours() + 1, 0, 0, 0);
    }

    while (cursor <= to) {
      markers.push({
        label: `${cursor.getHours().toString().padStart(2, '0')}:00`,
        left: (cursor.getTime() - from.getTime()) / 60000 * PX_PER_MIN,
      });
      cursor.setHours(cursor.getHours() + 1);
    }

    return markers;
  });

  readonly rows = computed<StaffStationRow[]>(() => {
    const { from } = this.timeRange();

    return this.areas().flatMap(area =>
      area.stations.map(station => ({
        areaId: area.staffAreaId,
        areaName: area.name,
        responsibleName: area.responsibleName,
        stationId: station.stationId,
        stationName: station.name,
        stationDescription: station.description,
        shifts: station.shifts.map(shift => ({
          shift,
          left: (new Date(shift.start).getTime() - from.getTime()) / 60000 * PX_PER_MIN,
          width: Math.max(
            (new Date(shift.end).getTime() - new Date(shift.start).getTime()) / 60000 * PX_PER_MIN,
            20
          ),
          staffingClass: this.staffingClass(shift.staffingStatus),
          staffingLabel: this.staffingStatusLabel(shift.staffingStatus),
        })),
      }))
    );
  });

  readonly hasRows = computed(() => this.rows().length > 0);

  isFirstRowInArea(index: number): boolean {
    const rows = this.rows();
    return index === 0 || rows[index - 1]?.areaId !== rows[index]?.areaId;
  }

  formatTime(iso: string): string {
    const d = new Date(iso);
    return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
  }

  staffingStatusLabel(status: string): string {
    return STAFFING_STATUS_LABEL[status] ?? status;
  }

  selectShift(shiftId: string): void {
    this.shiftSelected.emit(shiftId);
  }

  private staffingClass(status: string): string {
    switch (status) {
      case 'Unstaffed':
        return 'is-unstaffed';
      case 'UnderMin':
        return 'is-under-min';
      case 'Full':
        return 'is-full';
      case 'OverMax':
        return 'is-over-max';
      case 'Cancelled':
        return 'is-cancelled';
      default:
        return 'is-within';
    }
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
