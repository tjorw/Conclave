import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  ConventionService,
  DateTimeRangeComponent,
  EditionStaffMemberDto,
  ShiftDto,
  STAFF_APPLICATION_STATUS_LABEL,
  StaffApplicationSummaryDto,
  STAFFING_STATUS_LABEL,
  StaffScheduleDto,
  StaffScheduleShiftDto,
  StaffService,
  toErrorMessage,
} from 'shared';
import { ERROR } from '../../labels/errors.labels';
import { STAFFING_OVERVIEW } from '../../labels/pages.labels';
import { EditionContextService } from '../../services/edition-context.service';
import { StaffTimelineComponent } from '../../shared/staff-timeline/staff-timeline.component';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type StaffingSortKey = 'area' | 'station' | 'start' | 'end' | 'responsible' | 'staffing' | 'status';
type ViewMode = 'timeline' | 'table';
type SelectedShiftDetailView = 'assignments' | 'personTimeline';

const PERSON_TIMELINE_PX_PER_MIN = 2;
const PERSON_TIMELINE_MIN_WIDTH = 320;

interface StaffingTableRow {
  areaId: string;
  areaName: string;
  stationId: string;
  stationName: string;
  shift: StaffScheduleShiftDto;
}

interface StaffCandidate {
  personId: string;
  personName: string;
  warnings: string[];
  canAssign: boolean;
}

interface PersonTimelineShiftBlock {
  shiftId: string;
  areaName: string;
  stationName: string;
  start: string;
  end: string;
  left: number;
  width: number;
  isSelected: boolean;
}

interface PersonTimelineRow {
  personId: string;
  personName: string;
  shifts: PersonTimelineShiftBlock[];
}

@Component({
  selector: 'app-staff-areas',
  standalone: true,
  imports: [
    DatePipe,
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
    DateTimeRangeComponent,
    StaffTimelineComponent,
  ],
  templateUrl: './staff-areas.component.html',
  styleUrl: './staff-areas.component.scss',
})
export class StaffAreasComponent {
  private readonly staffSvc = inject(StaffService);
  private readonly conventionSvc = inject(ConventionService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  readonly editionCtx = inject(EditionContextService);

  readonly PAGE = STAFFING_OVERVIEW;

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly schedule = signal<StaffScheduleDto | null>(null);
  readonly staff = signal<EditionStaffMemberDto[]>([]);
  readonly applications = signal<StaffApplicationSummaryDto[]>([]);
  readonly shiftDetails = signal<Record<string, ShiftDto>>({});

  readonly selectedDay = signal<string | null>(null);
  readonly areaFilter = signal<string>('all');
  readonly stationFilter = signal<string>('all');
  readonly staffingFilter = signal<string>('all');
  readonly viewMode = signal<ViewMode>('timeline');
  readonly selectedShiftId = signal<string | null>(null);
  readonly selectedShiftDetail = signal<ShiftDto | null>(null);
  readonly selectedShiftDetailView = signal<SelectedShiftDetailView>('assignments');
  readonly shiftLoading = signal(false);
  readonly creatingShift = signal(false);
  readonly editingShift = signal(false);
  readonly sort = signal<SortState<StaffingSortKey>>({ key: 'start', direction: 'asc' });

  readonly createShiftForm = this.fb.group({
    stationId: ['', Validators.required],
    responsibleId: ['', Validators.required],
    startTime: ['', Validators.required],
    endTime: ['', Validators.required],
    minPersons: [1, [Validators.required, Validators.min(1)]],
    maxPersons: [4, [Validators.required, Validators.min(1)]],
  });

  readonly assignForm = this.fb.group({
    personId: ['', Validators.required],
  });

  readonly editShiftForm = this.fb.group({
    stationId: ['', Validators.required],
    responsibleId: ['', Validators.required],
    startTime: ['', Validators.required],
    endTime: ['', Validators.required],
    minPersons: [1, [Validators.required, Validators.min(1)]],
    maxPersons: [4, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    effect(() => {
      const summary = this.editionCtx.activeEdition();
      if (!summary) {
        this.loading.set(false);
        this.schedule.set(null);
        return;
      }

      this.loadSchedule(summary.id);
      this.loadStaff(summary.id);
      this.loadApplications(summary.id);
    });
  }

  readonly dayOptions = computed(() =>
    this.schedule()?.scheduleDays.map(day => day.date) ?? []
  );

  readonly areaOptions = computed(() =>
    this.schedule()?.staffAreas.map(area => ({ id: area.staffAreaId, name: area.name })) ?? []
  );

  readonly stationOptions = computed(() => {
    const areaId = this.areaFilter();
    const areas = this.schedule()?.staffAreas ?? [];

    return areas
      .filter(area => areaId === 'all' || area.staffAreaId === areaId)
      .flatMap(area =>
        area.stations.map(station => ({
          id: station.stationId,
          name: station.name,
        }))
      );
  });

  readonly allStationOptions = computed(() =>
    this.schedule()?.staffAreas.flatMap(area =>
      area.stations.map(station => ({
        id: station.stationId,
        name: `${area.name} · ${station.name}`,
      }))
    ) ?? []
  );

  readonly staffingOptions = computed(() =>
    Object.entries(STAFFING_STATUS_LABEL).map(([id, label]) => ({ id, label }))
  );

  readonly canCreateShift = computed(() => this.stationOptions().length > 0);
  readonly canEditSelectedShift = computed(() => this.selectedShiftDetail()?.status === 'Planned');

  readonly filteredSchedule = computed<StaffScheduleDto | null>(() => {
    const schedule = this.schedule();
    if (!schedule) {
      return null;
    }

    const day = this.selectedDay();
    const areaId = this.areaFilter();
    const stationId = this.stationFilter();
    const staffingStatus = this.staffingFilter();

    const filteredAreas = schedule.staffAreas
      .filter(area => areaId === 'all' || area.staffAreaId === areaId)
      .map(area => ({
        ...area,
        stations: area.stations
          .filter(station => stationId === 'all' || station.stationId === stationId)
          .map(station => ({
            ...station,
            shifts: station.shifts.filter(shift =>
              (!day || this.shiftOverlapsDay(shift, day)) &&
              (staffingStatus === 'all' || shift.staffingStatus === staffingStatus)
            ),
          }))
          .filter(station => station.shifts.length > 0),
      }))
      .filter(area => area.stations.length > 0);

    return {
      ...schedule,
      staffAreas: filteredAreas,
    };
  });

  readonly tableRows = computed<StaffingTableRow[]>(() => {
    const rows = this.filteredSchedule()?.staffAreas.flatMap(area =>
      area.stations.flatMap(station =>
        station.shifts.map(shift => ({
          areaId: area.staffAreaId,
          areaName: area.name,
          stationId: station.stationId,
          stationName: station.name,
          shift,
        }))
      )
    ) ?? [];

    return sortBy(rows, this.sort(), {
      area: row => row.areaName,
      station: row => row.stationName,
      start: row => row.shift.start,
      end: row => row.shift.end,
      responsible: row => row.shift.responsibleName,
      staffing: row => row.shift.activeAssignmentCount,
      status: row => this.staffingStatusLabel(row.shift.staffingStatus),
    });
  });

  readonly allRows = computed<StaffingTableRow[]>(() =>
    this.schedule()?.staffAreas.flatMap(area =>
      area.stations.flatMap(station =>
        station.shifts.map(shift => ({
          areaId: area.staffAreaId,
          areaName: area.name,
          stationId: station.stationId,
          stationName: station.name,
          shift,
        }))
      )
    ) ?? []
  );

  readonly selectedShift = computed(() =>
    this.allRows().find(row => row.shift.shiftId === this.selectedShiftId()) ?? null
  );

  readonly selectedCandidate = computed(() =>
    this.assignCandidates().find(candidate => candidate.personId === this.assignForm.controls.personId.value) ?? null
  );

  readonly selectedCandidateApplication = computed(() => {
    const personId = this.selectedCandidate()?.personId;
    return personId ? this.applicationForPerson(personId) ?? null : null;
  });

  readonly assignCandidates = computed<StaffCandidate[]>(() => {
    const selected = this.selectedShift();
    const shift = this.selectedShiftDetail();
    if (!selected || !shift) {
      return [];
    }

    return this.staff()
      .filter(person => this.isApprovedStaffCandidate(person.personId))
      .map(person => {
        const warnings = this.personWarnings(person.personId, selected);
        const alreadyAssigned = shift.assignments.some(assignment =>
          assignment.personId === person.personId && this.isActiveAssignment(assignment.status)
        );

        if (alreadyAssigned) {
          warnings.unshift(this.PAGE.assignmentAlreadyExistsWarning);
        }

        return {
          personId: person.personId,
          personName: person.personName,
          warnings,
          canAssign: !alreadyAssigned,
        };
      })
      .sort((left, right) =>
        left.warnings.length - right.warnings.length ||
        left.personName.localeCompare(right.personName, 'sv')
      );
  });

  readonly assignmentRows = computed(() => {
    const selected = this.selectedShift();
    const detail = this.selectedShiftDetail();
    if (!selected || !detail) {
      return [];
    }

    return detail.assignments.map(assignment => ({
      assignment,
      warnings: this.personWarnings(assignment.personId, selected, detail.id),
    }));
  });

  readonly personTimelineRange = computed(() => {
    const selected = this.selectedShift();
    if (!selected) {
      return null;
    }

    const day = selected.shift.start.slice(0, 10);
    return {
      from: this.scheduleBoundaryDate(day, 'start'),
      to: this.scheduleBoundaryDate(day, 'end'),
    };
  });

  readonly personTimelineWidth = computed(() => {
    const range = this.personTimelineRange();
    if (!range) {
      return PERSON_TIMELINE_MIN_WIDTH;
    }

    return Math.max(
      (range.to.getTime() - range.from.getTime()) / 60000 * PERSON_TIMELINE_PX_PER_MIN,
      PERSON_TIMELINE_MIN_WIDTH
    );
  });

  readonly personTimelineHourMarkers = computed(() => {
    const range = this.personTimelineRange();
    if (!range) {
      return [];
    }

    const markers: { label: string; left: number }[] = [];
    const cursor = new Date(range.from);

    if (cursor.getMinutes() !== 0) {
      cursor.setHours(cursor.getHours() + 1, 0, 0, 0);
    }

    while (cursor <= range.to) {
      markers.push({
        label: `${cursor.getHours().toString().padStart(2, '0')}:00`,
        left: (cursor.getTime() - range.from.getTime()) / 60000 * PERSON_TIMELINE_PX_PER_MIN,
      });
      cursor.setHours(cursor.getHours() + 1);
    }

    return markers;
  });

  readonly personTimelineRows = computed<PersonTimelineRow[]>(() => {
    const selected = this.selectedShift();
    const detail = this.selectedShiftDetail();
    const range = this.personTimelineRange();
    if (!selected || !detail || !range) {
      return [];
    }

    const allRows = this.allRows();
    const shiftsById = new Map<string, ShiftDto>(
      Object.values(this.shiftDetails()).map(shift => [shift.id, shift])
    );
    shiftsById.set(detail.id, detail);

    return detail.assignments
      .filter(assignment => this.isActiveAssignment(assignment.status))
      .map(assignment => ({
        personId: assignment.personId,
        personName: assignment.personName ?? assignment.personId,
        shifts: Array.from(shiftsById.values())
          .filter(shift =>
            shift.assignments.some(candidate =>
              candidate.personId === assignment.personId && this.isActiveAssignment(candidate.status)
            ) &&
            this.shiftIntersectsRange(shift, range.from, range.to)
          )
          .sort((left, right) => left.start.localeCompare(right.start))
          .map(shift => {
            const row = allRows.find(candidate => candidate.shift.shiftId === shift.id);
            const start = Math.max(new Date(shift.start).getTime(), range.from.getTime());
            const end = Math.min(new Date(shift.end).getTime(), range.to.getTime());

            return {
              shiftId: shift.id,
              areaName: row?.areaName ?? '',
              stationName: row?.stationName ?? shift.stationId,
              start: shift.start,
              end: shift.end,
              left: (start - range.from.getTime()) / 60000 * PERSON_TIMELINE_PX_PER_MIN,
              width: Math.max((end - start) / 60000 * PERSON_TIMELINE_PX_PER_MIN, 24),
              isSelected: shift.id === detail.id,
            };
          }),
      }))
      .sort((left, right) => left.personName.localeCompare(right.personName, 'sv'));
  });

  private loadStaff(editionId: string): void {
    this.conventionSvc.listEditionStaff(editionId).subscribe({
      next: staff => this.staff.set(staff),
      error: () => this.error.set(ERROR.fetchStaff),
    });
  }

  private loadApplications(editionId: string): void {
    this.staffSvc.listStaffApplications(editionId).subscribe({
      next: applications => this.applications.set(applications),
      error: () => this.error.set(ERROR.fetchStaffApplications),
    });
  }

  private loadSchedule(editionId: string, preserveContext = false): void {
    this.loading.set(true);
    this.error.set(null);

    const previousDay = preserveContext ? this.selectedDay() : null;
    const previousArea = preserveContext ? this.areaFilter() : 'all';
    const previousStation = preserveContext ? this.stationFilter() : 'all';
    const previousStaffing = preserveContext ? this.staffingFilter() : 'all';
    const previousShiftId = preserveContext ? this.selectedShiftId() : null;

    this.staffSvc.getStaffSchedule(editionId).subscribe({
      next: schedule => {
        this.schedule.set(schedule);
        this.shiftDetails.set({});
        this.primeShiftDetails(schedule);

        const validDay = previousDay && schedule.scheduleDays.some(day => day.date === previousDay)
          ? previousDay
          : schedule.scheduleDays[0]?.date ?? null;
        this.selectedDay.set(validDay);

        const validArea = previousArea === 'all' || schedule.staffAreas.some(area => area.staffAreaId === previousArea)
          ? previousArea
          : 'all';
        this.areaFilter.set(validArea);

        const validStation = previousStation === 'all' || schedule.staffAreas
          .flatMap(area => area.stations)
          .some(station => station.stationId === previousStation)
          ? previousStation
          : 'all';
        this.stationFilter.set(validStation);

        this.staffingFilter.set(previousStaffing);
        this.creatingShift.set(false);
        this.editingShift.set(false);
        this.selectedShiftId.set(previousShiftId);
        this.selectedShiftDetailView.set('assignments');
        this.selectedShiftDetail.set(null);
        this.assignForm.reset({ personId: '' });
        this.editShiftForm.reset({
          stationId: '',
          responsibleId: '',
          startTime: '',
          endTime: '',
          minPersons: 1,
          maxPersons: 4,
        });

        if (previousShiftId) {
          this.loadSelectedShift(previousShiftId, true);
        }

        this.loading.set(false);
      },
      error: () => {
        this.error.set(ERROR.fetchEdition);
        this.loading.set(false);
      },
    });
  }

  onAreaFilterChange(value: string): void {
    this.areaFilter.set(value);
    if (value !== 'all' && !this.stationOptions().some(option => option.id === this.stationFilter())) {
      this.stationFilter.set('all');
    }
    if (value === 'all') {
      this.stationFilter.set('all');
    }
    this.selectedShiftId.set(null);
    this.syncCreateShiftPrefill();
  }

  onStationFilterChange(value: string): void {
    this.stationFilter.set(value);
    this.selectedShiftId.set(null);
    this.syncCreateShiftPrefill();
  }

  onDayChange(value: string | null): void {
    this.selectedDay.set(value);
    this.selectedShiftId.set(null);
    this.syncCreateShiftPrefill();
  }

  onStaffingFilterChange(value: string): void {
    this.staffingFilter.set(value);
    this.selectedShiftId.set(null);
  }

  setViewMode(value: ViewMode): void {
    this.viewMode.set(value);
  }

  setSort(key: StaffingSortKey): void {
    this.sort.set(nextSort(this.sort(), key));
  }

  sortIconFor(key: StaffingSortKey): string {
    return sortIcon(this.sort(), key);
  }

  selectShift(shiftId: string): void {
    if (this.selectedShiftId() === shiftId) {
      this.selectedShiftId.set(null);
      this.selectedShiftDetail.set(null);
      this.selectedShiftDetailView.set('assignments');
      this.assignForm.reset({ personId: '' });
      this.editingShift.set(false);
      return;
    }

    this.selectedShiftId.set(shiftId);
    this.selectedShiftDetailView.set('assignments');
    this.loadSelectedShift(shiftId, true);
  }

  setSelectedShiftDetailView(value: SelectedShiftDetailView): void {
    this.selectedShiftDetailView.set(value);
  }

  openCreateShift(): void {
    this.creatingShift.set(true);
    this.syncCreateShiftPrefill(true);
  }

  cancelCreateShift(): void {
    this.creatingShift.set(false);
  }

  openEditShift(): void {
    const shift = this.selectedShiftDetail();
    if (!shift || shift.status !== 'Planned') {
      return;
    }

    this.editingShift.set(true);
    this.editShiftForm.reset({
      stationId: shift.stationId,
      responsibleId: shift.responsibleId,
      startTime: this.toLocalDateTimeInput(shift.start),
      endTime: this.toLocalDateTimeInput(shift.end),
      minPersons: shift.minPersons,
      maxPersons: shift.maxPersons,
    });
  }

  cancelEditShift(): void {
    this.editingShift.set(false);
  }

  submitCreateShift(): void {
    if (this.createShiftForm.invalid || this.saving()) {
      return;
    }

    const { stationId, responsibleId, startTime, endTime, minPersons, maxPersons } = this.createShiftForm.getRawValue();
    if (!stationId || !responsibleId || !startTime || !endTime) {
      return;
    }

    this.saving.set(true);
    this.staffSvc.createShift(stationId, responsibleId, startTime, endTime, minPersons!, maxPersons!).subscribe({
      next: () => {
        this.saving.set(false);
        this.creatingShift.set(false);
        this.reloadSchedule(true);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.createShift));
      },
    });
  }

  navigateToArea(areaId: string): void {
    this.router.navigate(['/staff-areas', areaId]);
  }

  submitEditShift(): void {
    const shift = this.selectedShiftDetail();
    if (!shift || this.editShiftForm.invalid || this.saving()) {
      return;
    }

    const { stationId, responsibleId, startTime, endTime, minPersons, maxPersons } = this.editShiftForm.getRawValue();
    if (!stationId || !responsibleId || !startTime || !endTime) {
      return;
    }

    this.saving.set(true);
    this.staffSvc.updateShift(shift.id, stationId, responsibleId, startTime, endTime, minPersons!, maxPersons!).subscribe({
      next: () => {
        this.saving.set(false);
        this.editingShift.set(false);
        this.reloadSchedule(true);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.saveShift));
      },
    });
  }

  assignPerson(): void {
    const shift = this.selectedShiftDetail();
    const personId = this.assignForm.controls.personId.value;
    if (!shift || !personId || this.assignForm.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.staffSvc.assignPerson(shift.id, personId).subscribe({
      next: () => {
        this.saving.set(false);
        this.assignForm.reset({ personId: '' });
        this.reloadSchedule(true);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.assignPerson));
      },
    });
  }

  confirmAssignment(assignmentId: string): void {
    const shift = this.selectedShiftDetail();
    if (!shift || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.staffSvc.confirmAssignment(shift.id, assignmentId).subscribe({
      next: () => {
        this.saving.set(false);
        this.reloadSchedule(true);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.confirmAssignment));
      },
    });
  }

  rejectAssignment(assignmentId: string): void {
    const shift = this.selectedShiftDetail();
    if (!shift || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.staffSvc.rejectAssignment(shift.id, assignmentId).subscribe({
      next: () => {
        this.saving.set(false);
        this.reloadSchedule(true);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.rejectAssignment));
      },
    });
  }

  cancelAssignment(assignmentId: string): void {
    const shift = this.selectedShiftDetail();
    if (!shift || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.staffSvc.cancelAssignment(shift.id, assignmentId).subscribe({
      next: () => {
        this.saving.set(false);
        this.reloadSchedule(true);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.unassignPerson));
      },
    });
  }

  cancelShift(): void {
    const shift = this.selectedShiftDetail();
    if (!shift || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.staffSvc.cancelShift(shift.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.reloadSchedule(true);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.cancelShift));
      },
    });
  }

  chooseCandidate(personId: string): void {
    this.assignForm.patchValue({ personId });
  }

  assignmentStatusLabel(status: string): string {
    return ({
      Assigned: 'Tilldelad',
      Confirmed: 'Bekräftad',
      Rejected: 'Nekad',
      Cancelled: 'Avbokad',
    } as Record<string, string>)[status] ?? status;
  }

  shiftStatusLabel(status: string): string {
    return ({
      Planned: 'Planerat',
      InProgress: 'Pågår',
      Cancelled: 'Inställt',
      Completed: 'Avslutat',
    } as Record<string, string>)[status] ?? status;
  }

  applicationStatusLabel(status: string | null): string {
    if (!status) {
      return this.PAGE.noApplication;
    }

    return STAFF_APPLICATION_STATUS_LABEL[status] ?? status;
  }

  staffingStatusLabel(status: string): string {
    return STAFFING_STATUS_LABEL[status] ?? status;
  }

  staffingSummary(shift: StaffScheduleShiftDto): string {
    return `${shift.activeAssignmentCount}/${shift.minPersons}-${shift.maxPersons}`;
  }

  applicationPreferenceNames(personId: string): string[] {
    const ids = this.applicationForPerson(personId)?.staffAreaPreferenceIds ?? [];
    const areas = this.schedule()?.staffAreas ?? [];

    return ids.map(id => areas.find(area => area.staffAreaId === id)?.name ?? id);
  }

  applicationAvailabilitySummary(personId: string): string[] {
    const availabilities = this.applicationForPerson(personId)?.availabilities ?? [];
    return availabilities.map(availability => this.formatAvailability(availability.start, availability.end));
  }

  applicationInterestDescription(personId: string): string | null {
    const value = this.applicationForPerson(personId)?.interestDescription?.trim();
    return value ? value : null;
  }

  applicationSummaryChips(personId: string): string[] {
    const chips: string[] = [];
    chips.push(...this.applicationPreferenceNames(personId));
    chips.push(...this.applicationAvailabilitySummary(personId));

    return chips;
  }

  hasApplicationSummary(personId: string): boolean {
    return !!this.applicationInterestDescription(personId) || this.applicationSummaryChips(personId).length > 0;
  }

  get shiftMin(): string | undefined {
    const day = this.selectedDay();
    const scheduleDay = this.schedule()?.scheduleDays.find(d => d.date === day);
    if (!day) {
      return undefined;
    }

    const time = scheduleDay?.startTime ?? '00:00';
    return `${day}T${time.slice(0, 5)}`;
  }

  get shiftMax(): string | undefined {
    const day = this.selectedDay();
    const scheduleDay = this.schedule()?.scheduleDays.find(d => d.date === day);
    if (!day) {
      return undefined;
    }

    const time = scheduleDay?.endTime ?? '23:59';
    return `${day}T${time.slice(0, 5)}`;
  }

  get editShiftMin(): string | undefined {
    const shift = this.selectedShiftDetail();
    if (!shift) {
      return undefined;
    }

    const day = shift.start.slice(0, 10);
    const scheduleDay = this.schedule()?.scheduleDays.find(d => d.date === day);
    return `${day}T${(scheduleDay?.startTime ?? '00:00').slice(0, 5)}`;
  }

  get editShiftMax(): string | undefined {
    const shift = this.selectedShiftDetail();
    if (!shift) {
      return undefined;
    }

    const day = shift.end.slice(0, 10);
    const scheduleDay = this.schedule()?.scheduleDays.find(d => d.date === day);
    return `${day}T${(scheduleDay?.endTime ?? '23:59').slice(0, 5)}`;
  }

  private shiftOverlapsDay(shift: StaffScheduleShiftDto, day: string): boolean {
    const scheduleDay = this.schedule()?.scheduleDays.find(d => d.date === day);
    const [startHour, startMinute] = (scheduleDay?.startTime ?? '00:00').split(':').map(Number);
    const [endHour, endMinute] = (scheduleDay?.endTime ?? '23:59').split(':').map(Number);

    const dayStart = this.parseDateOnly(day);
    dayStart.setHours(startHour, startMinute ?? 0, 0, 0);

    const dayEnd = this.parseDateOnly(day);
    dayEnd.setHours(endHour, endMinute ?? 0, 0, 0);

    const shiftStart = new Date(shift.start);
    const shiftEnd = new Date(shift.end);

    return shiftStart < dayEnd && shiftEnd > dayStart;
  }

  private parseDateOnly(value: string): Date {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, (month ?? 1) - 1, day ?? 1);
  }

  private scheduleBoundaryDate(day: string, boundary: 'start' | 'end'): Date {
    const scheduleDay = this.schedule()?.scheduleDays.find(candidate => candidate.date === day);
    const [hours, minutes] = (
      boundary === 'start'
        ? scheduleDay?.startTime ?? '00:00'
        : scheduleDay?.endTime ?? '23:59'
    ).split(':').map(Number);
    const date = this.parseDateOnly(day);
    date.setHours(hours, minutes ?? 0, 0, 0);
    return date;
  }

  private formatAvailability(start: string, end: string): string {
    const startDate = new Date(start);
    const endDate = new Date(end);
    const sameDay = startDate.toDateString() === endDate.toDateString();
    const dateLabel = startDate.toLocaleDateString('sv-SE', {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
    });
    const startTime = startDate.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });
    const endTime = endDate.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });

    return sameDay
      ? `${dateLabel} ${startTime}-${endTime}`
      : `${dateLabel} ${startTime} - ${endDate.toLocaleDateString('sv-SE', { weekday: 'short', day: 'numeric', month: 'short' })} ${endTime}`;
  }

  private syncCreateShiftPrefill(forceReset = false): void {
    if (!this.creatingShift() && !forceReset) {
      return;
    }

    const preferredStationId = this.stationFilter() !== 'all'
      ? this.stationFilter()
      : this.stationOptions()[0]?.id ?? '';

    const start = this.shiftMin ?? '';
    const end = start
      ? this.addMinutes(start, 60)
      : '';

    const current = this.createShiftForm.getRawValue();
    this.createShiftForm.reset({
      stationId: preferredStationId,
      responsibleId: forceReset ? '' : (current.responsibleId ?? ''),
      startTime: start,
      endTime: end,
      minPersons: forceReset ? 1 : (current.minPersons ?? 1),
      maxPersons: forceReset ? 4 : (current.maxPersons ?? 4),
    });
  }

  private addMinutes(value: string, minutes: number): string {
    const date = new Date(value);
    date.setMinutes(date.getMinutes() + minutes);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  private toLocalDateTimeInput(value: string): string {
    const date = new Date(value);
    const offset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
  }

  private reloadSchedule(preserveContext = true): void {
    const editionId = this.editionCtx.activeEdition()?.id;
    if (editionId) {
      this.loadSchedule(editionId, preserveContext);
      this.loadApplications(editionId);
    }
  }

  private loadSelectedShift(shiftId: string, force = false): void {
    if (!force) {
      const cached = this.shiftDetails()[shiftId];
      if (cached) {
        this.selectedShiftDetail.set(cached);
        return;
      }
    }

    this.shiftLoading.set(true);
    this.staffSvc.getShift(shiftId).subscribe({
      next: shift => {
        this.shiftDetails.update(details => ({ ...details, [shift.id]: shift }));
        if (this.selectedShiftId() === shift.id) {
          this.selectedShiftDetail.set(shift);
        }
        this.shiftLoading.set(false);
      },
      error: () => {
        this.shiftLoading.set(false);
      },
    });
  }

  private primeShiftDetails(schedule: StaffScheduleDto): void {
    const shiftIds = schedule.staffAreas
      .flatMap(area => area.stations)
      .flatMap(station => station.shifts)
      .map(shift => shift.shiftId);

    for (const shiftId of shiftIds) {
      if (this.shiftDetails()[shiftId]) {
        continue;
      }

      this.staffSvc.getShift(shiftId).subscribe({
        next: shift => {
          this.shiftDetails.update(details => ({ ...details, [shift.id]: shift }));
          if (this.selectedShiftId() === shift.id) {
            this.selectedShiftDetail.set(shift);
          }
        },
      });
    }
  }

  private personWarnings(personId: string, selected: StaffingTableRow, excludeShiftId?: string): string[] {
    const warnings: string[] = [];
    const application = this.applicationForPerson(personId);

    if (!application) {
      warnings.push(this.PAGE.missingApplicationWarning);
    } else {
      if (application.status !== 'Confirmed' && application.status !== 'Assigned') {
        warnings.push(this.PAGE.applicationNotApprovedWarning(this.applicationStatusLabel(application.status).toLowerCase()));
      }

      if (application.staffAreaPreferenceIds.length > 0 && !application.staffAreaPreferenceIds.includes(selected.areaId)) {
        warnings.push(this.PAGE.stationPreferenceWarning);
      }

      if (application.availabilities.length === 0) {
        warnings.push(this.PAGE.noAvailabilityWarning);
      } else if (!this.isAvailableForShift(application, selected.shift)) {
        warnings.push(this.PAGE.unavailableWarning);
      }
    }

    const overlaps = this.findOverlappingAssignments(personId, selected.shift.shiftId, excludeShiftId);
    if (overlaps.length > 0) {
      warnings.push(this.PAGE.overlapWarning(overlaps.length));
    }

    return warnings;
  }

  private applicationForPerson(personId: string): StaffApplicationSummaryDto | undefined {
    return this.applications().find(application => application.personId === personId);
  }

  private isApprovedStaffCandidate(personId: string): boolean {
    const status = this.applicationForPerson(personId)?.status;
    return status === 'Confirmed' || status === 'Assigned';
  }

  private isAvailableForShift(application: StaffApplicationSummaryDto, shift: StaffScheduleShiftDto): boolean {
    const shiftStart = new Date(shift.start).getTime();
    const shiftEnd = new Date(shift.end).getTime();

    return application.availabilities.some(availability => {
      const availableStart = new Date(availability.start).getTime();
      const availableEnd = new Date(availability.end).getTime();
      return availableStart <= shiftStart && availableEnd >= shiftEnd;
    });
  }

  private findOverlappingAssignments(personId: string, selectedShiftId: string, excludeShiftId?: string): ShiftDto[] {
    const currentShift = this.shiftDetails()[selectedShiftId] ?? this.selectedShiftDetail();
    if (!currentShift) {
      return [];
    }

    return Object.values(this.shiftDetails())
      .filter(shift =>
        shift.id !== selectedShiftId &&
        shift.id !== excludeShiftId &&
        shift.status !== 'Cancelled' &&
        shift.assignments.some(assignment =>
          assignment.personId === personId && this.isActiveAssignment(assignment.status)
        ) &&
        this.shiftsOverlap(currentShift, shift)
      );
  }

  private isActiveAssignment(status: string): boolean {
    return status === 'Assigned' || status === 'Confirmed';
  }

  private shiftsOverlap(left: Pick<ShiftDto, 'start' | 'end'>, right: Pick<ShiftDto, 'start' | 'end'>): boolean {
    return new Date(left.start) < new Date(right.end) && new Date(left.end) > new Date(right.start);
  }

  private shiftIntersectsRange(
    shift: Pick<ShiftDto, 'start' | 'end'>,
    from: Date,
    to: Date,
  ): boolean {
    return new Date(shift.start) < to && new Date(shift.end) > from;
  }
}
