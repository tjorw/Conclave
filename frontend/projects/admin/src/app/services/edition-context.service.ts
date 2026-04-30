import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ConventionService, EditionSummaryDto } from 'shared';

@Injectable({ providedIn: 'root' })
export class EditionContextService {
  private readonly conventionService = inject(ConventionService);
  private readonly STORAGE_KEY = 'active_edition_id';

  private readonly _editions = signal<EditionSummaryDto[]>([]);
  private readonly _activeId = signal<string | null>(sessionStorage.getItem(this.STORAGE_KEY));
  private readonly _loading = signal(false);
  private loadPromise: Promise<void> | null = null;

  readonly editions = this._editions.asReadonly();
  readonly loading = this._loading.asReadonly();

  readonly activeEdition = computed<EditionSummaryDto | null>(() => {
    const editions = this._editions();
    if (!editions.length) return null;

    const id = this._activeId();
    if (id) {
      const found = editions.find(e => e.id === id);
      if (found) return found;
    }

    return editions.find(e => e.status === 'Published')
      ?? [...editions].sort((a, b) => b.start.localeCompare(a.start))[0];
  });

  load(): Promise<void> {
    if (this._editions().length > 0) return Promise.resolve();
    if (this.loadPromise) return this.loadPromise;
    return this.fetch();
  }

  reload(): Promise<void> {
    return this.fetch();
  }

  private fetch(): Promise<void> {
    this._loading.set(true);

    this.loadPromise = firstValueFrom(this.conventionService.listEditions())
      .then(editions => {
        this._editions.set(editions);

        const stored = this._activeId();
        if (stored && !editions.find(e => e.id === stored)) {
          this._activeId.set(null);
          sessionStorage.removeItem(this.STORAGE_KEY);
        }
      })
      .catch(() => {
        // Keep the route usable; feature pages can show their own empty/error state.
      })
      .finally(() => {
        this._loading.set(false);
        this.loadPromise = null;
      });

    return this.loadPromise;
  }

  setActive(editionId: string): void {
    this._activeId.set(editionId);
    sessionStorage.setItem(this.STORAGE_KEY, editionId);
  }
}
