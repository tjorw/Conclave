import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { ConventionService, EditionDto, StaffAreaDto } from 'shared';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type StaffAreaSortKey = 'name' | 'stations';

@Component({
  selector: 'app-staff-areas',
  standalone: true,
  imports: [
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './staff-areas.component.html',
  styleUrl: './staff-areas.component.scss',
})
export class StaffAreasComponent {
  private readonly conventionSvc = inject(ConventionService);
  private readonly router        = inject(Router);
  readonly editionCtx            = inject(EditionContextService);

  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);
  readonly edition = signal<EditionDto | null>(null);

  readonly staffAreaSort = signal<SortState<StaffAreaSortKey>>({ key: 'name', direction: 'asc' });

  readonly sortedStaffAreas = computed(() =>
    sortBy(this.staffAreas, this.staffAreaSort(), {
      name: area => area.name,
      stations: area => this.stationCount(area.id),
    })
  );

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

    this.conventionSvc.getEdition(editionId).subscribe({
      next: ed => { this.edition.set(ed); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchEdition); this.loading.set(false); },
    });
  }

  get staffAreas(): StaffAreaDto[] {
    return this.edition()?.staffAreas ?? [];
  }

  stationCount(areaId: string): number {
    return (this.edition()?.stations ?? []).filter(s => s.staffAreaId === areaId).length;
  }

  navigateToArea(areaId: string): void {
    this.router.navigate(['/staff-areas', areaId]);
  }

  setStaffAreaSort(key: StaffAreaSortKey): void {
    this.staffAreaSort.set(nextSort(this.staffAreaSort(), key));
  }

  staffAreaSortIcon(key: StaffAreaSortKey): string {
    return sortIcon(this.staffAreaSort(), key);
  }
}
