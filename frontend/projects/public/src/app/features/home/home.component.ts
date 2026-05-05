import { Component, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { EditionContentService, EDITION_CONTENT_KEYS, EventSummaryFeedDto } from 'shared';
import { EditionService } from '../../services/edition.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  readonly editionSvc   = inject(EditionService);
  private readonly contentSvc = inject(EditionContentService);

  private readonly contentMap = signal<Record<string, string>>({});

  readonly featuredEvents = computed<EventSummaryFeedDto[]>(() =>
    (this.editionSvc.edition()?.events ?? []).slice(0, 3)
  );

  readonly heroTitle = computed(() =>
    this.contentMap()[EDITION_CONTENT_KEYS.heroTitle] || this.editionSvc.conventionName()
  );

  readonly heroIngress = computed(() =>
    this.contentMap()[EDITION_CONTENT_KEYS.heroIngress] || null
  );

  readonly ctaVisitorLabel = computed(() =>
    this.contentMap()[EDITION_CONTENT_KEYS.ctaVisitorLabel] || 'Bli besökare'
  );

  readonly ctaOrganiserLabel = computed(() =>
    this.contentMap()[EDITION_CONTENT_KEYS.ctaOrganiserLabel] || 'Arrangera ett evenemang'
  );

  readonly ctaStaffLabel = computed(() =>
    this.contentMap()[EDITION_CONTENT_KEYS.ctaStaffLabel] || 'Bli funktionär'
  );

  constructor() {
    effect(() => {
      const editionId = this.editionSvc.editionId();
      if (!editionId) return;
      this.contentSvc.getContent(editionId).subscribe({
        next: items => {
          const map: Record<string, string> = {};
          for (const item of items) {
            if (item.value) map[item.key] = item.value;
          }
          this.contentMap.set(map);
        },
        error: () => { /* fallback-texter används */ },
      });
    });
  }

  readonly firstSession = (event: EventSummaryFeedDto): string => {
    const s = event.sessions[0];
    if (!s) return '';
    return new Date(s.start).toLocaleDateString('sv-SE', { weekday: 'short', hour: '2-digit', minute: '2-digit' });
  };
}
