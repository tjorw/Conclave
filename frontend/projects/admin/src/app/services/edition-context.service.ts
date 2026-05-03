import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ConventionContextService, ConventionService, EditionSummaryDto } from 'shared';

@Injectable({ providedIn: 'root' })
export class EditionContextService {
  private readonly conventionService = inject(ConventionService);
  private readonly conventionContext = inject(ConventionContextService);

  private readonly _editions = signal<EditionSummaryDto[]>([]);
  private readonly _activeId = signal<string | null>(null);
  private readonly _loading = signal(false);
  private loadPromise: Promise<void> | null = null;

  readonly editions = this._editions.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly publicActiveEditionId = computed(() => this.conventionContext.convention()?.activeEditionId ?? null);

  readonly publicActiveEdition = computed<EditionSummaryDto | null>(() => {
    const id = this.publicActiveEditionId();
    return id ? this._editions().find(e => e.id === id) ?? null : null;
  });

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

    this.loadPromise = this.conventionContext.load()
      .then(() => firstValueFrom(this.conventionService.listEditions()))
      .then(editions => {
        this._editions.set(editions);

        const activeEditionId = this.conventionContext.convention()?.activeEditionId ?? null;
        this._activeId.set(editions.some(e => e.id === activeEditionId) ? activeEditionId : null);
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
  }

  setPublicActive(editionId: string): void {
    this.conventionContext.setActiveEditionId(editionId);
  }
}
