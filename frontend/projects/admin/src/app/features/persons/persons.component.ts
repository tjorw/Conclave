import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  ConventionService,
  EditionOrganiserDto,
  EditionStaffMemberDto,
  EditionVisitorDto,
  PersonDto,
} from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { CHIP, PERSON_EDITION_ROLE, PERSON_EDITION_ROLE_CHIP, PLACEHOLDER } from '../../labels/ui.labels';
import { createSortController, sortBy } from '../../shared/sort-utils';

type PersonSortKey = 'name' | 'email' | 'phone' | 'roles' | 'status' | 'account';

@Component({
  selector: 'app-persons',
  standalone: true,
  imports: [
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
  private readonly router = inject(Router);
  private readonly svc = inject(ConventionService);
  readonly editionContext = inject(EditionContextService);

  readonly PLACEHOLDER = PLACEHOLDER;
  readonly CHIP        = CHIP;

  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  readonly searchQuery        = signal('');
  readonly onlyEditionPersons = signal(true);
  readonly editionRolesMap    = signal<Map<string, string[]>>(new Map());
  readonly rolesLoading       = signal(false);
  readonly sort               = createSortController<PersonSortKey>({ key: 'name', direction: 'asc' });

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
    forkJoin({
      visitors:  this.svc.listEditionVisitors(editionId),
      organisers: this.svc.listEditionOrganisers(editionId),
      staff:     this.svc.listEditionStaff(editionId),
    }).pipe(
      catchError(() => of({ visitors: [] as EditionVisitorDto[], organisers: [] as EditionOrganiserDto[], staff: [] as EditionStaffMemberDto[] }))
    ).subscribe(({ visitors, organisers, staff }) => {
      this.editionRolesMap.set(this.buildRoleMap(visitors, organisers, staff));
      this.rolesLoading.set(false);
    });
  }

  private buildRoleMap(
    visitors: EditionVisitorDto[],
    organisers: EditionOrganiserDto[],
    staff: EditionStaffMemberDto[],
  ): Map<string, string[]> {
    const map = new Map<string, Set<string>>();
    const add = (pid: string, role: string) => {
      if (!map.has(pid)) map.set(pid, new Set());
      map.get(pid)!.add(role);
    };

    for (const v of visitors)   add(v.personId, PERSON_EDITION_ROLE.visitor);
    for (const o of organisers) add(o.personId, PERSON_EDITION_ROLE.organiser);
    for (const s of staff)      add(s.personId, PERSON_EDITION_ROLE.staff);

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
    sortBy(this.filteredPersons(), this.sort.state(), {
      name:    p => p.name,
      email:   p => p.email,
      phone:   p => p.phone ?? '',
      roles:   p => [p.isAdmin ? CHIP.admin : '', ...this.personRoles(p.id)].join(' '),
      status:  p => p.isActive ? CHIP.active : CHIP.inactive,
      account: p => p.isLocked ? CHIP.locked : (p.hasAccount ? CHIP.hasAccount : CHIP.noAccount),
    })
  );



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

  openDetail(personId: string): void {
    void this.router.navigate(['/persons', personId]);
  }

  openNew(): void {
    void this.router.navigate(['/persons', 'new']);
  }
}
