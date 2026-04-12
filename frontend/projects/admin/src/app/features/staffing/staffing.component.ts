import { DatePipe } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EditionContextService } from '../../services/edition-context.service';
import {
  ConventionService, EditionDto, PersonDto, ShiftDto, ShiftSummaryDto,
  StaffApplicationSummaryDto, StaffService, StaffAreaDto, StationDto,
} from 'shared';

@Component({
  selector: 'app-staffing',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTabsModule,
    MatTooltipModule,
  ],
  templateUrl: './staffing.component.html',
  styleUrl: './staffing.component.scss',
})
export class StaffingComponent {
  private readonly svc           = inject(StaffService);
  private readonly conventionSvc = inject(ConventionService);
  private readonly fb            = inject(FormBuilder);
  readonly editionCtx            = inject(EditionContextService);

  readonly loading     = signal(true);
  readonly saving      = signal(false);
  readonly error       = signal<string | null>(null);

  // Laddad EditionDto (med stationer/areas)
  readonly edition = signal<EditionDto | null>(null);

  // Pass per station: stationId → ShiftSummaryDto[]
  readonly shiftsByStation = signal<Record<string, ShiftSummaryDto[]>>({});

  // Valt pass för detaljvy
  readonly selectedShift  = signal<ShiftDto | null>(null);
  readonly shiftLoading   = signal(false);

  // Staffansökningar
  readonly applications       = signal<StaffApplicationSummaryDto[]>([]);
  readonly applicationsLoaded = signal(false);

  // Personlista för selectors
  readonly persons = signal<PersonDto[]>([]);

  // Skapa pass – vilken station
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
    this.conventionSvc.listPersons().subscribe({
      next: persons => this.persons.set(persons.filter(p => p.isActive)),
    });

    effect(() => {
      const summary = this.editionCtx.activeEdition();
      if (!summary) { this.loading.set(false); return; }
      this.loadEdition(summary.id);
    });
  }

  private loadEdition(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.selectedShift.set(null);
    this.applicationsLoaded.set(false);

    this.conventionSvc.getEdition(editionId).subscribe({
      next: ed => {
        this.edition.set(ed);
        this.loadAllShifts(ed);
      },
      error: () => { this.error.set('Kunde inte ladda upplagan.'); this.loading.set(false); },
    });
  }

  private loadAllShifts(ed: EditionDto): void {
    const stations = ed.stations;
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
        if (ed) this.loadAllShifts(ed);
      },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte skapa passet.'); },
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
        if (ed) this.loadAllShifts(ed);
      },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte ställa in passet.'); },
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
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte tilldela personen.'); },
    });
  }

  confirmAssignment(assignmentId: string): void {
    const shift = this.selectedShift();
    if (!shift || this.saving()) return;
    this.saving.set(true);
    this.svc.confirmAssignment(shift.id, assignmentId).subscribe({
      next: () => { this.saving.set(false); this.reloadShift(shift.id); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte bekräfta.'); },
    });
  }

  rejectAssignment(assignmentId: string): void {
    const shift = this.selectedShift();
    if (!shift || this.saving()) return;
    this.saving.set(true);
    this.svc.rejectAssignment(shift.id, assignmentId).subscribe({
      next: () => { this.saving.set(false); this.reloadShift(shift.id); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte avslå.'); },
    });
  }

  cancelAssignment(assignmentId: string): void {
    const shift = this.selectedShift();
    if (!shift || this.saving()) return;
    this.saving.set(true);
    this.svc.cancelAssignment(shift.id, assignmentId).subscribe({
      next: () => { this.saving.set(false); this.reloadShift(shift.id); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte ta bort tilldelningen.'); },
    });
  }

  private reloadShift(shiftId: string): void {
    this.svc.getShift(shiftId).subscribe({ next: s => this.selectedShift.set(s) });
  }

  // ── Staffansökningar ──────────────────────────────────────────────────────

  onApplicationsTabSelected(): void {
    if (this.applicationsLoaded()) return;
    const summary = this.editionCtx.activeEdition();
    if (!summary) return;
    this.svc.listStaffApplications(summary.id).subscribe({
      next: apps => { this.applications.set(apps); this.applicationsLoaded.set(true); },
      error: () => this.error.set('Kunde inte hämta ansökningar.'),
    });
  }

  acceptApplication(id: string): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.acceptApplication(id).subscribe({
      next: () => {
        this.saving.set(false);
        this.applications.update(apps => apps.map(a => a.id === id ? { ...a, status: 'Accepted' } : a));
      },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte acceptera.'); },
    });
  }

  rejectApplication(id: string): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.rejectApplication(id).subscribe({
      next: () => {
        this.saving.set(false);
        this.applications.update(apps => apps.map(a => a.id === id ? { ...a, status: 'Rejected' } : a));
      },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte avslå.'); },
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  get staffAreas(): StaffAreaDto[] {
    return this.edition()?.staffAreas ?? [];
  }

  stationsForArea(areaId: string): StationDto[] {
    return (this.edition()?.stations ?? []).filter(s => s.staffAreaId === areaId);
  }

  stationName(stationId: string): string {
    return this.edition()?.stations?.find(s => s.id === stationId)?.name ?? stationId;
  }

  assignmentStatusLabel(status: string): string {
    const map: Record<string, string> = {
      Pending: 'Väntar', Confirmed: 'Bekräftad', Rejected: 'Avslagen', Cancelled: 'Avbokad',
    };
    return map[status] ?? status;
  }

  applicationStatusLabel(status: string): string {
    const map: Record<string, string> = { Received: 'Mottagen', Accepted: 'Accepterad', Rejected: 'Avslagen' };
    return map[status] ?? status;
  }

  shiftStatusLabel(status: string): string {
    const map: Record<string, string> = { Open: 'Öppet', Full: 'Fullt', Cancelled: 'Inställt' };
    return map[status] ?? status;
  }
}
