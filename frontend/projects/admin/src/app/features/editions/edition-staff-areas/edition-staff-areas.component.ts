import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ConventionService, EditionDto, PersonDto, StaffAreaDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../labels/pages.labels';
import { TOOLTIP } from '../../../labels/ui.labels';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { nextSort, sortBy, sortIcon, SortState } from '../../../shared/sort-utils';

type SortKey = 'name' | 'description' | 'responsible' | 'stations';

@Component({
  selector: 'app-edition-staff-areas',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './edition-staff-areas.component.html',
  styleUrl: './edition-staff-areas.component.scss',
})
export class EditionStaffAreasComponent implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly svc    = inject(ConventionService);
  private readonly dialog = inject(MatDialog);

  readonly edition = signal<EditionDto | null>(null);
  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);
  readonly saving  = signal(false);
  readonly sort    = signal<SortState<SortKey>>({ key: 'name', direction: 'asc' });

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
    this.svc.listPersons().subscribe({ next: p => this.persons.set(p.filter(x => x.isActive)) });
  }

  private reload(): void {
    this.svc.getEdition(this.edition()!.id).subscribe({ next: e => this.edition.set(e) });
  }

  personName(id: string): string {
    return this.persons().find(p => p.id === id)?.name ?? id;
  }

  stationsForArea(area: StaffAreaDto): { id: string; name: string }[] {
    return this.edition()?.stations.filter(s => s.staffAreaId === area.id) ?? [];
  }

  sortedAreas(areas: StaffAreaDto[]): StaffAreaDto[] {
    return sortBy(areas, this.sort(), {
      name:        a => a.name,
      description: a => a.description ?? '',
      responsible: a => this.personName(a.responsibleId),
      stations:    a => this.stationsForArea(a).length,
    });
  }

  setSort(key: SortKey): void { this.sort.set(nextSort(this.sort(), key)); }
  sortIcon(key: SortKey): string { return sortIcon(this.sort(), key); }

  openDetail(areaId: string): void {
    void this.router.navigate([areaId], { relativeTo: this.route });
  }

  openNew(): void {
    void this.router.navigate(['new'], { relativeTo: this.route });
  }

  delete(area: StaffAreaDto): void {
    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: { title: this.PAGE.deleteStaffAreaTitle, message: this.PAGE.deleteStaffAreaMessage(area.name) },
        width: '400px',
      })
      .afterClosed()
      .pipe(map(r => r === true))
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.saving.set(true);
        this.svc.removeStaffArea(this.edition()!.id, area.id).subscribe({
          next: () => { this.reload(); this.saving.set(false); },
          error: (err) => { this.error.set(toContextErrorMessage(err, ERROR.deleteStaffArea)); this.saving.set(false); },
        });
      });
  }
}
