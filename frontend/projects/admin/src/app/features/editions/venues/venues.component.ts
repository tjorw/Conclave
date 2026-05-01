import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, EditionDto, VenueDto } from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { createSortController, sortBy } from '../../../shared/sort-utils';
import { HelpPanelComponent } from '../../../../help/components/help-panel/help-panel.component';

type VenueSortKey = 'name' | 'building' | 'description';

@Component({
  selector: 'app-venues',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule, HelpPanelComponent],
  templateUrl: './venues.component.html',
  styleUrl: './venues.component.scss',
})
export class VenuesComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly svc = inject(ConventionService);

  readonly edition = signal<EditionDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly sort = createSortController<VenueSortKey>({ key: 'name', direction: 'asc' });

  ngOnInit(): void {
    this.route.paramMap.pipe(map((p) => p.get('id')!)).subscribe((id) => this.loadData(id));
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.svc.getEdition(editionId).subscribe({
      next: (e) => {
        this.edition.set(e);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(ERROR.fetchEdition);
        this.loading.set(false);
      },
    });
  }

  sortedVenues(venues: VenueDto[]): VenueDto[] {
    return sortBy(venues, this.sort.state(), {
      name: (v) => v.name,
      building: (v) => v.building,
      description: (v) => v.description ?? '',
    });
  }


  openDetail(venueId: string): void {
    void this.router.navigate([venueId], { relativeTo: this.route });
  }

  openNew(): void {
    void this.router.navigate(['new'], { relativeTo: this.route });
  }
}
