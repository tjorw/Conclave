import { Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { EditionService } from '../../services/edition.service';
import { EventSummaryFeedDto } from 'shared';

@Component({
  selector: 'app-hem',
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './hem.component.html',
  styleUrl: './hem.component.scss',
})
export class HemComponent {
  readonly editionSvc = inject(EditionService);

  readonly featuredEvents = computed<EventSummaryFeedDto[]>(() =>
    (this.editionSvc.edition()?.events ?? []).slice(0, 3)
  );

  readonly firstSession = (event: EventSummaryFeedDto): string => {
    const s = event.sessions[0];
    if (!s) return '';
    return new Date(s.start).toLocaleDateString('sv-SE', { weekday: 'short', hour: '2-digit', minute: '2-digit' });
  };
}
