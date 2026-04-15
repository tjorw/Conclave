import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DatePipe } from '@angular/common';
import { EditionContextService } from '../../services/edition-context.service';
import { TOOLTIP } from '../../labels/ui.labels';
import {
  ConventionService, EditionDto, StaffApplicationSummaryDto, StaffService,
  StaffAreaDto, StationDto,
  STAFF_APPLICATION_STATUS_LABEL, STAFF_APPLICATION_STATUS_CHIP,
} from 'shared';

type StatusFilter = 'all' | 'pending' | 'accepted' | 'rejected';

@Component({
  selector: 'app-staffing',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    MatTooltipModule,
  ],
  templateUrl: './staffing.component.html',
  styleUrl: './staffing.component.scss',
})
export class StaffingComponent {
  private readonly svc           = inject(StaffService);
  private readonly conventionSvc = inject(ConventionService);
  private readonly router        = inject(Router);
  readonly editionCtx            = inject(EditionContextService);

  readonly TOOLTIP = TOOLTIP;

  readonly loading     = signal(true);
  readonly saving      = signal(false);
  readonly error       = signal<string | null>(null);

  readonly edition = signal<EditionDto | null>(null);

  readonly applications       = signal<StaffApplicationSummaryDto[]>([]);
  readonly applicationsLoaded = signal(false);
  readonly statusFilter       = signal<StatusFilter>('pending');

  readonly filteredApplications = computed(() => {
    const filter = this.statusFilter();
    const apps   = this.applications();
    switch (filter) {
      case 'pending':  return apps.filter(a => a.status === 'Received' || a.status === 'UnderReview');
      case 'accepted': return apps.filter(a => a.status === 'Confirmed' || a.status === 'Assigned');
      case 'rejected': return apps.filter(a => a.status === 'Rejected');
      default:         return apps;
    }
  });

  readonly pendingCount  = computed(() => this.applications().filter(a => a.status === 'Received' || a.status === 'UnderReview').length);
  readonly acceptedCount = computed(() => this.applications().filter(a => a.status === 'Confirmed' || a.status === 'Assigned').length);
  readonly rejectedCount = computed(() => this.applications().filter(a => a.status === 'Rejected').length);

  constructor() {
    effect(() => {
      const summary = this.editionCtx.activeEdition();
      if (!summary) { this.loading.set(false); return; }
      this.loadEdition(summary.id);
    });
  }

  private loadEdition(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.applicationsLoaded.set(false);

    this.conventionSvc.getEdition(editionId).subscribe({
      next: ed => { this.edition.set(ed); this.loading.set(false); },
      error: () => { this.error.set('Kunde inte ladda upplagan.'); this.loading.set(false); },
    });
  }

  get staffAreas(): StaffAreaDto[] {
    return this.edition()?.staffAreas ?? [];
  }

  stationCount(areaId: string): number {
    return (this.edition()?.stations ?? []).filter(s => s.staffAreaId === areaId).length;
  }

  navigateToArea(areaId: string): void {
    this.router.navigate(['/staffing/area', areaId]);
  }

  // ── Ansökningar ──────────────────────────────────────────────────────────

  onApplicationsTabSelected(): void {
    if (this.applicationsLoaded()) return;
    this.loadApplications();
  }

  private loadApplications(): void {
    const summary = this.editionCtx.activeEdition();
    if (!summary) return;
    this.svc.listStaffApplications(summary.id).subscribe({
      next: apps => { this.applications.set(apps); this.applicationsLoaded.set(true); },
      error: () => this.error.set('Kunde inte hämta ansökningar.'),
    });
  }

  private reloadApplications(): void {
    const summary = this.editionCtx.activeEdition();
    if (!summary) return;
    this.svc.listStaffApplications(summary.id).subscribe({
      next: apps => this.applications.set(apps),
      error: () => this.error.set('Kunde inte uppdatera ansökningslistan.'),
    });
  }

  accept(app: StaffApplicationSummaryDto): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.acceptApplication(app.id).subscribe({
      next: () => { this.saving.set(false); this.reloadApplications(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte acceptera ansökan.'); },
    });
  }

  reject(app: StaffApplicationSummaryDto): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.rejectApplication(app.id).subscribe({
      next: () => { this.saving.set(false); this.reloadApplications(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte avslå ansökan.'); },
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

  stationName(stationId: string): string {
    return this.edition()?.stations?.find((s: StationDto) => s.id === stationId)?.name ?? stationId;
  }

  formatAvailability(start: string, end: string): string {
    const s = new Date(start);
    const e = new Date(end);
    const sameDay = s.toDateString() === e.toDateString();
    const dateStr = s.toLocaleDateString('sv-SE', { month: 'short', day: 'numeric' });
    const sTime   = s.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });
    const eTime   = e.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });
    return sameDay
      ? `${dateStr} ${sTime}–${eTime}`
      : `${dateStr} ${sTime} – ${e.toLocaleDateString('sv-SE', { month: 'short', day: 'numeric' })} ${eTime}`;
  }

}
