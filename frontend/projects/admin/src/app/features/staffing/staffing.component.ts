import { Component, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { DatePipe } from '@angular/common';
import { EditionContextService } from '../../services/edition-context.service';
import {
  ConventionService, EditionDto, StaffApplicationSummaryDto, StaffService,
  StaffAreaDto, StationDto,
} from 'shared';

@Component({
  selector: 'app-staffing',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTabsModule,
  ],
  templateUrl: './staffing.component.html',
  styleUrl: './staffing.component.scss',
})
export class StaffingComponent {
  private readonly svc           = inject(StaffService);
  private readonly conventionSvc = inject(ConventionService);
  private readonly router        = inject(Router);
  readonly editionCtx            = inject(EditionContextService);

  readonly loading     = signal(true);
  readonly error       = signal<string | null>(null);

  readonly edition = signal<EditionDto | null>(null);

  readonly applications       = signal<StaffApplicationSummaryDto[]>([]);
  readonly applicationsLoaded = signal(false);

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

  onApplicationsTabSelected(): void {
    if (this.applicationsLoaded()) return;
    const summary = this.editionCtx.activeEdition();
    if (!summary) return;
    this.svc.listStaffApplications(summary.id).subscribe({
      next: apps => { this.applications.set(apps); this.applicationsLoaded.set(true); },
      error: () => this.error.set('Kunde inte hämta ansökningar.'),
    });
  }

  applicationStatusLabel(status: string): string {
    const map: Record<string, string> = { Received: 'Mottagen', Accepted: 'Accepterad', Rejected: 'Avslagen' };
    return map[status] ?? status;
  }

  stationName(stationId: string): string {
    return this.edition()?.stations?.find((s: StationDto) => s.id === stationId)?.name ?? stationId;
  }
}
