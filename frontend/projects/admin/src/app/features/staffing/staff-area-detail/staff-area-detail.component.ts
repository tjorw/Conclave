import { DatePipe } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EditionContextService } from '../../../services/edition-context.service';
import {
  ConventionService, EditionDto, PersonDto, ShiftDto, ShiftSummaryDto,
  StaffService, StaffAreaDto, StationDto,
} from 'shared';

@Component({
  selector: 'app-staff-area-detail',
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
  private readonly router        = inject(Router);
  readonly editionCtx            = inject(EditionContextService);

  readonly loading   = signal(true);
  readonly saving    = signal(false);
  readonly error     = signal<string | null>(null);

  readonly areaId = signal<string>('');
  readonly edition = signal<EditionDto | null>(null);

  readonly shiftsByStation = signal<Record<string, ShiftSummaryDto[]>>({});
  readonly selectedShift   = signal<ShiftDto | null>(null);
  readonly shiftLoading    = signal(false);

  readonly persons = signal<PersonDto[]>([]);

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

    this.conventionSvc.getEdition(editionId).subscribe({
      next: ed => {
        this.edition.set(ed);
        this.loadShiftsForArea(ed);
      },
      error: () => { this.error.set('Kunde inte ladda upplagan.'); this.loading.set(false); },
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

  goBack(): void {
    this.router.navigate(['/staffing']);
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
        if (ed) this.loadShiftsForArea(ed);
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

  // ── Helpers ───────────────────────────────────────────────────────────────

  assignmentStatusLabel(status: string): string {
    const map: Record<string, string> = {
      Pending: 'Väntar', Confirmed: 'Bekräftad', Rejected: 'Avslagen', Cancelled: 'Avbokad',
    };
    return map[status] ?? status;
  }

  shiftStatusLabel(status: string): string {
    const map: Record<string, string> = { Open: 'Öppet', Full: 'Fullt', Cancelled: 'Inställt' };
    return map[status] ?? status;
  }
}
