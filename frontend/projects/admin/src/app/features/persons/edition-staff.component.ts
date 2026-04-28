import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  ConventionService,
  EditionDto,
  PersonDto,
  RegistrationService,
  StaffApplicationSummaryDto,
  StaffAreaDto,
  StaffService,
  StaffTicketAssignmentDto,
  StaffTicketTypeDto,
  STAFF_APPLICATION_STATUS_CHIP,
  STAFF_APPLICATION_STATUS_LABEL,
  toErrorMessage,
} from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { ACTION, FIELD, PLACEHOLDER, TOOLTIP } from '../../labels/ui.labels';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';

type StaffApplicationSortKey = 'person' | 'interest' | 'staffAreas' | 'availability' | 'created' | 'status';

@Component({
  selector: 'app-edition-staff',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
  ],
  templateUrl: './edition-staff.component.html',
  styleUrl: './edition-staff.component.scss',
})
export class EditionStaffComponent {
  private readonly conventionSvc = inject(ConventionService);
  private readonly staffSvc = inject(StaffService);
  private readonly regSvc = inject(RegistrationService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  readonly editionContext = inject(EditionContextService);

  readonly ACTION = ACTION;
  readonly TOOLTIP = TOOLTIP;
  readonly FIELD = FIELD;
  readonly PLACEHOLDER = PLACEHOLDER;

  readonly edition = signal<EditionDto | null>(null);
  readonly applications = signal<StaffApplicationSummaryDto[]>([]);
  readonly staffTicketTypes = signal<StaffTicketTypeDto[]>([]);
  readonly staffTicketAssignments = signal<StaffTicketAssignmentDto[]>([]);
  readonly ticketSelection = signal<Record<string, string | null>>({});
  readonly savingPersonId = signal<string | null>(null);
  readonly savingApplicationId = signal<string | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchQuery = signal('');
  readonly applicationSort = signal<SortState<StaffApplicationSortKey>>({ key: 'created', direction: 'desc' });

  readonly persons = signal<PersonDto[]>([]);
  readonly personsLoaded = signal(false);
  readonly showAddForm = signal(false);
  readonly addSaving = signal(false);
  readonly addEmailInput = signal('');
  readonly addNameInput = signal('');

  readonly addStaffForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    name: [''],
    phone: [''],
    note: [''],
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

  readonly filteredApplications = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    const apps = this.applications();
    if (!q) return apps;

    return apps.filter(app => {
      const staffAreas = app.staffAreaPreferenceIds.map(id => this.staffAreaName(id).toLowerCase()).join(' ');
      const availability = app.availabilities.map(av => `${av.start} ${av.end}`).join(' ').toLowerCase();
      const personName = (app.personName ?? '').toLowerCase();
      const status = this.applicationStatusLabel(app.status).toLowerCase();

      return personName.includes(q)
        || app.personId.toLowerCase().includes(q)
        || app.interestDescription.toLowerCase().includes(q)
        || staffAreas.includes(q)
        || availability.includes(q)
        || status.includes(q);
    });
  });

  readonly sortedFilteredApplications = computed(() =>
    sortBy(this.filteredApplications(), this.applicationSort(), {
      person: app => app.personName ?? app.personId,
      interest: app => app.interestDescription,
      staffAreas: app => app.staffAreaPreferenceIds.map(id => this.staffAreaName(id)).join(', '),
      availability: app => app.availabilities.map(av => `${av.start}-${av.end}`).join(', '),
      created: app => app.createdAt,
      status: app => this.applicationStatusLabel(app.status),
    })
  );

  readonly hasStaffTicketTypes = computed(() => this.staffTicketTypes().length > 0);

  constructor() {
    effect(() => {
      const edition = this.editionContext.activeEdition();
      if (edition) {
        this.load(edition.id);
      } else {
        this.edition.set(null);
        this.applications.set([]);
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

    let primaryLoadsRemaining = 2;
    const finishPrimaryLoad = () => {
      primaryLoadsRemaining -= 1;
      if (primaryLoadsRemaining <= 0) {
        this.loading.set(false);
      }
    };

    this.conventionSvc.getEdition(editionId).subscribe({
      next: edition => {
        this.edition.set(edition);
        finishPrimaryLoad();
      },
      error: () => {
        this.error.set(ERROR.fetchEdition);
        finishPrimaryLoad();
      },
    });

    this.staffSvc.listStaffApplications(editionId).subscribe({
      next: applications => {
        this.applications.set(applications);
        finishPrimaryLoad();
      },
      error: () => {
        this.error.set(ERROR.fetchStaffApplications);
        finishPrimaryLoad();
      },
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

  setApplicationSort(key: StaffApplicationSortKey): void {
    this.applicationSort.set(nextSort(this.applicationSort(), key));
  }

  applicationSortIcon(key: StaffApplicationSortKey): string {
    return sortIcon(this.applicationSort(), key);
  }

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  applicationStatusLabel(status: string): string {
    return STAFF_APPLICATION_STATUS_LABEL[status] ?? status;
  }

  applicationStatusChipClass(status: string): string {
    return STAFF_APPLICATION_STATUS_CHIP[status] ?? 'chip chip-grey';
  }

  currentTicketLabel(personId: string): string {
    const assignment = this.staffTicketAssignments().find(a => a.personId === personId);
    return assignment?.ticketTypeName ?? 'Ingen aktiv funktionärsbiljett';
  }

  ticketTypePriceLabel(price: number): string {
    if (price === 0) return 'Kostnadsfri';
    return new Intl.NumberFormat('sv-SE', {
      style: 'currency',
      currency: 'SEK',
      maximumFractionDigits: 0,
    }).format(price / 100);
  }

  updateTicket(personId: string, ticketTypeId: string | null): void {
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId || this.savingPersonId()) return;

    this.ticketSelection.update(sel => ({ ...sel, [personId]: ticketTypeId }));
    this.savingPersonId.set(personId);
    this.error.set(null);

    this.regSvc.assignStaffTicket(editionId, personId, ticketTypeId).subscribe({
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
    this.conventionSvc.listPersons().subscribe({
      next: persons => {
        this.persons.set(persons.filter(p => p.isActive));
        this.personsLoaded.set(true);
      },
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
      name: matched ? matched.name : name!.trim(),
      phone: matched ? matched.phone : phone || null,
      note: note || null,
    }).subscribe({
      next: () => {
        this.addSaving.set(false);
        this.cancelAddForm();
        this.reloadStaffingData();
      },
      error: err => {
        this.addSaving.set(false);
        this.error.set(toErrorMessage(err, ERROR.addStaffMember));
      },
    });
  }

  openApplicationDetail(applicationId: string): void {
    void this.router.navigate(['/persons/staff', applicationId]);
  }

  acceptApplication(app: StaffApplicationSummaryDto, event?: Event): void {
    event?.stopPropagation();
    if (this.savingApplicationId()) return;

    this.savingApplicationId.set(app.id);
    this.error.set(null);
    this.staffSvc.acceptApplication(app.id).subscribe({
      next: () => {
        this.savingApplicationId.set(null);
        this.reloadStaffingData();
      },
      error: err => {
        this.savingApplicationId.set(null);
        this.error.set(toErrorMessage(err, ERROR.acceptApplication));
      },
    });
  }

  rejectApplication(app: StaffApplicationSummaryDto, event?: Event): void {
    event?.stopPropagation();
    if (this.savingApplicationId()) return;

    this.savingApplicationId.set(app.id);
    this.error.set(null);
    this.staffSvc.rejectApplication(app.id).subscribe({
      next: () => {
        this.savingApplicationId.set(null);
        this.reloadStaffingData();
      },
      error: err => {
        this.savingApplicationId.set(null);
        this.error.set(toErrorMessage(err, ERROR.rejectApplication));
      },
    });
  }

  removeApplication(app: StaffApplicationSummaryDto, event?: Event): void {
    event?.stopPropagation();
    if (this.savingApplicationId() || !confirm(`Ta bort ansökan för ${app.personName ?? app.personId}?`)) return;

    this.savingApplicationId.set(app.id);
    this.error.set(null);
    this.staffSvc.deleteApplication(app.id).subscribe({
      next: () => {
        this.savingApplicationId.set(null);
        this.reloadStaffingData();
      },
      error: err => {
        this.savingApplicationId.set(null);
        this.error.set(toErrorMessage(err, ERROR.deleteApplication));
      },
    });
  }

  canReview(status: string): boolean {
    return status === 'Received' || status === 'UnderReview';
  }

  canManageTicket(status: string): boolean {
    return status === 'Confirmed' || status === 'Assigned';
  }

  staffAreaName(staffAreaId: string): string {
    return this.edition()?.staffAreas?.find((s: StaffAreaDto) => s.id === staffAreaId)?.name ?? staffAreaId;
  }

  formatAvailability(start: string, end: string): string {
    const s = new Date(start);
    const e = new Date(end);
    const sameDay = s.toDateString() === e.toDateString();
    const dateStr = s.toLocaleDateString('sv-SE', { month: 'short', day: 'numeric' });
    const sTime = s.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });
    const eTime = e.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });

    return sameDay
      ? `${dateStr} ${sTime}-${eTime}`
      : `${dateStr} ${sTime} - ${e.toLocaleDateString('sv-SE', { month: 'short', day: 'numeric' })} ${eTime}`;
  }

  private reloadStaffingData(): void {
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId) return;

    this.load(editionId);
  }
}
