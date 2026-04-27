import { computed, inject, Injectable, signal } from '@angular/core';
import { ConventionService, EditionSummaryDto } from 'shared';

@Injectable({ providedIn: 'root' })
export class EditionContextService {
  private readonly conventionService = inject(ConventionService);

  private readonly _editions = signal<EditionSummaryDto[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal(false);

  readonly editions = this._editions.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  readonly activeEdition = computed<EditionSummaryDto | null>(() => {
    const editions = this._editions();
    if (!editions.length) return null;
    return (
      editions.find(e => e.status === 'Published') ??
      [...editions].sort((a, b) => b.start.localeCompare(a.start))[0]
    );
  });

  load(): void {
    if (this._editions().length > 0 || this._loading()) return;
    this._loading.set(true);
    this._error.set(false);
    this.conventionService.listEditions().subscribe({
      next: editions => {
        this._editions.set(editions);
        this._loading.set(false);
      },
      error: () => {
        this._loading.set(false);
        this._error.set(true);
      },
    });
  }
}
