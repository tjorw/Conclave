import { DatePipe } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EditionContextService } from '../../../services/edition-context.service';
import { ERROR } from '../../../labels/errors.labels';
import {
  ConventionService, DateTimeRangeComponent, EditionDto, EditionStaffMemberDto, ShiftDto, ShiftSummaryDto,
  StaffService, StaffAreaDto, StationDto,
  ASSIGNMENT_STATUS_LABEL, SHIFT_STATUS_LABEL,
  toErrorMessage,
} from 'shared';
import { MatDividerModule } from '@angular/material/divider';
import { nextSort, sortBy, sortIcon, SortState } from '../../../shared/sort-utils';

type ShiftSortKey = 'responsible' | 'start' | 'end' | 'min' | 'max' | 'staffing' | 'status';
type AssignmentSortKey = 'person' | 'status' | 'assigned';

@Component({
  selector: 'app-staff-area-detail',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    DateTimeRangeComponent,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
  ],
  templateUrl: './staff-area-detail.component.html',
  styleUrl: './staff-area-detail.component.scss',
})
export class StaffAreaDetailComponent {
  private readonly svc           = inject(StaffService);
  private readonly conventionSvc = inject(ConventionService);
  private readonly fb            = inject(FormBuilder);
  private readonly route         = inject(ActivatedRoute);
  readonly editionCtx            = inject(EditionContextService);

  readonly loading   = signal(true);
  readonly saving    = signal(false);
  readonly error     = signal<string | null>(null);

  readonly areaId = signal<string>('');
  readonly edition = signal<EditionDto | null>(null);

  readonly shiftsByStation = signal<Record<string, ShiftSummaryDto[]>>({});
  readonly selectedShift   = signal<ShiftDto | null>(null);
  readonly shiftLoading    = signal(false);
  readonly shiftSort = signal<SortState<ShiftSortKey>>({ key: 'start', direction: 'desc' });
  readonly assignmentSort = signal<SortState<AssignmentSortKey>>({ key: 'assigned', direction: 'desc' });

  readonly staff = signal<EditionStaffMemberDto[]>([]);

  // Stationshantering
  readonly editingStation    = signal<StationDto | null>(null);
  readonly addingStation     = signal(false);

  readonly stationForm = this.fb.group({
    name:        ['', Validators.required],
    description: [''],
  });

  readonly createShiftForStation = signal<string | null>(null);

  readonly createShiftForm = this.fb.group({
    responsibleId: ['', Validators.required],
    startTime:     ['', Validators.required],
    endTime:       ['', Validators.required],
    minPersons:    [1,  [Validators.required, Validators.min(1)]],
    maxPersons:    [4,  [Validators.required, Validators.min(1)]],
  });

  readonly assignForm = this.fb.group({
    personId: ['', Validators.required],
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('areaId') ?? '';
    this.areaId.set(id);

    effect(() => {
      const summary = this.editionCtx.activeEdition();
      if (!summary) { this.loading.set(false); return; }
      this.loadEdition(summary.id);
      this.loadStaff(summary.id);
    });
  }

  private loadStaff(editionId: string): void {
    this.conventionSvc.listEditionStaff(editionId).subscribe({
      next: staff => this.staff.set(staff),
      error: () => this.error.set(ERROR.fetchStaff),
    });
  }

  private loadEdition(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.selectedShift.set(null);

    this.conventionSvc.getEdition(editionId).subscribe({
      next: ed => {
        this.edition.set(ed);
        this.loadShiftsForArea(ed);
      },
      error: () => { this.error.set(ERROR.fetchEdition); this.loading.set(false); },
    });
  }

  private loadShiftsForArea(ed: EditionDto): void {
    const stations = this.stations;
    if (stations.length === 0) { this.shiftsByStation.set({}); this.loading.set(false); return; }

    let remaining = stations.length;
    const result: Record<string, ShiftSummaryDto[]> = {};

    for (const station of stations) {
      this.svc.listShifts(station.id).subscribe({
        next: shifts => {
          result[station.id] = shifts;
          if (--remaining === 0) { this.shiftsByStation.set({ ...result }); this.loading.set(false); }
        },
        error: () => {
          result[station.id] = [];
          if (--remaining === 0) { this.shiftsByStation.set({ ...result }); this.loading.set(false); }
        },
      });
    }
  }

  get area(): StaffAreaDto | undefined {
    return this.edition()?.staffAreas?.find(a => a.id === this.areaId());
  }

  get stations(): StationDto[] {
    return (this.edition()?.stations ?? []).filter(s => s.staffAreaId === this.areaId());
  }

  sortedShifts(shifts: ShiftSummaryDto[] | undefined): ShiftSummaryDto[] {
    return sortBy(shifts ?? [], this.shiftSort(), {
      responsible: shift => shift.responsibleName,
      start: shift => shift.start,
      end: shift => shift.end,
      min: shift => shift.minPersons,
      max: shift => shift.maxPersons,
      staffing: shift => shift.activeAssignmentCount,
      status: shift => this.shiftStatusLabel(shift.status),
    });
  }

  setShiftSort(key: ShiftSortKey): void {
    this.shiftSort.set(nextSort(this.shiftSort(), key));
  }

  shiftSortIcon(key: ShiftSortKey): string {
    return sortIcon(this.shiftSort(), key);
  }

  sortedAssignments(shift: ShiftDto): ShiftDto['assignments'] {
    return sortBy(shift.assignments, this.assignmentSort(), {
      person: assignment => assignment.personName,
      status: assignment => this.assignmentStatusLabel(assignment.status),
      assigned: assignment => assignment.assignedAt,
    });
  }

  setAssignmentSort(key: AssignmentSortKey): void {
    this.assignmentSort.set(nextSort(this.assignmentSort(), key));
  }

  assignmentSortIcon(key: AssignmentSortKey): string {
    return sortIcon(this.assignmentSort(), key);
  }

  // ── Stationer ─────────────────────────────────────────────────────────────

  openAddStation(): void {
    this.stationForm.reset({ name: '', description: '' });
    this.addingStation.set(true);
    this.editingStation.set(null);
  }

  openEditStation(station: StationDto): void {
    this.stationForm.patchValue({ name: station.name, description: station.description ?? '' });
    this.editingStation.set(station);
    this.addingStation.set(false);
  }

  cancelStationForm(): void {
    this.addingStation.set(false);
    this.editingStation.set(null);
  }

  submitStation(): void {
    if (this.stationForm.invalid || this.saving()) return;
    const { name, description } = this.stationForm.getRawValue();
    const editionId = this.editionCtx.activeEdition()?.id;
    if (!editionId) return;

    this.saving.set(true);
    const editing = this.editingStation();

    if (editing) {
      this.conventionSvc.updateStation(editionId, editing.id, { name: name!, description: description || null }).subscribe({
        next: () => { this.saving.set(false); this.cancelStationForm(); this.reloadEdition(); },
        error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.updateStation)); },
      });
    } else {
      this.conventionSvc.createStation(editionId, { name: name!, description: description || null, staffAreaId: this.areaId() }).subscribe({
        next: () => { this.saving.set(false); this.cancelStationForm(); this.reloadEdition(); },
        error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.createStation)); },
      });
    }
  }

  removeStation(station: StationDto): void {
    if (this.saving()) return;
    const editionId = this.editionCtx.activeEdition()?.id;
    if (!editionId) return;
    this.saving.set(true);
    this.conventionSvc.removeStation(editionId, station.id).subscribe({
      next: () => { this.saving.set(false); this.reloadEdition(); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.removeStation)); },
    });
  }

  private reloadEdition(): void {
    const editionId = this.editionCtx.activeEdition()?.id;
    if (editionId) this.loadEdition(editionId);
  }

  // ── Pass ──────────────────────────────────────────────────────────────────

  selectShift(shiftId: string): void {
    if (this.selectedShift()?.id === shiftId) { this.selectedShift.set(null); return; }
    this.shiftLoading.set(true);
    this.svc.getShift(shiftId).subscribe({
      next: s => { this.selectedShift.set(s); this.shiftLoading.set(false); },
      error: () => this.shiftLoading.set(false),
    });
  }

  openCreateShift(stationId: string): void {
    this.createShiftForStation.set(stationId);
    this.createShiftForm.reset({ minPersons: 1, maxPersons: 4 });
  }

  cancelCreateShift(): void { this.createShiftForStation.set(null); }

  submitCreateShift(): void {
    const stationId = this.createShiftForStation();
    if (!stationId || this.createShiftForm.invalid || this.saving()) return;
    const { responsibleId, startTime, endTime, minPersons, maxPersons } = this.createShiftForm.getRawValue();
    this.saving.set(true);
    this.svc.createShift(stationId, responsibleId!, startTime!, endTime!, minPersons!, maxPersons!).subscribe({
      next: () => {
        this.saving.set(false);
        this.createShiftForStation.set(null);
        const ed = this.edition();
        if (ed) this.loadShiftsForArea(ed);
      },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.createShift)); },
    });
  }

  cancelShift(shiftId: string): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.cancelShift(shiftId).subscribe({
      next: () => {
        this.saving.set(false);
        this.selectedShift.set(null);
        const ed = this.edition();
        if (ed) this.loadShiftsForArea(ed);
      },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.cancelShift)); },
    });
  }

  // ── Tilldelningar ─────────────────────────────────────────────────────────

  assignPerson(): void {
    const shift = this.selectedShift();
    if (!shift || this.assignForm.invalid || this.saving()) return;
    const { personId } = this.assignForm.getRawValue();
    this.saving.set(true);
    this.svc.assignPerson(shift.id, personId!).subscribe({
      next: () => { this.saving.set(false); this.reloadShift(shift.id); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.assignPerson)); },
    });
  }

  confirmAssignment(assignmentId: string): void {
    const shift = this.selectedShift();
    if (!shift || this.saving()) return;
    this.saving.set(true);
    this.svc.confirmAssignment(shift.id, assignmentId).subscribe({
      next: () => { this.saving.set(false); this.reloadShift(shift.id); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.confirmAssignment)); },
    });
  }

  rejectAssignment(assignmentId: string): void {
    const shift = this.selectedShift();
    if (!shift || this.saving()) return;
    this.saving.set(true);
    this.svc.rejectAssignment(shift.id, assignmentId).subscribe({
      next: () => { this.saving.set(false); this.reloadShift(shift.id); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.rejectAssignment)); },
    });
  }

  cancelAssignment(assignmentId: string): void {
    const shift = this.selectedShift();
    if (!shift || this.saving()) return;
    this.saving.set(true);
    this.svc.cancelAssignment(shift.id, assignmentId).subscribe({
      next: () => { this.saving.set(false); this.reloadShift(shift.id); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.unassignPerson)); },
    });
  }

  private reloadShift(shiftId: string): void {
    this.svc.getShift(shiftId).subscribe({ next: s => this.selectedShift.set(s) });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  assignmentStatusLabel(status: string): string {
    return ASSIGNMENT_STATUS_LABEL[status] ?? status;
  }

  shiftStatusLabel(status: string): string {
    return SHIFT_STATUS_LABEL[status] ?? status;
  }

  get shiftMin(): string | undefined { return this.edition()?.start.slice(0, 16); }
  get shiftMax(): string | undefined { return this.edition()?.end.slice(0, 16); }
}
