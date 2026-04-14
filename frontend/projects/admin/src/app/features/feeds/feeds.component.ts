import { Component, computed, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ENVIRONMENT } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { TOOLTIP } from '../../labels/ui.labels';

@Component({
  selector: 'app-feeds',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, MatTooltipModule],
  templateUrl: './feeds.component.html',
  styleUrl: './feeds.component.scss',
})
export class FeedsComponent {
  private readonly env    = inject(ENVIRONMENT);
  readonly editionCtx     = inject(EditionContextService);

  readonly TOOLTIP = TOOLTIP;

  private readonly base = `${this.env.apiBaseUrl}/feed/${this.env.conventionId}`;

  readonly activeEditionUrl = `${this.base}/active-edition`;

  readonly editionUrls = computed(() =>
    this.editionCtx.editions().map(ed => ({
      name: ed.name,
      url:  `${this.base}/editions/${ed.id}`,
    }))
  );

  copy(url: string): void {
    navigator.clipboard.writeText(url);
  }
}
