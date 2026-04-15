import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EditionService } from '../../../services/edition.service';
import { EventService, EventSummaryDto, EVENT_STATUS_LABEL, EVENT_STATUS_CHIP } from 'shared';

@Component({
  selector: 'app-my-events',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './my-events.component.html',
  styleUrl: './my-events.component.scss',
})
export class MyEventsComponent implements OnInit {
  private readonly editionSvc = inject(EditionService);
  private readonly eventSvc   = inject(EventService);

  readonly loading = signal(true);
  readonly events  = signal<EventSummaryDto[]>([]);

  readonly statusLabel = EVENT_STATUS_LABEL;
  readonly statusChip  = EVENT_STATUS_CHIP;

  ngOnInit(): void {
    const editionId = this.editionSvc.editionId();
    if (!editionId) {
      this.loading.set(false);
      return;
    }
    this.eventSvc.getMyEvents(editionId).subscribe({
      next: ev => { this.events.set(ev); this.loading.set(false); },
      error: ()  => this.loading.set(false),
    });
  }
}
