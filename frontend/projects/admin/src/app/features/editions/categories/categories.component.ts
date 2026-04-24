import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CategoryDto, ConventionService, EditionDto, PersonDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../labels/pages.labels';
import { TOOLTIP } from '../../../labels/ui.labels';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { nextSort, sortBy, sortIcon, SortState } from '../../../shared/sort-utils';

type SortKey = 'name' | 'description' | 'responsible';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss',
})
export class CategoriesComponent implements OnInit {
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

  sortedCategories(categories: CategoryDto[]): CategoryDto[] {
    return sortBy(categories, this.sort(), {
      name:        c => c.name,
      description: c => c.description ?? '',
      responsible: c => this.personName(c.responsibleId),
    });
  }

  setSort(key: SortKey): void { this.sort.set(nextSort(this.sort(), key)); }
  sortIcon(key: SortKey): string { return sortIcon(this.sort(), key); }

  openDetail(categoryId: string): void {
    void this.router.navigate([categoryId], { relativeTo: this.route });
  }

  openNew(): void {
    void this.router.navigate(['new'], { relativeTo: this.route });
  }

  delete(category: CategoryDto): void {
    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: { title: this.PAGE.deleteCategoryTitle, message: this.PAGE.deleteCategoryMessage(category.name) },
        width: '400px',
      })
      .afterClosed()
      .pipe(map(r => r === true))
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.saving.set(true);
        this.svc.removeCategory(this.edition()!.id, category.id).subscribe({
          next: () => { this.reload(); this.saving.set(false); },
          error: (err) => { this.error.set(toContextErrorMessage(err, ERROR.deleteCategory)); this.saving.set(false); },
        });
      });
  }
}
