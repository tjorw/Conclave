import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FeedService, EventFeedDto, SessionFeedDto, REGISTRATION_KIND_LABEL } from 'shared';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './event-detail.component.html',
  styleUrl: './event-detail.component.scss',
})
export class EventDetailComponent implements OnInit {
  private readonly route   = inject(ActivatedRoute);
  private readonly feedSvc = inject(FeedService);

  readonly loading  = signal(true);
  readonly error    = signal<string | null>(null);
  readonly event    = signal<EventFeedDto | null>(null);

  // Expanderade sessioner
  readonly expandedSessions = signal<Set<string>>(new Set());

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.feedSvc.getEvent(id).subscribe({
      next: ev  => { this.event.set(ev); this.loading.set(false); },
      error: () => { this.error.set('Evenemanget hittades inte.'); this.loading.set(false); },
    });
  }

  toggleSession(id: string): void {
    this.expandedSessions.update(set => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  sessionTimeLabel(s: SessionFeedDto): string {
    const start = new Date(s.start);
    const end   = new Date(s.end);
    return start.toLocaleDateString('sv-SE', { weekday: 'long', day: 'numeric', month: 'long' })
      + ', ' + start.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' })
      + '–' + end.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });
  }

  registrationLabel(type: string): string {
    return REGISTRATION_KIND_LABEL[type] ?? type;
  }
}
