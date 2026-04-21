import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom, forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  ConventionService,
  MyAssignedShiftSummaryDto,
  MyStaffApplicationDto,
  RegistrationService,
  STAFF_APPLICATION_STATUS_LABEL,
  StationDto,
} from 'shared';
import { EditionService } from '../../../services/edition.service';

interface AvailabilityDay {
  date: string;
  label: string;
}

interface EditionDateRange {
  start: string;
  end: string;
}

@Component({
  selector: 'app-my-staff',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './my-staff.component.html',
  styleUrl: './my-staff.component.scss',
})
export class MyStaffComponent implements OnInit {
  private readonly editionSvc = inject(EditionService);
  private readonly conventionSvc = inject(ConventionService);
  private readonly regSvc = inject(RegistrationService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly stations = signal<StationDto[]>([]);
  readonly application = signal<MyStaffApplicationDto | null>(null);
  readonly assignedShifts = signal<MyAssignedShiftSummaryDto[]>([]);
  readonly editionDateRange = signal<EditionDateRange | null>(null);

  readonly staffRegistrationOpen = computed(
    () => this.editionSvc.edition()?.staffRegistrationOpen ?? false
  );

  readonly availabilityDays = computed<AvailabilityDay[]>(() => {
    const range = this.editionDateRange() ?? this.activeFeedDateRange();
    return range ? this.createAvailabilityDays(range.start, range.end) : [];
  });

  readonly applicationForm = this.fb.group({
    interestDescription: this.fb.control('', { validators: Validators.required, nonNullable: true }),
    availabilityDates: this.fb.control<string[]>([], { nonNullable: true }),
    stationIds: this.fb.control<string[]>([], { nonNullable: true }),
  });

  ngOnInit(): void {
    this.loadState();
  }

  statusLabel(status: string): string {
    return STAFF_APPLICATION_STATUS_LABEL[status] ?? status;
  }

  statusChipClass(status: string): string {
    if (status === 'Assigned' || status === 'Confirmed') return 'status-chip green';
    if (status === 'Rejected') return 'status-chip red';
    return 'status-chip orange';
  }

  isStationSelected(stationId: string): boolean {
    return this.applicationForm.controls.stationIds.value.includes(stationId);
  }

  toggleStation(stationId: string, checked: boolean): void {
    this.updateStringSelection(this.applicationForm.controls.stationIds, stationId, checked);
  }

  isAvailabilityDateSelected(date: string): boolean {
    return this.applicationForm.controls.availabilityDates.value.includes(date);
  }

  toggleAvailabilityDate(date: string, checked: boolean): void {
    this.updateStringSelection(this.applicationForm.controls.availabilityDates, date, checked);
  }

  async submitApplication(): Promise<void> {
    if (this.submitting()) return;

    const editionId = this.editionSvc.editionId();

    if (!editionId) {
      this.error.set('Ingen aktiv upplaga hittades.');
      return;
    }

    if (!this.staffRegistrationOpen()) {
      this.error.set('Funktionärsregistrering är inte öppen för denna upplaga.');
      return;
    }

    if (this.applicationForm.invalid) {
      this.applicationForm.markAllAsTouched();
      return;
    }

    const selectedStationIds = this.applicationForm.controls.stationIds.value;
    if (selectedStationIds.length === 0) {
      this.error.set('Välj minst en stationspreferens.');
      return;
    }

    const selectedAvailabilityDates = this.applicationForm.controls.availabilityDates.value;
    if (selectedAvailabilityDates.length === 0) {
      this.error.set('Välj minst en dag för tillgänglighet.');
      return;
    }

    const availabilityRanges = this.createAvailabilityRanges(selectedAvailabilityDates);
    if (availabilityRanges.length === 0) {
      this.error.set('Kunde inte beräkna tillgänglighet för valda dagar inom upplagans datum.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    try {
      const submitResult = await firstValueFrom(
        this.regSvc.submitStaffApplication(editionId, this.applicationForm.controls.interestDescription.value)
      );

      for (const range of availabilityRanges) {
        await firstValueFrom(this.regSvc.addStaffAvailability(submitResult.id, range.from, range.to));
      }

      for (const stationId of selectedStationIds) {
        await firstValueFrom(this.regSvc.addStaffStationPreference(submitResult.id, stationId));
      }

      await this.loadApplicationState(editionId);
    } catch (err) {
      this.error.set(this.toErrorMessage(err));
    } finally {
      this.submitting.set(false);
    }
  }

  displayDate(value: string | null | undefined): string {
    if (!value || Number.isNaN(Date.parse(value))) {
      return 'Okänd tid';
    }

    return new Intl.DateTimeFormat('sv-SE', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  displayTime(value: string | null | undefined): string {
    if (!value || Number.isNaN(Date.parse(value))) {
      return '--:--';
    }

    return new Intl.DateTimeFormat('sv-SE', {
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  private loadState(): void {
    const editionId = this.editionSvc.editionId();
    if (!editionId) {
      this.error.set('Ingen aktiv upplaga hittades.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      edition: this.conventionSvc.getEdition(editionId).pipe(catchError(() => of(null))),
      application: this.regSvc.getMyStaffApplication(editionId).pipe(catchError(() => of(null))),
      shifts: this.regSvc.getMyAssignedShifts(editionId).pipe(catchError(() => of([] as MyAssignedShiftSummaryDto[]))),
    }).subscribe({
      next: result => {
        this.stations.set(result.edition?.stations ?? []);
        this.application.set(result.application);
        this.editionDateRange.set(
          result.edition
            ? { start: result.edition.start, end: result.edition.end }
            : this.activeFeedDateRange()
        );
        this.assignedShifts.set(this.sortShifts(result.shifts));

        if (!result.application) {
          this.selectDefaultAvailabilityDates();
        }

        this.loading.set(false);
      },
      error: () => {
        this.error.set('Kunde inte läsa bemanningsinformation just nu.');
        this.loading.set(false);
      },
    });
  }

  private async loadApplicationState(editionId: string): Promise<void> {
    const [application, shifts] = await Promise.all([
      firstValueFrom(this.regSvc.getMyStaffApplication(editionId).pipe(catchError(() => of(null)))),
      firstValueFrom(this.regSvc.getMyAssignedShifts(editionId).pipe(catchError(() => of([] as MyAssignedShiftSummaryDto[])))),
    ]);

    this.application.set(application);
    this.assignedShifts.set(this.sortShifts(shifts));
  }

  private activeFeedDateRange(): EditionDateRange | null {
    const edition = this.editionSvc.edition();
    return edition ? { start: edition.startDate, end: edition.endDate } : null;
  }

  private selectDefaultAvailabilityDates(): void {
    if (this.applicationForm.controls.availabilityDates.value.length > 0) return;
    this.applicationForm.controls.availabilityDates.setValue(this.availabilityDays().map(day => day.date));
  }

  private createAvailabilityDays(startDate: string, endDate: string): AvailabilityDay[] {
    const start = this.parseDateOnly(startDate);
    const end = this.parseDateOnly(endDate);
    const days: AvailabilityDay[] = [];

    if (!start || !end || start > end) {
      return days;
    }

    for (const current = new Date(start); current <= end; current.setDate(current.getDate() + 1)) {
      const date = this.toDateOnly(current);
      days.push({
        date,
        label: new Intl.DateTimeFormat('sv-SE', {
          weekday: 'long',
          day: 'numeric',
          month: 'long',
        }).format(current),
      });
    }

    return days;
  }

  private createAvailabilityRanges(dates: string[]): Array<{ from: string; to: string }> {
    const availableDates = new Set(this.availabilityDays().map(day => day.date));

    return [...new Set(dates)]
      .filter(date => availableDates.has(date))
      .sort()
      .map(date => ({
        from: `${date}T08:00:00`,
        to: `${date}T23:00:00`,
      }));
  }

  private parseDateOnly(value: string): Date | null {
    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
    if (!match) return null;

    const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
    return Number.isNaN(date.getTime()) ? null : date;
  }

  private toDateOnly(value: Date): string {
    return [
      value.getFullYear(),
      this.pad2(value.getMonth() + 1),
      this.pad2(value.getDate()),
    ].join('-');
  }

  private pad2(value: number): string {
    return value.toString().padStart(2, '0');
  }

  private sortShifts(shifts: MyAssignedShiftSummaryDto[]): MyAssignedShiftSummaryDto[] {
    return [...shifts].sort((a, b) => Date.parse(a.start) - Date.parse(b.start));
  }

  private updateStringSelection(
    control: typeof this.applicationForm.controls.stationIds,
    value: string,
    checked: boolean
  ): void {
    const selected = control.value;
    const updated = checked
      ? [...new Set([...selected, value])]
      : selected.filter(id => id !== value);

    control.setValue(updated);
  }

  private toErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      return error.error?.detail
        ?? error.error?.title
        ?? error.error?.message
        ?? 'Kunde inte skicka ansökan just nu. Försök igen.';
    }

    return 'Kunde inte skicka ansökan just nu. Försök igen.';
  }
}
