import { Component, computed, effect, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, EditionResponsibleDto } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';

@Component({
  selector: 'app-edition-responsibles',
  standalone: true,
  imports: [
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './edition-responsibles.component.html',
  styleUrl: './edition-responsibles.component.scss',
})
export class EditionResponsiblesComponent {
  private readonly svc = inject(ConventionService);
  readonly editionContext = inject(EditionContextService);

  readonly responsibles = signal<EditionResponsibleDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchQuery = signal('');

  constructor() {
    effect(() => {
      const edition = this.editionContext.activeEdition();
      if (edition) {
        this.load(edition.id);
      } else {
        this.responsibles.set([]);
        this.loading.set(false);
      }
    });
  }

  private load(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.listEditionResponsibles(editionId).subscribe({
      next: r => { this.responsibles.set(r); this.loading.set(false); },
      error: () => { this.error.set('Kunde inte hämta ansvariga.'); this.loading.set(false); },
    });
  }

  readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.responsibles() : this.responsibles().filter(
      r => r.position.toLowerCase().includes(q) || (r.personName ?? '').toLowerCase().includes(q)
    );
  });

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }
}
