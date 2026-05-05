import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ConventionService, EditionOrganiserDto, formatTicketPrice, OrganiserTicketAssignmentDto, OrganiserTicketTypeDto, RegistrationService, toErrorMessage } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { FIELD, PLACEHOLDER } from '../../labels/ui.labels';
import { createSortController, sortBy } from '../../shared/sort-utils';

type OrganiserSortKey = 'name' | 'email' | 'event' | 'role';

interface OrganiserPersonRow {
  personId: string;
  personName: string;
  email: string;
  phone: string | null;
  events: {
    eventId: string;
    eventTitle: string;
    role: string;
  }[];
}

@Component({
  selector: 'app-edition-organisers',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './edition-organisers.component.html',
  styleUrl: './edition-organisers.component.scss',
})
export class EditionOrganisersComponent {
  private readonly svc = inject(ConventionService);
  private readonly regSvc = inject(RegistrationService);
  private readonly route = inject(ActivatedRoute);
  readonly editionContext = inject(EditionContextService);

  readonly FIELD       = FIELD;
  readonly PLACEHOLDER = PLACEHOLDER;

  readonly organisers = signal<EditionOrganiserDto[]>([]);
  readonly organiserTicketTypes = signal<OrganiserTicketTypeDto[]>([]);
  readonly organiserTicketAssignments = signal<OrganiserTicketAssignmentDto[]>([]);
  readonly ticketSelection = signal<Record<string, string | null>>({});
  readonly savingPersonId = signal<string | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchQuery = signal('');
  readonly sort = createSortController<OrganiserSortKey>({ key: 'name', direction: 'asc' });
  readonly routeEditionId = this.route.snapshot.paramMap.get('id');

  constructor() {
    if (this.routeEditionId) {
      this.editionContext.setActive(this.routeEditionId);
    }

    effect(() => {
      const edition = this.editionContext.activeEdition();
      if (edition) {
        this.load(edition.id);
      } else {
        this.organisers.set([]);
        this.organiserTicketTypes.set([]);
        this.organiserTicketAssignments.set([]);
        this.ticketSelection.set({});
        this.loading.set(false);
      }
    });
  }

  private load(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.listEditionOrganisers(editionId).subscribe({
      next: o => { this.organisers.set(o); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchOrganisers); this.loading.set(false); },
    });
    this.regSvc.getOrganiserTicketTypes(editionId).subscribe({
      next: ticketTypes => this.organiserTicketTypes.set(ticketTypes),
      error: () => this.organiserTicketTypes.set([]),
    });
    this.loadTicketAssignments(editionId);
  }

  private loadTicketAssignments(editionId: string): void {
    this.regSvc.getEditionOrganiserTicketAssignments(editionId).subscribe({
      next: assignments => {
        this.organiserTicketAssignments.set(assignments);
        this.ticketSelection.set(Object.fromEntries(assignments.map(a => [a.personId, a.ticketTypeId])));
      },
      error: () => {
        this.organiserTicketAssignments.set([]);
        this.ticketSelection.set({});
      },
    });
  }

  readonly organiserRows = computed<OrganiserPersonRow[]>(() => {
    const rows = new Map<string, OrganiserPersonRow>();

    for (const organiser of this.organisers()) {
      const row = rows.get(organiser.personId);
      const event = {
        eventId: organiser.eventId,
        eventTitle: organiser.eventTitle,
        role: organiser.role,
      };

      if (row) {
        row.events.push(event);
      } else {
        rows.set(organiser.personId, {
          personId: organiser.personId,
          personName: organiser.personName,
          email: organiser.email,
          phone: organiser.phone,
          events: [event],
        });
      }
    }

    return [...rows.values()];
  });

  readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.organiserRows() : this.organiserRows().filter(
      o => o.personName.toLowerCase().includes(q)
        || o.email.toLowerCase().includes(q)
        || o.events.some(event => event.eventTitle.toLowerCase().includes(q) || event.role.toLowerCase().includes(q))
    );
  });

  readonly sortedFiltered = computed(() =>
    sortBy(this.filtered(), this.sort.state(), {
      name: o => o.personName,
      email: o => o.email,
      event: o => this.eventTitles(o),
      role: o => this.eventRoles(o),
    })
  );

  readonly hasOrganiserTicketTypes = computed(() => this.organiserTicketTypes().length > 0);



  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  currentTicketLabel(personId: string): string {
    const assignment = this.organiserTicketAssignments().find(a => a.personId === personId);
    return assignment?.ticketTypeName ?? 'Ingen aktiv arrangörsbiljett';
  }

  eventTitles(row: OrganiserPersonRow): string {
    return row.events.map(event => event.eventTitle).join(', ');
  }

  eventRoles(row: OrganiserPersonRow): string {
    return [...new Set(row.events.map(event => event.role))].join(', ');
  }

  protected readonly ticketTypePriceLabel = formatTicketPrice;

  updateTicket(personId: string, ticketTypeId: string | null): void {
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId || this.savingPersonId()) return;

    this.ticketSelection.update(selection => ({ ...selection, [personId]: ticketTypeId }));
    this.savingPersonId.set(personId);
    this.error.set(null);
    this.regSvc.assignOrganiserTicket(editionId, personId, ticketTypeId).subscribe({
      next: () => {
        this.savingPersonId.set(null);
        this.loadTicketAssignments(editionId);
      },
      error: err => {
        this.savingPersonId.set(null);
        this.error.set(toErrorMessage(err, 'Kunde inte uppdatera arrangörsbiljetten.'));
      },
    });
  }
}
