import { Component, computed, effect, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, formatDate, PersonDto, ReceptionStaffMemberDto, toErrorMessage } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { ACTION, FIELD, PLACEHOLDER } from '../../labels/ui.labels';
import { createSortController, sortBy } from '../../shared/sort-utils';

type SortKey = 'name' | 'email' | 'addedAt';

@Component({
  selector: 'app-edition-reception-staff',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './edition-reception-staff.component.html',
  styleUrl: './edition-reception-staff.component.scss',
})
export class EditionReceptionStaffComponent {
  private readonly svc    = inject(ConventionService);
  readonly editionContext = inject(EditionContextService);

  readonly ACTION      = ACTION;
  readonly FIELD       = FIELD;
  readonly PLACEHOLDER = PLACEHOLDER;

  readonly members     = signal<ReceptionStaffMemberDto[]>([]);
  readonly loading     = signal(false);
  readonly error       = signal<string | null>(null);
  readonly searchQuery = signal('');
  readonly sort        = createSortController<SortKey>({ key: 'name', direction: 'asc' });

  // ── Lägg till ────────────────────────────────────────────────────────────
  readonly persons       = signal<PersonDto[]>([]);
  readonly personsLoaded = signal(false);
  readonly showAddForm   = signal(false);
  readonly addEmailInput = signal('');
  readonly addSaving     = signal(false);

  readonly matchedPerson = computed(() => {
    const email = this.addEmailInput().trim().toLowerCase();
    if (!email) return null;
    return this.persons().find(p => p.email.toLowerCase() === email) ?? null;
  });

  readonly alreadyMember = computed(() => {
    const matched = this.matchedPerson();
    if (!matched) return false;
    return this.members().some(m => m.personId === matched.id);
  });

  readonly addDisabled = computed(() =>
    !this.matchedPerson() || this.alreadyMember() || this.addSaving()
  );

  // ── Ta bort ──────────────────────────────────────────────────────────────
  readonly removingPersonId = signal<string | null>(null);
  readonly confirmPersonId  = signal<string | null>(null);

  constructor() {
    effect(() => {
      const edition = this.editionContext.activeEdition();
      if (edition) {
        this.load(edition.id);
      } else {
        this.members.set([]);
        this.loading.set(false);
      }
    });
  }

  private load(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.listEditionReceptionStaff(editionId).subscribe({
      next: m => { this.members.set(m); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchReceptionStaff); this.loading.set(false); },
    });
  }

  readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.members() : this.members().filter(
      m => m.name.toLowerCase().includes(q) || m.email.toLowerCase().includes(q)
    );
  });

  readonly sortedFiltered = computed(() =>
    sortBy(this.filtered(), this.sort.state(), {
      name:    m => m.name,
      email:   m => m.email,
      addedAt: m => m.addedAt,
    })
  );



  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  openAddForm(): void {
    this.showAddForm.set(true);
    this.addEmailInput.set('');
    if (this.personsLoaded()) return;
    this.svc.listPersons().subscribe({
      next: persons => { this.persons.set(persons.filter(p => p.isActive)); this.personsLoaded.set(true); },
    });
  }

  cancelAddForm(): void {
    this.showAddForm.set(false);
    this.addEmailInput.set('');
  }

  submitAdd(): void {
    const matched = this.matchedPerson();
    if (!matched || this.addDisabled()) return;
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId) return;

    this.addSaving.set(true);
    this.error.set(null);
    this.svc.addEditionReceptionStaff(editionId, matched.id).subscribe({
      next: () => {
        this.addSaving.set(false);
        this.cancelAddForm();
        this.load(editionId);
      },
      error: err => {
        this.addSaving.set(false);
        this.error.set(toErrorMessage(err, ERROR.addReceptionStaff));
      },
    });
  }

  requestRemove(personId: string): void {
    this.confirmPersonId.set(personId);
  }

  cancelRemove(): void {
    this.confirmPersonId.set(null);
  }

  confirmRemove(personId: string): void {
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId || this.removingPersonId()) return;

    this.removingPersonId.set(personId);
    this.confirmPersonId.set(null);
    this.error.set(null);
    this.svc.removeEditionReceptionStaff(editionId, personId).subscribe({
      next: () => {
        this.removingPersonId.set(null);
        this.load(editionId);
      },
      error: err => {
        this.removingPersonId.set(null);
        this.error.set(toErrorMessage(err, ERROR.removeReceptionStaff));
      },
    });
  }

  protected readonly formatDate = formatDate;
}
