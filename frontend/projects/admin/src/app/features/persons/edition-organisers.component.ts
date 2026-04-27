import { Component, computed, effect, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ConventionService, EditionOrganiserDto, OrganiserTicketAssignmentDto, OrganiserTicketTypeDto, RegistrationService, toErrorMessage } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { FIELD, PLACEHOLDER } from '../../labels/ui.labels';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type OrganiserSortKey = 'name' | 'email' | 'event' | 'role';

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
  readonly sort = signal<SortState<OrganiserSortKey>>({ key: 'name', direction: 'asc' });

  constructor() {
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

  readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.organisers() : this.organisers().filter(
      o => o.personName.toLowerCase().includes(q) || o.eventTitle.toLowerCase().includes(q)
    );
  });

  readonly sortedFiltered = computed(() =>
    sortBy(this.filtered(), this.sort(), {
      name: o => o.personName,
      email: o => o.email,
      event: o => o.eventTitle,
      role: o => o.role,
    })
  );

  readonly hasOrganiserTicketTypes = computed(() => this.organiserTicketTypes().length > 0);

  setSort(key: OrganiserSortKey): void {
    this.sort.set(nextSort(this.sort(), key));
  }

  sortIcon(key: OrganiserSortKey): string {
    return sortIcon(this.sort(), key);
  }

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  setTicketSelection(personId: string, ticketTypeId: string | null): void {
    this.ticketSelection.update(selection => ({ ...selection, [personId]: ticketTypeId }));
  }

  currentTicketLabel(personId: string): string {
    const assignment = this.organiserTicketAssignments().find(a => a.personId === personId);
    return assignment?.ticketTypeName ?? 'Ingen aktiv arrangörsbiljett';
  }

  ticketTypePriceLabel(price: number): string {
    if (price === 0) return 'Kostnadsfri';

    return new Intl.NumberFormat('sv-SE', {
      style: 'currency',
      currency: 'SEK',
      maximumFractionDigits: 0,
    }).format(price / 100);
  }

  saveTicket(personId: string): void {
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId || this.savingPersonId()) return;

    this.savingPersonId.set(personId);
    this.error.set(null);
    this.regSvc.assignOrganiserTicket(editionId, personId, this.ticketSelection()[personId] ?? null).subscribe({
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
