import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  ConventionService,
  EditionDto,
  StaffApplicationSummaryDto,
  StaffAreaDto,
  StaffService,
  STAFF_APPLICATION_STATUS_CHIP,
  STAFF_APPLICATION_STATUS_LABEL,
  toErrorMessage,
} from 'shared';
import { EditionContextService } from '../../../services/edition-context.service';
import { ERROR } from '../../../labels/errors.labels';

@Component({
  selector: 'app-staff-application-detail',
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
  ],
  templateUrl: './staff-application-detail.component.html',
  styleUrl: './staff-application-detail.component.scss',
})
export class StaffApplicationDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(StaffService);
  private readonly conventionSvc = inject(ConventionService);
  readonly editionCtx = inject(EditionContextService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly application = signal<StaffApplicationSummaryDto | null>(null);
  readonly edition = signal<EditionDto | null>(null);
  readonly routeEditionId = this.route.snapshot.paramMap.get('id');

  private applicationId = '';

  readonly form = this.fb.group({
    interestDescription: ['', Validators.required],
    availabilityDates: this.fb.control<string[]>([], { nonNullable: true }),
    staffAreaIds: this.fb.control<string[]>([], { nonNullable: true }),
  });

  readonly title = computed(() => this.application()?.personName ?? 'Funktionär');

  constructor() {
    if (this.routeEditionId) {
      this.editionCtx.setActive(this.routeEditionId);
    }

    this.route.paramMap
      .pipe(takeUntilDestroyed())
      .subscribe(params => {
        this.applicationId = params.get('applicationId') ?? '';
        this.loadData();
      });
  }

  private loadData(): void {
    if (!this.applicationId) {
      this.error.set('Funktionären hittades inte.');
      this.loading.set(false);
      return;
    }

    const activeEdition = this.editionCtx.activeEdition();
    if (!activeEdition) {
      this.error.set('Välj en upplaga i verktygsfältet först.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.conventionSvc.getEdition(activeEdition.id).subscribe({
      next: edition => this.edition.set(edition),
      error: () => this.error.set(ERROR.fetchEdition),
    });

    this.svc.getStaffApplication(this.applicationId).subscribe({
      next: application => {
        this.application.set(application);
        this.form.reset({
          interestDescription: application.interestDescription,
          availabilityDates: this.availabilityDatesFrom(application.availabilities),
          staffAreaIds: [...application.staffAreaPreferenceIds],
        });
        this.loading.set(false);
      },
      error: err => {
        this.error.set(toErrorMessage(err, ERROR.fetchStaffApplications));
        this.loading.set(false);
      },
    });
  }

  save(): void {
    if (this.form.invalid || this.saving() || !this.application()) return;

    const interestDescription = (this.form.controls.interestDescription.value ?? '').trim();
    const availabilityDates = [...this.form.controls.availabilityDates.value].sort();
    const staffAreaIds = this.form.controls.staffAreaIds.value;

    if (!interestDescription) {
      this.error.set('Intressebeskrivning krävs.');
      return;
    }

    this.saving.set(true);
    this.svc.updateApplication(this.applicationId, {
      interestDescription,
      availabilities: availabilityDates.map(date => ({ from: `${date}T00:00:00`, to: `${date}T23:59:00` })),
      staffAreaIds,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.navigateBack();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.updateApplication));
      },
    });
  }

  remove(): void {
    const application = this.application();
    if (!application || this.saving() || !confirm(`Ta bort ansökan för ${application.personName ?? application.personId}?`)) return;

    this.saving.set(true);
    this.svc.deleteApplication(application.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.navigateBack();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.deleteApplication));
      },
    });
  }

  navigateBack(): void {
    const editionId = this.editionCtx.activeEdition()?.id;
    void this.router.navigate(editionId ? ['/editions', editionId, 'persons', 'staff'] : ['/dashboard']);
  }

  applicationStatusLabel(status: string): string {
    return STAFF_APPLICATION_STATUS_LABEL[status] ?? status;
  }

  applicationStatusChipClass(status: string): string {
    return STAFF_APPLICATION_STATUS_CHIP[status] ?? 'chip chip-grey';
  }

  isStaffAreaSelected(staffAreaId: string): boolean {
    return this.form.controls.staffAreaIds.value.includes(staffAreaId);
  }

  toggleStaffArea(staffAreaId: string, checked: boolean): void {
    const selected = this.form.controls.staffAreaIds.value;
    this.form.controls.staffAreaIds.setValue(
      checked ? [...new Set([...selected, staffAreaId])] : selected.filter(id => id !== staffAreaId)
    );
  }

  isAvailabilityDateSelected(date: string): boolean {
    return this.form.controls.availabilityDates.value.includes(date);
  }

  toggleAvailabilityDate(date: string, checked: boolean): void {
    const selected = this.form.controls.availabilityDates.value;
    this.form.controls.availabilityDates.setValue(
      checked ? [...new Set([...selected, date])] : selected.filter(id => id !== date)
    );
  }

  staffAreaName(staffAreaId: string): string {
    return this.edition()?.staffAreas?.find((s: StaffAreaDto) => s.id === staffAreaId)?.name ?? staffAreaId;
  }

  availabilityDays(): Array<{ date: string; label: string }> {
    const edition = this.edition();
    if (!edition) return [];

    const start = this.parseDateOnly(edition.start);
    const end = this.parseDateOnly(edition.end);
    if (!start || !end || start > end) return [];

    const days: Array<{ date: string; label: string }> = [];
    for (const current = new Date(start); current <= end; current.setDate(current.getDate() + 1)) {
      const date = this.toDateOnly(current);
      days.push({
        date,
        label: new Intl.DateTimeFormat('sv-SE', {
          weekday: 'short',
          day: 'numeric',
          month: 'short',
        }).format(current),
      });
    }

    return days;
  }

  private availabilityDatesFrom(availabilities: StaffApplicationSummaryDto['availabilities']): string[] {
    return availabilities
      .map(av => av.start.slice(0, 10))
      .filter((value, index, all) => all.indexOf(value) === index)
      .sort();
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
      `${value.getMonth() + 1}`.padStart(2, '0'),
      `${value.getDate()}`.padStart(2, '0'),
    ].join('-');
  }
}
