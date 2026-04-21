import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  AuthService,
  ConventionService,
  EditionOrganiserDto,
  EditionResponsibleDto,
  EditionStaffMemberDto,
  EditionVisitorDto,
  PersonDto,
} from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { ACTION, CHIP, FIELD, PERSON_EDITION_ROLE, PERSON_EDITION_ROLE_CHIP, PLACEHOLDER, TOOLTIP } from '../../labels/ui.labels';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type PersonSortKey = 'name' | 'email' | 'phone' | 'roles' | 'status' | 'account';

@Component({
  selector: 'app-persons',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './persons.component.html',
  styleUrl: './persons.component.scss',
})
export class PersonsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(ConventionService);
  private readonly auth = inject(AuthService);
  readonly editionContext = inject(EditionContextService);

  readonly ACTION      = ACTION;
  readonly TOOLTIP     = TOOLTIP;
  readonly FIELD       = FIELD;
  readonly PLACEHOLDER = PLACEHOLDER;
  readonly CHIP        = CHIP;

  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly showCreateForm = signal(false);

  readonly searchQuery = signal('');
  readonly editingPerson = signal<PersonDto | null>(null);

  readonly onlyEditionPersons = signal(true);
  readonly editionRolesMap = signal<Map<string, string[]>>(new Map());
  readonly rolesLoading = signal(false);
  readonly currentPersonId = this.auth.personId;
  readonly sort = signal<SortState<PersonSortKey>>({ key: 'name', direction: 'asc' });

  constructor() {
    effect(() => {
      const edition = this.editionContext.activeEdition();
      if (edition) {
        this.loadEditionRoles(edition.id);
      } else {
        this.editionRolesMap.set(new Map());
      }
    });
  }

  private loadEditionRoles(editionId: string): void {
    this.rolesLoading.set(true);
    let pending = 4;
    let visitors: EditionVisitorDto[] = [];
    let organisers: EditionOrganiserDto[] = [];
    let staff: EditionStaffMemberDto[] = [];
    let responsibles: EditionResponsibleDto[] = [];

    const tryBuild = () => {
      if (--pending === 0) {
        this.editionRolesMap.set(this.buildRoleMap(visitors, organisers, staff, responsibles));
        this.rolesLoading.set(false);
      }
    };

    this.svc.listEditionVisitors(editionId).subscribe({ next: v => { visitors = v; tryBuild(); }, error: tryBuild });
    this.svc.listEditionOrganisers(editionId).subscribe({ next: o => { organisers = o; tryBuild(); }, error: tryBuild });
    this.svc.listEditionStaff(editionId).subscribe({ next: s => { staff = s; tryBuild(); }, error: tryBuild });
    this.svc.listEditionResponsibles(editionId).subscribe({ next: r => { responsibles = r; tryBuild(); }, error: tryBuild });
  }

  private buildRoleMap(
    visitors: EditionVisitorDto[],
    organisers: EditionOrganiserDto[],
    staff: EditionStaffMemberDto[],
    responsibles: EditionResponsibleDto[]
  ): Map<string, string[]> {
    const map = new Map<string, Set<string>>();
    const add = (pid: string, role: string) => {
      if (!map.has(pid)) map.set(pid, new Set());
      map.get(pid)!.add(role);
    };

    for (const v of visitors)   add(v.personId, PERSON_EDITION_ROLE.visitor);
    for (const o of organisers) add(o.personId, PERSON_EDITION_ROLE.organiser);
    for (const s of staff)      add(s.personId, PERSON_EDITION_ROLE.staff);
    for (const r of responsibles) {
      if (!r.personId) continue;
      if (r.position === 'Bemanningskoordinator' || r.position === 'Evenemangskoordinator') {
        add(r.personId, PERSON_EDITION_ROLE.coordinator);
      } else if (r.position.startsWith('Funktionsområdesansvarig') || r.position.startsWith('Kategoriansvarig')) {
        add(r.personId, PERSON_EDITION_ROLE.responsible);
      }
    }

    return new Map([...map.entries()].map(([k, v]) => [k, [...v]]));
  }

  personRoles(personId: string): string[] {
    return this.editionRolesMap().get(personId) ?? [];
  }

  roleChipClass(role: string): string {
    return PERSON_EDITION_ROLE_CHIP[role] ?? 'chip-grey';
  }

  readonly filteredPersons = computed(() => {
    const q = this.searchQuery().toLowerCase();
    const onlyEdition = this.onlyEditionPersons();
    const roleMap = this.editionRolesMap();
    const hasEditionContext = this.editionContext.activeEdition() !== null;

    return this.persons().filter(p => {
      if (q && !p.name.toLowerCase().includes(q) && !p.email.toLowerCase().includes(q)) return false;
      if (onlyEdition && hasEditionContext && !this.rolesLoading() && !p.isAdmin && !roleMap.has(p.id)) return false;
      return true;
    });
  });

  readonly sortedFilteredPersons = computed(() =>
    sortBy(this.filteredPersons(), this.sort(), {
      name: p => p.name,
      email: p => p.email,
      phone: p => p.phone ?? '',
      roles: p => [p.isAdmin ? CHIP.admin : '', ...this.personRoles(p.id)].join(' '),
      status: p => p.isActive ? CHIP.active : CHIP.inactive,
      account: p => p.isLocked ? CHIP.locked : (p.hasAccount ? CHIP.hasAccount : CHIP.noAccount),
    })
  );

  setSort(key: PersonSortKey): void {
    this.sort.set(nextSort(this.sort(), key));
  }

  sortIcon(key: PersonSortKey): string {
    return sortIcon(this.sort(), key);
  }

  readonly createForm = this.fb.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
  });

  readonly editForm = this.fb.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
  });

  ngOnInit(): void {
    this.editionContext.load();
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.svc.listPersons().subscribe({
      next: persons => {
        this.persons.set(persons);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(ERROR.fetchPersons);
        this.loading.set(false);
      },
    });
  }

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  toggleCreateForm(): void {
    this.showCreateForm.update(v => !v);
    if (!this.showCreateForm()) this.createForm.reset();
  }

  create(): void {
    if (this.createForm.invalid || this.saving()) return;
    const { name, email, phone } = this.createForm.getRawValue();
    this.saving.set(true);
    this.svc.createPerson({ name: name!, email: email!, phone: phone || null }).subscribe({
      next: () => {
        this.saving.set(false);
        this.createForm.reset();
        this.showCreateForm.set(false);
        this.load();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.detail ?? ERROR.createPerson);
      },
    });
  }

  startEdit(person: PersonDto): void {
    this.editingPerson.set(person);
    this.editForm.setValue({
      name: person.name,
      email: person.email,
      phone: person.phone ?? '',
    });
  }

  cancelEdit(): void {
    this.editingPerson.set(null);
    this.editForm.reset();
  }

  saveEdit(): void {
    const person = this.editingPerson();
    if (!person || this.editForm.invalid || this.saving()) return;
    const { name, email, phone } = this.editForm.getRawValue();
    this.saving.set(true);
    this.svc.updatePerson(person.id, { name: name!, email: email!, phone: phone || null }).subscribe({
      next: () => {
        this.saving.set(false);
        this.editingPerson.set(null);
        this.load();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.detail ?? ERROR.updatePerson);
      },
    });
  }

  deactivate(person: PersonDto): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.deactivatePerson(person.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.load();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.detail ?? ERROR.deactivatePerson);
      },
    });
  }

  reactivate(person: PersonDto): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.reactivatePerson(person.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.load();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.detail ?? ERROR.reactivatePerson);
      },
    });
  }

  sendResetLink(person: PersonDto): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.sendResetLink(person.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.error.set(null);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.detail ?? ERROR.sendResetLink);
      },
    });
  }

  toggleLock(person: PersonDto): void {
    if (this.saving()) return;
    this.saving.set(true);
    const action = person.isLocked
      ? this.svc.unlockAccount(person.id)
      : this.svc.lockAccount(person.id);
    action.subscribe({
      next: () => {
        this.saving.set(false);
        this.load();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.detail ?? ERROR.setLock);
      },
    });
  }

  makeAdmin(person: PersonDto): void {
    if (this.saving() || person.isAdmin) return;
    this.saving.set(true);
    this.svc.addAdministrator(person.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.load();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.detail ?? ERROR.setAdmin);
      },
    });
  }

  removeAdmin(person: PersonDto): void {
    if (this.saving() || !person.isAdmin) return;
    if (person.id === this.currentPersonId()) {
      this.error.set('Du kan inte ta bort dig själv som admin.');
      return;
    }

    this.saving.set(true);
    this.svc.removeAdministrator(person.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.load();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.detail ?? ERROR.setAdmin);
      },
    });
  }
}
