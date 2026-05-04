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
  ProgramTagDefinitionDto,
  PersonDto,
  EVENT_STATUS_LABEL,
  EVENT_STATUS_CHIP,
  toErrorMessage,
} from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { ACTION, FIELD, TOOLTIP } from '../../labels/ui.labels';
import { createSortController, sortBy } from '../../shared/sort-utils';

type EventSortKey = 'title' | 'category' | 'organiser' | 'sessions' | 'comments' | 'status';

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
  readonly programTagDefinitions = signal<ProgramTagDefinitionDto[]>([]);
  readonly loading    = signal(false);
  readonly saving     = signal(false);
  readonly error      = signal<string | null>(null);
  readonly filter     = signal<string>('UnderReview');
  readonly showCreateForm = signal(false);
  readonly sort = createSortController<EventSortKey>({ key: 'title', direction: 'asc' });

  readonly createForm = this.fb.group({
    categoryId:       ['', Validators.required],
    leadOrganiserId:  ['', Validators.required],
    programTags:      this.fb.control<string[]>([], { nonNullable: true }),
  });

  readonly filteredEvents = computed(() => {
    const f = this.filter();
    if (f === 'PendingComments') {
      return this.events().filter(e => e.pendingCommentCount > 0);
    }
    return f === 'All' ? this.events() : this.events().filter(e => e.status === f);
  });

  readonly sortedFilteredEvents = computed(() =>
    sortBy(this.filteredEvents(), this.sort.state(), {
      title: e => e.title ?? '',
      category: e => e.categoryName ?? '',
      organiser: e => e.leadOrganiserName ?? '',
      sessions: e => e.sessionCount,
      comments: e => e.pendingCommentCount,
      status: e => this.statusLabel(e.status),
    })
  );

  readonly counts = computed(() => {
    const all = this.events();
    return {
      all:         all.length,
      underReview: all.filter(e => e.status === 'UnderReview').length,
      published:   all.filter(e => e.status === 'Published').length,
      draft:       all.filter(e => e.status === 'Draft').length,
      cancelled:   all.filter(e => e.status === 'Cancelled').length,
      pendingComments: all.filter(e => e.pendingCommentCount > 0).length,
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
      error: () => { this.error.set(ERROR.fetchEvents); this.loading.set(false); },
    });
  }

  private loadSupportData(editionId: string): void {
    this.conventionSvc.getEdition(editionId).subscribe({
      next: edition => {
        this.categories.set(edition.categories);
        this.programTagDefinitions.set(edition.programTagDefinitions ?? []);
      },
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
    const { categoryId, leadOrganiserId, programTags } = this.createForm.getRawValue();
    this.saving.set(true);
    this.eventSvc.createEvent(edition.id, categoryId!, leadOrganiserId!, programTags ?? []).subscribe({
      next: ({ id }) => {
        this.saving.set(false);
        this.createForm.reset();
        this.showCreateForm.set(false);
        this.loadEvents(edition.id);
        this.router.navigate(['/events', id]);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.createEvent));
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
        this.error.set(toErrorMessage(err, ERROR.cancelEvent));
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
