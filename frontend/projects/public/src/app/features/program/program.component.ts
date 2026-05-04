import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { EditionService } from '../../services/edition.service';
import { CategoryFeedDto, EventSummaryFeedDto } from 'shared';
import { StripMarkdownPipe } from '../../pipes/strip-markdown.pipe';

@Component({
  selector: 'app-program',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, StripMarkdownPipe],
  templateUrl: './program.component.html',
  styleUrl: './program.component.scss',
})
export class ProgramComponent {
  readonly editionSvc = inject(EditionService);

  readonly selectedDay      = signal<string>('alla');
  readonly selectedCategory = signal<string | null>(null);
  readonly selectedTag      = signal<string | null>(null);

  // Unika dagar extraherade från alla sessioner
  readonly availableDays = computed<string[]>(() => {
    const days = new Set<string>();
    for (const ev of (this.editionSvc.edition()?.events ?? [])) {
      for (const s of ev.sessions) {
        days.add(this.dateOnly(s.start));
      }
    }
    return Array.from(days).sort();
  });

  readonly categories = computed<CategoryFeedDto[]>(
    () => this.editionSvc.edition()?.categories ?? []
  );

  readonly availableTags = computed<string[]>(() => {
    const tags = new Set<string>();
    for (const ev of (this.editionSvc.edition()?.events ?? [])) {
      for (const tag of (ev.programTags ?? [])) {
        tags.add(tag);
      }
    }

    return Array.from(tags).sort((a, b) => a.localeCompare(b, 'sv-SE'));
  });

  readonly filteredEvents = computed<EventSummaryFeedDto[]>(() => {
    const events = this.editionSvc.edition()?.events ?? [];
    const day  = this.selectedDay();
    const cat  = this.selectedCategory();
    const tag  = this.selectedTag();

    return events.filter(ev => {
      const matchesCat = !cat || ev.categoryId === cat;
      const matchesDay = day === 'alla' || ev.sessions.some(s =>
        this.dateOnly(s.start) === day
      );
      const matchesTag = !tag || (ev.programTags ?? []).includes(tag);
      return matchesCat && matchesDay && matchesTag;
    });
  });

  readonly loading = computed(() => !this.editionSvc.edition());

  dayLabel(iso: string): string {
    const [year, month, day] = iso.split('-').map(Number);
    return new Date(year, month - 1, day).toLocaleDateString('sv-SE', { weekday: 'long' });
  }

  firstSessionLabel(event: EventSummaryFeedDto): string {
    const day  = this.selectedDay();
    const sessions = day === 'alla'
      ? event.sessions
      : event.sessions.filter(s => this.dateOnly(s.start) === day);
    const s = sessions[0] ?? event.sessions[0];
    if (!s) return '';
    const d = new Date(s.start);
    return d.toLocaleDateString('sv-SE', { weekday: 'short' }) + ' '
      + d.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' })
      + '–'
      + new Date(s.end).toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });
  }

  toggleCategory(id: string): void {
    this.selectedCategory.update(c => c === id ? null : id);
  }

  toggleTag(tag: string): void {
    this.selectedTag.update(current => current === tag ? null : tag);
  }

  private dateOnly(isoString: string): string {
    const d = new Date(isoString);
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
