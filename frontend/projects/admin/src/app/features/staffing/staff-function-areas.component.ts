import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, EditionDto, PersonDto, StaffAreaDto } from 'shared';
import { ERROR } from '../../labels/errors.labels';
import { EditionContextService } from '../../services/edition-context.service';
import { createSortController, sortBy } from '../../shared/sort-utils';

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
  private readonly route = inject(ActivatedRoute);
  private readonly conventionSvc = inject(ConventionService);
  readonly editionCtx = inject(EditionContextService);

  readonly edition = signal<EditionDto | null>(null);
  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly sort = createSortController<SortKey>({ key: 'name', direction: 'asc' });
  readonly routeEditionId = this.route.snapshot.paramMap.get('id');

  constructor() {
    if (this.routeEditionId) {
      this.editionCtx.setActive(this.routeEditionId);
    }

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
    return sortBy(areas, this.sort.state(), {
      name: a => a.name,
      description: a => a.description ?? '',
      responsible: a => this.personName(a.responsibleId),
      stations: a => this.stationsForArea(a).length,
    });
  });



  openDetail(areaId: string): void {
    const editionId = this.editionCtx.activeEdition()?.id;
    if (!editionId) return;
    void this.router.navigate(['/editions', editionId, 'staffing', 'function-areas', areaId]);
  }
}
