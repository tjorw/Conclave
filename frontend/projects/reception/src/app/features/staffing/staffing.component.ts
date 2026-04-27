import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import {
  STAFFING_STATUS_LABEL,
  StaffScheduleAreaDto,
  StaffScheduleDto,
  StaffScheduleShiftDto,
  StaffService,
} from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { StaffTimelineComponent } from '../../shared/staff-timeline/staff-timeline.component';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type StaffingSortKey = 'area' | 'station' | 'start' | 'end' | 'responsible' | 'staffing';

interface ShiftRow {
  areaId: string;
  areaName: string;
  stationName: string;
  responsibleName: string | null;
  shift: StaffScheduleShiftDto;
  staffingLabel: string;
}

@Component({
  selector: 'app-staffing',
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
    StaffTimelineComponent,
  ],
  templateUrl: './staffing.component.html',
  styleUrl: './staffing.component.scss',
})
export class StaffingComponent {
  private readonly staffSvc = inject(StaffService);
  readonly editionContext = inject(EditionContextService);

  readonly schedule = signal<StaffScheduleDto | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly viewMode = signal<'timeline' | 'table'>('timeline');
  readonly areaFilter = signal<string>('all');
  readonly searchText = signal('');
  readonly selectedShiftId = signal<string | null>(null);
  readonly sort = signal<SortState<StaffingSortKey>>({ key: 'start', direction: 'asc' });

  readonly areaOptions = computed(() =>
    (this.schedule()?.staffAreas ?? [])
      .map(a => ({ id: a.staffAreaId, name: a.name }))
      .sort((a, b) => a.name.localeCompare(b.name, 'sv-SE'))
  );

  private readonly allShiftRows = computed<ShiftRow[]>(() => {
    const areas: StaffScheduleAreaDto[] = this.schedule()?.staffAreas ?? [];
    return areas.flatMap(area =>
      area.stations.flatMap(station =>
        station.shifts.map(shift => ({
          areaId: area.staffAreaId,
          areaName: area.name,
          stationName: station.name,
          responsibleName: area.responsibleName,
          shift,
          staffingLabel: STAFFING_STATUS_LABEL[shift.staffingStatus] ?? shift.staffingStatus,
        }))
      )
    );
  });

  readonly filteredShiftRows = computed<ShiftRow[]>(() => {
    const area = this.areaFilter();
    const search = this.searchText().trim().toLowerCase();

    return this.allShiftRows().filter(row => {
      if (area !== 'all' && row.areaId !== area) return false;
      if (search) {
        const haystack = `${row.areaName} ${row.stationName} ${row.responsibleName ?? ''}`.toLowerCase();
        if (!haystack.includes(search)) return false;
      }
      return true;
    });
  });

  readonly sortedShiftRows = computed(() =>
    sortBy(this.filteredShiftRows(), this.sort(), {
      area: r => r.areaName,
      station: r => r.stationName,
      start: r => r.shift.start,
      end: r => r.shift.end,
      responsible: r => r.responsibleName ?? '',
      staffing: r => r.staffingLabel,
    })
  );

  readonly filteredSchedule = computed<StaffScheduleDto | null>(() => {
    const sched = this.schedule();
    if (!sched) return null;
    const area = this.areaFilter();
    const search = this.searchText().trim().toLowerCase();
    if (area === 'all' && !search) return sched;

    const filteredAreas = sched.staffAreas
      .filter(a => area === 'all' || a.staffAreaId === area)
      .map(a => ({
        ...a,
        stations: a.stations.filter(st => {
          if (!search) return true;
          return `${a.name} ${st.name} ${a.responsibleName ?? ''}`.toLowerCase().includes(search);
        }),
      }))
      .filter(a => a.stations.length > 0);

    return { ...sched, staffAreas: filteredAreas };
  });

  readonly selectedShift = computed(() => {
    const id = this.selectedShiftId();
    if (!id) return null;
    const row = this.allShiftRows().find(r => r.shift.shiftId === id) ?? null;
    return row;
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
    this.selectedShiftId.set(null);
  }

  onShiftSelected(shiftId: string): void {
    this.selectedShiftId.set(this.selectedShiftId() === shiftId ? null : shiftId);
  }

  setSort(key: StaffingSortKey): void {
    this.sort.set(nextSort(this.sort(), key));
  }

  sortIcon(key: StaffingSortKey): string {
    return sortIcon(this.sort(), key);
  }

  onSearch(event: Event): void {
    this.searchText.set((event.target as HTMLInputElement).value);
    this.selectedShiftId.set(null);
  }

  formatTime(iso: string): string {
    const d = new Date(iso);
    return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
  }

  staffingClass(status: string): string {
    switch (status) {
      case 'Unstaffed': return 'chip chip-red';
      case 'UnderMin':  return 'chip chip-orange';
      case 'Full':      return 'chip chip-blue';
      case 'OverMax':   return 'chip chip-purple';
      case 'Cancelled': return 'chip chip-grey';
      default:          return 'chip chip-green';
    }
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.selectedShiftId.set(null);

    this.staffSvc.getStaffSchedule(editionId).subscribe({
      next: sched => { this.schedule.set(sched); this.loading.set(false); },
      error: () => { this.error.set('Kunde inte hämta bemanningsschema.'); this.loading.set(false); },
    });
  }
}
