import { computed, inject, Injectable, signal } from '@angular/core';
import { ConventionService, EditionSummaryDto } from 'shared';

@Injectable({ providedIn: 'root' })
export class EditionContextService {
  private readonly conventionService = inject(ConventionService);
  private readonly STORAGE_KEY = 'active_edition_id';

  private readonly _editions = signal<EditionSummaryDto[]>([]);
  private readonly _activeId = signal<string | null>(sessionStorage.getItem(this.STORAGE_KEY));
  private readonly _loading = signal(false);

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

    // Default: senaste upplagan per startdatum
    return [...editions].sort((a, b) => b.start.localeCompare(a.start))[0];
  });

  load(): void {
    if (this._editions().length > 0 || this._loading()) return;
    this._loading.set(true);
    this.conventionService.listEditions().subscribe({
      next: editions => {
        this._editions.set(editions);
        this._loading.set(false);
        // Rensa sparat id om det inte längre är giltigt
        const stored = this._activeId();
        if (stored && !editions.find(e => e.id === stored)) {
          this._activeId.set(null);
          sessionStorage.removeItem(this.STORAGE_KEY);
        }
      },
      error: () => this._loading.set(false),
    });
  }

  setActive(editionId: string): void {
    this._activeId.set(editionId);
    sessionStorage.setItem(this.STORAGE_KEY, editionId);
  }
}
