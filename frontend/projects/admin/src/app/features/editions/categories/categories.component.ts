import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CategoryDto, ConventionService, EditionDto, PersonDto } from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { nextSort, sortBy, sortIcon, SortState } from '../../../shared/sort-utils';

type SortKey = 'name' | 'description' | 'responsible';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss',
})
export class CategoriesComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly svc = inject(ConventionService);

  readonly edition = signal<EditionDto | null>(null);
  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly sort = signal<SortState<SortKey>>({ key: 'name', direction: 'asc' });

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
    this.svc
      .listPersons()
      .subscribe({ next: (p) => this.persons.set(p.filter((x) => x.isActive)) });
  }

  personName(id: string): string {
    return this.persons().find((p) => p.id === id)?.name ?? id;
  }

  sortedCategories(categories: CategoryDto[]): CategoryDto[] {
    return sortBy(categories, this.sort(), {
      name: (c) => c.name,
      description: (c) => c.publicDescription ?? '',
      responsible: (c) => this.personName(c.responsibleId),
    });
  }

  setSort(key: SortKey): void {
    this.sort.set(nextSort(this.sort(), key));
  }
  sortIcon(key: SortKey): string {
    return sortIcon(this.sort(), key);
  }

  openDetail(categoryId: string): void {
    void this.router.navigate([categoryId], { relativeTo: this.route });
  }

  openNew(): void {
    void this.router.navigate(['new'], { relativeTo: this.route });
  }
}
