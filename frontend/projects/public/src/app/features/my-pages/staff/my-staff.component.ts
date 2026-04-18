import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom, forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  ConventionService,
  MyAssignedShiftSummaryDto,
  MyStaffApplicationDto,
  RegistrationService,
  STAFF_APPLICATION_STATUS_LABEL,
  StationDto,
} from 'shared';
import { EditionService } from '../../../services/edition.service';

@Component({
  selector: 'app-my-staff',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <div class="page-container staff-page">
      <div class="back-link">
        <a routerLink="/my-pages">← Mina sidor</a>
      </div>

      <header class="page-header">
        <h1 class="page-title">Min funktionering</h1>
      </header>

      @if (loading()) {
        <div class="loading">
          <mat-spinner diameter="40" />
        </div>
      } @else {
        @if (error()) {
          <p class="error-banner">{{ error() }}</p>
        }

        @if (application(); as app) {
          <section class="card status-card">
            <h2>Ansökningsstatus</h2>
            <p class="status-row">
              <span class="label">Status</span>
              <span [class]="statusChipClass(app.status)">{{ statusLabel(app.status) }}</span>
            </p>
          </section>

          <section class="card shift-card">
            <h2>Tilldelade pass</h2>
            @if (assignedShifts().length === 0) {
              <p class="empty-text">Du har inga tilldelade pass ännu.</p>
            } @else {
              <div class="shift-list">
                @for (shift of assignedShifts(); track shift.shiftId) {
                  <article class="shift-item">
                    <h3>{{ shift.stationName }}</h3>
                    <p>{{ displayDate(shift.start) }}</p>
                    <p>{{ displayTime(shift.start) }} - {{ displayTime(shift.end) }}</p>
                  </article>
                }
              </div>
            }
          </section>
        } @else {
          <section class="card form-card">
            <h2>Ansök som funktionär</h2>

            @if (!staffRegistrationOpen()) {
              <p class="registration-closed">
                Funktionärsregistrering är inte öppen för denna upplaga just nu.
              </p>
            }

            <form [formGroup]="applicationForm" (ngSubmit)="submitApplication()">
              <div class="field-group">
                <label for="interestDescription">Motivering</label>
                <textarea
                  id="interestDescription"
                  formControlName="interestDescription"
                  rows="5"
                  placeholder="Beskriv hur du vill bidra och vad du har erfarenhet av"
                ></textarea>
                @if (applicationForm.controls.interestDescription.touched && applicationForm.controls.interestDescription.invalid) {
                  <p class="field-error">Skriv minst 10 tecken i motiveringen.</p>
                }
              </div>

              <div class="field-group">
                <p class="group-label">Stationspreferenser</p>
                @if (stations().length === 0) {
                  <p class="empty-text">Inga stationer är upplagda ännu.</p>
                } @else {
                  <div class="checkbox-grid">
                    @for (station of stations(); track station.id) {
                      <label class="checkbox-card">
                        <input
                          type="checkbox"
                          [checked]="isStationSelected(station.id)"
                          (change)="toggleStation(station.id, $any($event.target).checked)"
                        />
                        <span>{{ station.name }}</span>
                      </label>
                    }
                  </div>
                }
              </div>

              <div class="field-group">
                <p class="group-label">Tillgänglighet</p>
                <div class="checkbox-grid days">
                  <label class="checkbox-card">
                    <input type="checkbox" formControlName="availableFriday" />
                    <span>Fredag</span>
                  </label>
                  <label class="checkbox-card">
                    <input type="checkbox" formControlName="availableSaturday" />
                    <span>Lördag</span>
                  </label>
                  <label class="checkbox-card">
                    <input type="checkbox" formControlName="availableSunday" />
                    <span>Söndag</span>
                  </label>
                </div>
              </div>

              <button
                mat-flat-button
                type="submit"
                class="submit-btn"
                [disabled]="submitting() || !staffRegistrationOpen()"
              >
                @if (submitting()) {
                  Skickar ansökan...
                } @else {
                  Skicka ansökan
                }
              </button>
            </form>
          </section>
        }
      }
    </div>
  `,
  styles: [`
    .staff-page {
      padding-top: 32px;
      padding-bottom: 48px;
      max-width: 760px;
    }

    .back-link {
      margin-bottom: 8px;
      font-size: .875rem;
    }

    .back-link a {
      color: var(--brand-primary);
      text-decoration: none;
    }

    .back-link a:hover {
      text-decoration: underline;
    }

    .page-header {
      margin-bottom: 16px;
    }

    .page-title {
      font-size: 1.75rem;
      font-weight: 500;
      color: var(--brand-primary);
      margin: 0;
    }

    .loading {
      display: flex;
      justify-content: center;
      padding: 48px 0;
    }

    .error-banner {
      margin: 0 0 16px;
      border-radius: 8px;
      padding: 10px 12px;
      background: #fdecea;
      color: #8a1f11;
      font-size: .9rem;
    }

    .card {
      background: var(--brand-surface);
      border-radius: 12px;
      box-shadow: 0 2px 12px rgba(0, 0, 0, .07);
      padding: 24px;
    }

    .card h2 {
      font-size: 1.15rem;
      margin: 0 0 16px;
      color: var(--brand-text);
    }

    .status-card {
      margin-bottom: 16px;
    }

    .status-row {
      margin: 0;
      display: flex;
      align-items: center;
      gap: 10px;
    }

    .label {
      color: var(--brand-text-muted);
      font-size: .9rem;
    }

    .status-chip {
      display: inline-block;
      padding: 3px 10px;
      border-radius: 12px;
      font-size: .78rem;
      font-weight: 500;
    }

    .status-chip.green {
      background: #e8f5e9;
      color: #2e7d32;
    }

    .status-chip.orange {
      background: #fff7ed;
      color: #9a3412;
    }

    .status-chip.red {
      background: #fdecea;
      color: #8a1f11;
    }

    .shift-card {
      margin-bottom: 16px;
    }

    .shift-list {
      display: grid;
      gap: 10px;
    }

    .shift-item {
      border: 1px solid #dbe2ea;
      border-radius: 10px;
      padding: 12px 14px;
    }

    .shift-item h3 {
      margin: 0 0 4px;
      color: var(--brand-text);
      font-size: 1rem;
      font-weight: 600;
    }

    .shift-item p {
      margin: 0;
      color: var(--brand-text-muted);
      font-size: .88rem;
    }

    .shift-item p + p {
      margin-top: 2px;
    }

    .form-card form {
      display: flex;
      flex-direction: column;
      gap: 18px;
    }

    .field-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .field-group label,
    .field-group .group-label {
      margin: 0;
      color: var(--brand-text);
      font-weight: 500;
      font-size: .95rem;
    }

    textarea {
      border: 1px solid #dbe2ea;
      border-radius: 10px;
      padding: 10px 12px;
      font: inherit;
      background: #fff;
      color: var(--brand-text);
    }

    textarea:focus {
      outline: none;
      border-color: var(--brand-primary);
      box-shadow: 0 0 0 2px color-mix(in srgb, var(--brand-primary) 16%, transparent);
    }

    .checkbox-grid {
      display: grid;
      gap: 10px;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    }

    .checkbox-grid.days {
      grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
    }

    .checkbox-card {
      display: flex;
      align-items: center;
      gap: 8px;
      border: 1px solid #dbe2ea;
      border-radius: 10px;
      padding: 10px 12px;
      background: #fff;
      color: var(--brand-text);
      cursor: pointer;
    }

    .checkbox-card input {
      width: 16px;
      height: 16px;
    }

    .field-error {
      margin: 0;
      color: #8a1f11;
      font-size: .84rem;
    }

    .empty-text {
      margin: 0;
      color: var(--brand-text-muted);
      font-size: .9rem;
    }

    .registration-closed {
      margin: 0 0 4px;
      border-radius: 8px;
      padding: 10px 12px;
      background: #fff7ed;
      color: #9a3412;
      font-size: .9rem;
    }

    .submit-btn {
      width: fit-content;
      background-color: var(--brand-primary) !important;
      color: #fff !important;
    }

    @media (max-width: 680px) {
      .card {
        padding: 18px;
      }

      .checkbox-grid {
        grid-template-columns: 1fr;
      }
    }
  `],
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

  readonly staffRegistrationOpen = computed(
    () => this.editionSvc.edition()?.staffRegistrationOpen ?? false
  );

  readonly applicationForm = this.fb.group({
    interestDescription: this.fb.control('', { validators: [Validators.required, Validators.minLength(10)], nonNullable: true }),
    availableFriday: this.fb.control(true, { nonNullable: true }),
    availableSaturday: this.fb.control(true, { nonNullable: true }),
    availableSunday: this.fb.control(true, { nonNullable: true }),
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
    const selected = this.applicationForm.controls.stationIds.value;
    const updated = checked
      ? [...selected, stationId]
      : selected.filter(id => id !== stationId);

    this.applicationForm.controls.stationIds.setValue(updated);
  }

  async submitApplication(): Promise<void> {
    if (this.submitting()) return;

    const editionId = this.editionSvc.editionId();
    const edition = this.editionSvc.edition();

    if (!editionId || !edition) {
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

    const selectedWeekdays = this.selectedWeekdays();
    if (selectedWeekdays.length === 0) {
      this.error.set('Välj minst en dag för tillgänglighet.');
      return;
    }

    const availabilityRanges = this.createAvailabilityRanges(edition.startDate, edition.endDate, selectedWeekdays);
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
      const detail = this.toErrorMessage(err);
      this.error.set(detail);
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
        this.assignedShifts.set(
          [...result.shifts].sort((a, b) => Date.parse(a.start) - Date.parse(b.start))
        );
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
    this.assignedShifts.set(
      [...shifts].sort((a, b) => Date.parse(a.start) - Date.parse(b.start))
    );
  }

  private selectedWeekdays(): number[] {
    const weekdays: number[] = [];
    if (this.applicationForm.controls.availableFriday.value) weekdays.push(5);
    if (this.applicationForm.controls.availableSaturday.value) weekdays.push(6);
    if (this.applicationForm.controls.availableSunday.value) weekdays.push(0);
    return weekdays;
  }

  private createAvailabilityRanges(startDate: string, endDate: string, weekdays: number[]): Array<{ from: string; to: string }> {
    const start = new Date(`${startDate}T00:00:00`);
    const end = new Date(`${endDate}T00:00:00`);
    const ranges: Array<{ from: string; to: string }> = [];

    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || start > end) {
      return ranges;
    }

    for (const current = new Date(start); current <= end; current.setDate(current.getDate() + 1)) {
      if (!weekdays.includes(current.getDay())) continue;
      const year = current.getFullYear();
      const month = current.getMonth() + 1;
      const day = current.getDate();
      ranges.push({
        from: `${year}-${this.pad2(month)}-${this.pad2(day)}T08:00:00`,
        to: `${year}-${this.pad2(month)}-${this.pad2(day)}T23:00:00`,
      });
    }

    return ranges;
  }

  private pad2(value: number): string {
    return value.toString().padStart(2, '0');
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
