import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ConventionService, EditionStaffMemberDto, PersonDto, RegistrationService, StaffService, StaffTicketAssignmentDto, StaffTicketTypeDto, STAFF_APPLICATION_STATUS_LABEL, toErrorMessage } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { ACTION, FIELD, PLACEHOLDER, TOOLTIP } from '../../labels/ui.labels';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type StaffSortKey = 'name' | 'email' | 'phone' | 'status';

@Component({
  selector: 'app-edition-staff',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './edition-staff.component.html',
  styleUrl: './edition-staff.component.scss',
})
export class EditionStaffComponent {
  private readonly svc     = inject(ConventionService);
  private readonly staffSvc = inject(StaffService);
  private readonly regSvc  = inject(RegistrationService);
  private readonly fb      = inject(FormBuilder);
  readonly editionContext  = inject(EditionContextService);

  readonly ACTION      = ACTION;
  readonly TOOLTIP     = TOOLTIP;
  readonly FIELD       = FIELD;
  readonly PLACEHOLDER = PLACEHOLDER;

  readonly staff       = signal<EditionStaffMemberDto[]>([]);
  readonly staffTicketTypes       = signal<StaffTicketTypeDto[]>([]);
  readonly staffTicketAssignments = signal<StaffTicketAssignmentDto[]>([]);
  readonly ticketSelection        = signal<Record<string, string | null>>({});
  readonly savingPersonId         = signal<string | null>(null);
  readonly loading     = signal(false);
  readonly error       = signal<string | null>(null);
  readonly searchQuery = signal('');
  readonly sort = signal<SortState<StaffSortKey>>({ key: 'name', direction: 'asc' });

  // ── Lägg till funktionär ─────────────────────────────────────────────────
  readonly persons       = signal<PersonDto[]>([]);
  readonly personsLoaded = signal(false);
  readonly showAddForm   = signal(false);
  readonly addSaving     = signal(false);
  readonly addEmailInput = signal('');
  readonly addNameInput  = signal('');

  readonly addStaffForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    name:  [''],
    phone: [''],
    note:  [''],
  });

  readonly matchedPerson = computed(() => {
    const email = this.addEmailInput().trim().toLowerCase();
    if (!email) return null;
    return this.persons().find(p => p.email.toLowerCase() === email) ?? null;
  });

  readonly addFormInvalid = computed(() => {
    const email = this.addEmailInput().trim();
    if (!email || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) return true;
    if (!this.matchedPerson() && !this.addNameInput().trim()) return true;
    return false;
  });

  constructor() {
    effect(() => {
      const edition = this.editionContext.activeEdition();
      if (edition) {
        this.load(edition.id);
      } else {
        this.staff.set([]);
        this.staffTicketTypes.set([]);
        this.staffTicketAssignments.set([]);
        this.ticketSelection.set({});
        this.loading.set(false);
      }
    });
  }

  private load(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.listEditionStaff(editionId).subscribe({
      next: s => { this.staff.set(s); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchStaff); this.loading.set(false); },
    });
    this.regSvc.getStaffTicketTypes(editionId).subscribe({
      next: types => this.staffTicketTypes.set(types),
      error: () => this.staffTicketTypes.set([]),
    });
    this.loadTicketAssignments(editionId);
  }

  private loadTicketAssignments(editionId: string): void {
    this.regSvc.getEditionStaffTicketAssignments(editionId).subscribe({
      next: assignments => {
        this.staffTicketAssignments.set(assignments);
        this.ticketSelection.set(Object.fromEntries(assignments.map(a => [a.personId, a.ticketTypeId])));
      },
      error: () => {
        this.staffTicketAssignments.set([]);
        this.ticketSelection.set({});
      },
    });
  }

  readonly hasStaffTicketTypes = computed(() => this.staffTicketTypes().length > 0);

  readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.staff() : this.staff().filter(
      s => s.personName.toLowerCase().includes(q) || s.email.toLowerCase().includes(q)
    );
  });

  readonly sortedFiltered = computed(() =>
    sortBy(this.filtered(), this.sort(), {
      name: s => s.personName,
      email: s => s.email,
      phone: s => s.phone ?? '',
      status: s => this.applicationStatusLabel(s.applicationStatus),
    })
  );

  setSort(key: StaffSortKey): void {
    this.sort.set(nextSort(this.sort(), key));
  }

  sortIcon(key: StaffSortKey): string {
    return sortIcon(this.sort(), key);
  }

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  applicationStatusLabel(status: string): string {
    return STAFF_APPLICATION_STATUS_LABEL[status] ?? status;
  }

  currentTicketLabel(personId: string): string {
    const assignment = this.staffTicketAssignments().find(a => a.personId === personId);
    return assignment?.ticketTypeName ?? 'Ingen aktiv funktionärsbiljett';
  }

  setTicketSelection(personId: string, ticketTypeId: string | null): void {
    this.ticketSelection.update(sel => ({ ...sel, [personId]: ticketTypeId }));
  }

  ticketTypePriceLabel(price: number): string {
    if (price === 0) return 'Kostnadsfri';
    return new Intl.NumberFormat('sv-SE', {
      style: 'currency', currency: 'SEK', maximumFractionDigits: 0,
    }).format(price / 100);
  }

  saveTicket(personId: string): void {
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId || this.savingPersonId()) return;

    this.savingPersonId.set(personId);
    this.error.set(null);
    this.regSvc.assignStaffTicket(editionId, personId, this.ticketSelection()[personId] ?? null).subscribe({
      next: () => {
        this.savingPersonId.set(null);
        this.loadTicketAssignments(editionId);
      },
      error: err => {
        this.savingPersonId.set(null);
        this.error.set(toErrorMessage(err, 'Kunde inte uppdatera funktionärsbiljetten.'));
      },
    });
  }

  openAddForm(): void {
    this.showAddForm.set(true);
    this.addStaffForm.reset();
    this.addEmailInput.set('');
    this.addNameInput.set('');
    if (this.personsLoaded()) return;
    this.svc.listPersons().subscribe({
      next: persons => { this.persons.set(persons.filter(p => p.isActive)); this.personsLoaded.set(true); },
    });
  }

  cancelAddForm(): void {
    this.showAddForm.set(false);
    this.addStaffForm.reset();
    this.addEmailInput.set('');
    this.addNameInput.set('');
  }

  submitAddStaff(): void {
    if (this.addFormInvalid() || this.addSaving()) return;
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId) return;

    const { email, name, phone, note } = this.addStaffForm.getRawValue();
    const matched = this.matchedPerson();

    this.addSaving.set(true);
    this.staffSvc.addStaffMember(editionId, {
      email: email!.trim(),
      name:  matched ? matched.name  : name!.trim(),
      phone: matched ? matched.phone : phone || null,
      note:  note || null,
    }).subscribe({
      next: () => {
        this.addSaving.set(false);
        this.cancelAddForm();
        this.load(editionId);
      },
      error: err => {
        this.addSaving.set(false);
        this.error.set(toErrorMessage(err, ERROR.addStaffMember));
      },
    });
  }
}
