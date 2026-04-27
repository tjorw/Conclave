import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, EditionDto, PersonDto, StaffAreaDto } from 'shared';
import { ERROR } from '../../labels/errors.labels';
import { EditionContextService } from '../../services/edition-context.service';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type SortKey = 'name' | 'description' | 'responsible' | 'stations';

@Component({
  selector: 'app-staff-function-areas',
  standalone: true,
  imports: [MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './staff-function-areas.component.html',
  styleUrl: './staff-function-areas.component.scss',
})
export class StaffFunctionAreasComponent {
  private readonly router = inject(Router);
  private readonly conventionSvc = inject(ConventionService);
  readonly editionCtx = inject(EditionContextService);

  readonly edition = signal<EditionDto | null>(null);
  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly sort = signal<SortState<SortKey>>({ key: 'name', direction: 'asc' });

  constructor() {
    effect(() => {
      const active = this.editionCtx.activeEdition();
      if (!active) {
        this.edition.set(null);
        return;
      }
      this.loadData(active.id);
    });
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.conventionSvc.getEdition(editionId).subscribe({
      next: e => {
        this.edition.set(e);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(ERROR.fetchEdition);
        this.loading.set(false);
      },
    });
    this.conventionSvc.listPersons().subscribe({
      next: p => this.persons.set(p.filter(x => x.isActive)),
    });
  }

  personName(id: string): string {
    return this.persons().find(p => p.id === id)?.name ?? id;
  }

  stationsForArea(area: StaffAreaDto): string[] {
    return this.edition()?.stations.filter(s => s.staffAreaId === area.id).map(s => s.name) ?? [];
  }

  readonly sortedAreas = computed<StaffAreaDto[]>(() => {
    const areas = this.edition()?.staffAreas ?? [];
    return sortBy(areas, this.sort(), {
      name: a => a.name,
      description: a => a.description ?? '',
      responsible: a => this.personName(a.responsibleId),
      stations: a => this.stationsForArea(a).length,
    });
  });

  setSort(key: SortKey): void {
    this.sort.set(nextSort(this.sort(), key));
  }

  sortIconFor(key: SortKey): string {
    return sortIcon(this.sort(), key);
  }

  openDetail(areaId: string): void {
    void this.router.navigate(['/staff-areas', areaId]);
  }
}
