import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { TOOLTIP } from '../../labels/ui.labels';
import {
  ConventionService, EditionDto, formatTimeRange, StaffApplicationSummaryDto, StaffService,
  StaffAreaDto, STAFF_APPLICATION_STATUS_CHIP, STAFF_APPLICATION_STATUS_LABEL,
  toErrorMessage,
} from 'shared';
import { createSortController, sortBy } from '../../shared/sort-utils';

type StatusFilter = 'all' | 'pending' | 'accepted' | 'rejected';
type StaffApplicationSortKey = 'person' | 'interest' | 'staffAreas' | 'availability' | 'created' | 'status';

@Component({
  selector: 'app-staff-applications',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './staff-applications.component.html',
  styleUrl: './staff-applications.component.scss',
})
export class StaffApplicationsComponent {
  private readonly svc           = inject(StaffService);
  private readonly conventionSvc = inject(ConventionService);
  readonly editionCtx            = inject(EditionContextService);

  readonly TOOLTIP = TOOLTIP;

  readonly loading = signal(true);
  readonly saving  = signal(false);
  readonly error   = signal<string | null>(null);

  readonly edition      = signal<EditionDto | null>(null);
  readonly applications = signal<StaffApplicationSummaryDto[]>([]);
  readonly statusFilter = signal<StatusFilter>('pending');
  readonly applicationSort = createSortController<StaffApplicationSortKey>({ key: 'created', direction: 'desc' });

  readonly filteredApplications = computed(() => {
    const filter = this.statusFilter();
    const apps = this.applications();
    switch (filter) {
      case 'pending': return apps.filter(a => a.status === 'Received' || a.status === 'UnderReview');
      case 'accepted': return apps.filter(a => a.status === 'Confirmed' || a.status === 'Assigned');
      case 'rejected': return apps.filter(a => a.status === 'Rejected');
      default: return apps;
    }
  });

  readonly sortedFilteredApplications = computed(() =>
    sortBy(this.filteredApplications(), this.applicationSort.state(), {
      person: app => app.personName ?? app.personId,
      interest: app => app.interestDescription,
      staffAreas: app => app.staffAreaPreferenceIds.map(id => this.staffAreaName(id)).join(', '),
      availability: app => app.availabilities.map(av => `${av.start}-${av.end}`).join(', '),
      created: app => app.createdAt,
      status: app => this.applicationStatusLabel(app.status),
    })
  );

  readonly pendingCount = computed(() => this.applications().filter(a => a.status === 'Received' || a.status === 'UnderReview').length);
  readonly acceptedCount = computed(() => this.applications().filter(a => a.status === 'Confirmed' || a.status === 'Assigned').length);
  readonly rejectedCount = computed(() => this.applications().filter(a => a.status === 'Rejected').length);

  constructor() {
    effect(() => {
      const summary = this.editionCtx.activeEdition();
      if (!summary) {
        this.loading.set(false);
        return;
      }

      this.loadPage(summary.id);
    });
  }

  private loadPage(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.conventionSvc.getEdition(editionId).subscribe({
      next: ed => this.edition.set(ed),
      error: () => this.error.set(ERROR.fetchEdition),
    });

    this.svc.listStaffApplications(editionId).subscribe({
      next: apps => {
        this.applications.set(apps);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(ERROR.fetchStaffApplications);
        this.loading.set(false);
      },
    });
  }

  private reloadApplications(): void {
    const summary = this.editionCtx.activeEdition();
    if (!summary) return;

    this.svc.listStaffApplications(summary.id).subscribe({
      next: apps => this.applications.set(apps),
      error: () => this.error.set(ERROR.fetchStaffApplications),
    });
  }



  accept(app: StaffApplicationSummaryDto): void {
    if (this.saving()) return;

    this.saving.set(true);
    this.svc.acceptApplication(app.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.reloadApplications();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.acceptApplication));
      },
    });
  }

  reject(app: StaffApplicationSummaryDto): void {
    if (this.saving()) return;

    this.saving.set(true);
    this.svc.rejectApplication(app.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.reloadApplications();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.rejectApplication));
      },
    });
  }

  remove(app: StaffApplicationSummaryDto): void {
    if (this.saving() || !confirm(`Ta bort ansökan för ${app.personName ?? app.personId}?`)) return;

    this.saving.set(true);
    this.svc.deleteApplication(app.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.reloadApplications();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.deleteApplication));
      },
    });
  }

  canReview(status: string): boolean {
    return status === 'Received' || status === 'UnderReview';
  }

  applicationStatusLabel(status: string): string {
    return STAFF_APPLICATION_STATUS_LABEL[status] ?? status;
  }

  applicationStatusChipClass(status: string): string {
    return STAFF_APPLICATION_STATUS_CHIP[status] ?? 'chip chip-grey';
  }

  staffAreaName(staffAreaId: string): string {
    return this.edition()?.staffAreas?.find((s: StaffAreaDto) => s.id === staffAreaId)?.name ?? staffAreaId;
  }

  protected readonly formatAvailability = formatTimeRange;
}
