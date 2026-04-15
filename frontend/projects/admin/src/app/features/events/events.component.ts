import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  CategoryDto,
  ConventionService,
  EventService,
  EventSummaryDto,
  PersonDto,
  EVENT_STATUS_LABEL,
  EVENT_STATUS_CHIP,
} from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ACTION, FIELD, TOOLTIP } from '../../labels/ui.labels';

@Component({
  selector: 'app-events',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
  ],
  templateUrl: './events.component.html',
  styleUrl: './events.component.scss',
})
export class EventsComponent {
  private readonly eventSvc    = inject(EventService);
  private readonly conventionSvc = inject(ConventionService);
  private readonly router      = inject(Router);
  private readonly fb          = inject(FormBuilder);
  readonly editionContext      = inject(EditionContextService);

  readonly ACTION  = ACTION;
  readonly TOOLTIP = TOOLTIP;
  readonly FIELD   = FIELD;

  readonly events     = signal<EventSummaryDto[]>([]);
  readonly persons    = signal<PersonDto[]>([]);
  readonly categories = signal<CategoryDto[]>([]);
  readonly loading    = signal(false);
  readonly saving     = signal(false);
  readonly error      = signal<string | null>(null);
  readonly filter     = signal<string>('UnderReview');
  readonly showCreateForm = signal(false);

  readonly createForm = this.fb.group({
    categoryId:       ['', Validators.required],
    leadOrganiserId:  ['', Validators.required],
  });

  readonly filteredEvents = computed(() => {
    const f = this.filter();
    return f === 'All' ? this.events() : this.events().filter(e => e.status === f);
  });

  readonly counts = computed(() => {
    const all = this.events();
    return {
      all:         all.length,
      underReview: all.filter(e => e.status === 'UnderReview').length,
      published:   all.filter(e => e.status === 'Published').length,
      draft:       all.filter(e => e.status === 'Draft').length,
      cancelled:   all.filter(e => e.status === 'Cancelled').length,
    };
  });

  constructor() {
    effect(() => {
      const edition = this.editionContext.activeEdition();
      if (edition) {
        this.loadEvents(edition.id);
        this.loadSupportData(edition.id);
      }
    });
  }

  private loadEvents(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.eventSvc.listEvents(editionId).subscribe({
      next: events => { this.events.set(events); this.loading.set(false); },
      error: () => { this.error.set('Kunde inte ladda evenemang.'); this.loading.set(false); },
    });
  }

  private loadSupportData(editionId: string): void {
    this.conventionSvc.getEdition(editionId).subscribe({
      next: edition => this.categories.set(edition.categories),
    });
    this.conventionSvc.listPersons().subscribe({
      next: persons => this.persons.set(persons.filter(p => p.isActive)),
    });
  }

  toggleCreateForm(): void {
    this.showCreateForm.update(v => !v);
    if (!this.showCreateForm()) this.createForm.reset();
  }

  create(): void {
    const edition = this.editionContext.activeEdition();
    if (!edition || this.createForm.invalid || this.saving()) return;
    const { categoryId, leadOrganiserId } = this.createForm.getRawValue();
    this.saving.set(true);
    this.eventSvc.createEvent(edition.id, categoryId!, leadOrganiserId!).subscribe({
      next: ({ id }) => {
        this.saving.set(false);
        this.createForm.reset();
        this.showCreateForm.set(false);
        this.loadEvents(edition.id);
        this.router.navigate(['/events', id]);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.detail ?? 'Kunde inte skapa evenemang.');
      },
    });
  }

  cancelEvent(event: EventSummaryDto): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.eventSvc.cancelEvent(event.id).subscribe({
      next: () => {
        this.saving.set(false);
        const edition = this.editionContext.activeEdition();
        if (edition) this.loadEvents(edition.id);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.detail ?? 'Kunde inte ställa in evenemanget.');
      },
    });
  }

  openEvent(id: string): void {
    this.router.navigate(['/events', id]);
  }

  statusLabel(status: string): string {
    return EVENT_STATUS_LABEL[status] ?? status;
  }

  statusChipClass(status: string): string {
    return EVENT_STATUS_CHIP[status] ?? 'chip-grey';
  }
}
