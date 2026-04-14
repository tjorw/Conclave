import { Component, computed, effect, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, EditionOrganiserDto } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';

@Component({
  selector: 'app-edition-organisers',
  standalone: true,
  imports: [
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './edition-organisers.component.html',
  styleUrl: './edition-organisers.component.scss',
})
export class EditionOrganisersComponent {
  private readonly svc = inject(ConventionService);
  readonly editionContext = inject(EditionContextService);

  readonly organisers = signal<EditionOrganiserDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchQuery = signal('');

  constructor() {
    effect(() => {
      const edition = this.editionContext.activeEdition();
      if (edition) {
        this.load(edition.id);
      } else {
        this.organisers.set([]);
        this.loading.set(false);
      }
    });
  }

  private load(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.listEditionOrganisers(editionId).subscribe({
      next: o => { this.organisers.set(o); this.loading.set(false); },
      error: () => { this.error.set('Kunde inte hämta arrangörer.'); this.loading.set(false); },
    });
  }

  readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.organisers() : this.organisers().filter(
      o => o.personName.toLowerCase().includes(q) || o.eventTitle.toLowerCase().includes(q)
    );
  });

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }
}
