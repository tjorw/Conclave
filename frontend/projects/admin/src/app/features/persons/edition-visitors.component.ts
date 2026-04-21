import { Component, computed, effect, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, EditionVisitorDto } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { FIELD, PLACEHOLDER } from '../../labels/ui.labels';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type VisitorSortKey = 'name' | 'email' | 'phone';

@Component({
  selector: 'app-edition-visitors',
  standalone: true,
  imports: [
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './edition-visitors.component.html',
  styleUrl: './edition-visitors.component.scss',
})
export class EditionVisitorsComponent {
  private readonly svc = inject(ConventionService);
  readonly editionContext = inject(EditionContextService);

  readonly FIELD       = FIELD;
  readonly PLACEHOLDER = PLACEHOLDER;

  readonly visitors = signal<EditionVisitorDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchQuery = signal('');
  readonly sort = signal<SortState<VisitorSortKey>>({ key: 'name', direction: 'asc' });

  constructor() {
    effect(() => {
      const edition = this.editionContext.activeEdition();
      if (edition) {
        this.load(edition.id);
      } else {
        this.visitors.set([]);
        this.loading.set(false);
      }
    });
  }

  private load(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.listEditionVisitors(editionId).subscribe({
      next: v => { this.visitors.set(v); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchVisitors); this.loading.set(false); },
    });
  }

  readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.visitors() : this.visitors().filter(
      v => v.personName.toLowerCase().includes(q) || v.email.toLowerCase().includes(q)
    );
  });

  readonly sortedFiltered = computed(() =>
    sortBy(this.filtered(), this.sort(), {
      name: v => v.personName,
      email: v => v.email,
      phone: v => v.phone ?? '',
    })
  );

  setSort(key: VisitorSortKey): void {
    this.sort.set(nextSort(this.sort(), key));
  }

  sortIcon(key: VisitorSortKey): string {
    return sortIcon(this.sort(), key);
  }

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }
}
