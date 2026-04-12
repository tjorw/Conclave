import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { FeedService, EditionFeedDto } from 'shared';

@Injectable({ providedIn: 'root' })
export class EditionService {
  private readonly feedSvc = inject(FeedService);

  readonly edition        = signal<EditionFeedDto | null>(null);
  readonly conventionName = signal('Conclave');
  readonly editionYear    = signal('');
  readonly editionId      = signal<string | null>(null);

  async load(): Promise<void> {
    try {
      const ed = await firstValueFrom(this.feedSvc.getActiveEdition());
      this.edition.set(ed);
      this.conventionName.set(ed.name);
      this.editionYear.set(new Date(ed.startDate).getFullYear().toString());
      this.editionId.set(ed.id);
    } catch {
      // Ingen aktiv upplaga konfigurerad – fortsätt med standardvärden
    }
  }
}
