import { Component, computed, inject, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ConventionContextService, ENVIRONMENT } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { TOOLTIP } from '../../labels/ui.labels';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type FeedEditionSortKey = 'name' | 'url';

@Component({
  selector: 'app-feeds',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, MatTooltipModule],
  templateUrl: './feeds.component.html',
  styleUrl: './feeds.component.scss',
})
export class FeedsComponent {
  private readonly env    = inject(ENVIRONMENT);
  private readonly conventionContext = inject(ConventionContextService);
  readonly editionCtx     = inject(EditionContextService);

  readonly TOOLTIP = TOOLTIP;
  readonly editionSort = signal<SortState<FeedEditionSortKey>>({ key: 'name', direction: 'asc' });

  private readonly base = computed(() =>
    `${this.env.apiBaseUrl}/feed/${this.conventionContext.requireConventionId()}`
  );

  readonly activeEditionUrl = computed(() => `${this.base()}/active-edition`);

  readonly editionUrls = computed(() =>
    sortBy(
      this.editionCtx.editions().map(ed => ({
        name: ed.name,
        url:  `${this.base()}/editions/${ed.id}`,
      })),
      this.editionSort(),
      {
        name: item => item.name,
        url: item => item.url,
      }
    )
  );

  copy(url: string): void {
    navigator.clipboard.writeText(url);
  }

  setEditionSort(key: FeedEditionSortKey): void {
    this.editionSort.set(nextSort(this.editionSort(), key));
  }

  editionSortIcon(key: FeedEditionSortKey): string {
    return sortIcon(this.editionSort(), key);
  }
}
