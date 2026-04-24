import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ConventionService, EditionDto, VenueDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../labels/pages.labels';
import { TOOLTIP } from '../../../labels/ui.labels';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { nextSort, sortBy, sortIcon, SortState } from '../../../shared/sort-utils';

type VenueSortKey = 'name' | 'building' | 'description';

@Component({
  selector: 'app-venues',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './venues.component.html',
  styleUrl: './venues.component.scss',
})
export class VenuesComponent implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly svc    = inject(ConventionService);
  private readonly dialog = inject(MatDialog);

  readonly edition = signal<EditionDto | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);
  readonly saving  = signal(false);
  readonly sort    = signal<SortState<VenueSortKey>>({ key: 'name', direction: 'asc' });

  readonly PAGE    = EDITION_DETAIL;
  readonly TOOLTIP = TOOLTIP;

  ngOnInit(): void {
    this.route.paramMap.pipe(map(p => p.get('id')!)).subscribe(id => this.loadData(id));
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.svc.getEdition(editionId).subscribe({
      next: e => { this.edition.set(e); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchEdition); this.loading.set(false); },
    });
  }

  private reload(): void {
    this.svc.getEdition(this.edition()!.id).subscribe({ next: e => this.edition.set(e) });
  }

  sortedVenues(venues: VenueDto[]): VenueDto[] {
    return sortBy(venues, this.sort(), {
      name:        v => v.name,
      building:    v => v.building,
      description: v => v.description ?? '',
    });
  }

  setSort(key: VenueSortKey): void { this.sort.set(nextSort(this.sort(), key)); }
  sortIcon(key: VenueSortKey): string { return sortIcon(this.sort(), key); }

  openDetail(venueId: string): void {
    void this.router.navigate([venueId], { relativeTo: this.route });
  }

  openNew(): void {
    void this.router.navigate(['new'], { relativeTo: this.route });
  }

  delete(venue: VenueDto): void {
    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: { title: this.PAGE.deleteVenueTitle, message: this.PAGE.deleteVenueMessage(venue.name) },
        width: '400px',
      })
      .afterClosed()
      .pipe(map(r => r === true))
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.saving.set(true);
        this.svc.removeVenue(this.edition()!.id, venue.id).subscribe({
          next: () => { this.reload(); this.saving.set(false); },
          error: (err) => { this.error.set(toContextErrorMessage(err, ERROR.deleteVenue)); this.saving.set(false); },
        });
      });
  }
}
