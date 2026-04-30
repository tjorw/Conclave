import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { distinctUntilChanged, map } from 'rxjs';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
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
  createAsyncState,
  EditionDto,
  formatDate,
  MarkdownEditorComponent,
  PersonDto,
  RegistrationService,
  StaffAreaDto,
  TicketTypeAdminDto,
  VenueDto,
  toContextErrorMessage,
} from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../labels/pages.labels';
import { ACTION, FIELD, TOOLTIP } from '../../../labels/ui.labels';
import { ConfirmDialogService } from '../../../shared/confirm-dialog/confirm-dialog.service';
import { EditionContextService } from '../../../services/edition-context.service';
import { nextSort, sortBy, sortIcon, SortState } from '../../../shared/sort-utils';

type VenueSortKey = 'name' | 'building' | 'description';
type StaffAreaSortKey = 'name' | 'description' | 'responsible' | 'stations';
type CategorySortKey = 'name' | 'organizerInstructions' | 'publicDescription' | 'responsible';
type TicketTypeSortKey = 'name' | 'category' | 'validDays' | 'allowedCategories' | 'price';
type EditionDetailSection = 'basics' | 'lifecycle' | 'venues' | 'staff-areas' | 'categories' | 'ticket-types';

const EDITION_DETAIL_SECTIONS: EditionDetailSection[] = [
  'basics',
  'lifecycle',
  'venues',
  'staff-areas',
  'categories',
  'ticket-types',
];

@Component({
  selector: 'app-edition-detail',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
    MarkdownEditorComponent,
  ],
  templateUrl: './edition-detail.component.html',
  styleUrl: './edition-detail.component.scss',
})
export class EditionDetailComponent implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);
  private readonly svc    = inject(ConventionService);
  private readonly regSvc = inject(RegistrationService);
  private readonly confirmSvc     = inject(ConfirmDialogService);
  private readonly editionContext = inject(EditionContextService);

  readonly edition = signal<EditionDto | null>(null);
  readonly persons = signal<PersonDto[]>([]);
  protected readonly state = createAsyncState(true);
  readonly section = signal<EditionDetailSection>('basics');
  readonly scheduleDayTimes = signal<Record<string, { startTime: string | null; endTime: string | null }>>({});
  readonly showAddVenueForm = signal(false);
  readonly showAddStaffAreaForm = signal(false);
  readonly showAddCategoryForm = signal(false);
  readonly showAddTicketTypeForm = signal(false);

  // Edit targets
  readonly editingVenue = signal<VenueDto | null>(null);
  readonly editingStaffArea = signal<StaffAreaDto | null>(null);
  readonly editingCategory   = signal<CategoryDto | null>(null);

  // Biljettyper (laddas separat – inte en del av EditionDto)
  readonly ticketTypes        = signal<TicketTypeAdminDto[]>([]);
  readonly editingTicketType  = signal<TicketTypeAdminDto | null>(null);
  readonly venueSort = signal<SortState<VenueSortKey>>({ key: 'name', direction: 'asc' });
  readonly staffAreaSort = signal<SortState<StaffAreaSortKey>>({ key: 'name', direction: 'asc' });
  readonly categorySort = signal<SortState<CategorySortKey>>({ key: 'name', direction: 'asc' });
  readonly ticketTypeSort = signal<SortState<TicketTypeSortKey>>({ key: 'name', direction: 'asc' });

  private handleError(context: string, err: unknown): void {
    this.state.error.set(toContextErrorMessage(err, context));
    this.state.saving.set(false);
  }

  readonly ACTION  = ACTION;
  readonly TOOLTIP = TOOLTIP;
  readonly FIELD   = FIELD;
  readonly PAGE    = EDITION_DETAIL;

  readonly isDraft = computed(() => this.edition()?.status === 'Draft');
  readonly isPublished = computed(() => this.edition()?.status === 'Published');
  readonly activeEdition = this.editionContext.activeEdition;
  readonly isActiveEdition = computed(() => {
    const currentEditionId = this.edition()?.id;
    const activeEditionId = this.activeEdition()?.id;
    return !!currentEditionId && currentEditionId === activeEditionId;
  });
  readonly openRegistrationCount = computed(() =>
    this.registrationTypes.filter(type => this.registrationOpen(type.type)).length
  );
  readonly editionDayOptions = computed(() => {
    const edition = this.edition();
    if (!edition) {
      return [] as { value: string; label: string }[];
    }

    const start = new Date(`${edition.start.substring(0, 10)}T00:00:00`);
    const end = new Date(`${edition.end.substring(0, 10)}T00:00:00`);
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || start > end) {
      return [] as { value: string; label: string }[];
    }

    const options: { value: string; label: string }[] = [];
    for (const current = new Date(start); current <= end; current.setDate(current.getDate() + 1)) {
      const value = `${current.getFullYear()}-${(current.getMonth() + 1).toString().padStart(2, '0')}-${current.getDate().toString().padStart(2, '0')}`;
      const label = new Intl.DateTimeFormat('sv-SE', {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
      }).format(current);
      options.push({ value, label });
    }

    return options;
  });

  readonly registrationTypes: { type: 'organiser' | 'staff' | 'visitor'; label: string }[] = [
    { type: 'organiser', label: 'Arrangörsregistrering' },
    { type: 'staff', label: 'Staffregistrering' },
    { type: 'visitor', label: 'Besökarregistrering' },
  ];

  readonly registrationTypeLabels: Record<'organiser' | 'staff' | 'visitor', string> = {
    organiser: this.PAGE.organiserSubLabel,
    staff: this.PAGE.staffSubLabel,
    visitor: this.PAGE.visitorSubLabel,
  };

  readonly registrationPillLabels: Record<'organiser' | 'staff' | 'visitor', string> = {
    organiser: 'Arrangör',
    staff: 'Funktionär',
    visitor: 'Besökare',
  };

  // ── Skapa-formulär ───────────────────────────────────────────────────────

  readonly venueForm = this.fb.group({
    name: ['', Validators.required],
    building: ['', Validators.required],
    description: [''],
  });

  readonly staffAreaForm = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    responsibleId: ['', Validators.required],
  });

  readonly categoryForm = this.fb.group({
    name: ['', Validators.required],
    organizerInstructions: [''],
    publicDescription: [''],
    responsibleId: ['', Validators.required],
  });

  // ── Redigera-formulär ────────────────────────────────────────────────────

  readonly editEditionForm = this.fb.group({
    name: ['', Validators.required],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    staffCoordinatorId: ['', Validators.required],
    eventCoordinatorId: ['', Validators.required],
  });

  readonly editVenueForm = this.fb.group({
    name: ['', Validators.required],
    building: ['', Validators.required],
    description: [''],
  });

  readonly editStaffAreaForm = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    responsibleId: ['', Validators.required],
  });

  readonly editCategoryForm = this.fb.group({
    name: ['', Validators.required],
    organizerInstructions: [''],
    publicDescription: [''],
    responsibleId: ['', Validators.required],
  });

  readonly ticketTypeCategories = [
    { value: 'Visitor',   label: 'Besökare' },
    { value: 'Organiser', label: 'Arrangör' },
    { value: 'Staff',     label: 'Funktionär' },
  ];

  ticketTypeCategoryLabel(cat: string): string {
    const map: Record<string, string> = { Visitor: 'Besökare', Organiser: 'Arrangör', Staff: 'Funktionär' };
    return map[cat] ?? cat;
  }

  readonly addTicketTypeForm = this.fb.group({
    name:     ['', Validators.required],
    price:    [0, [Validators.required, Validators.min(0)]],
    category: ['Visitor', Validators.required],
    validDays: this.fb.control<string[]>([], { nonNullable: true }),
    allowedCategories: this.fb.control<string[]>([], { nonNullable: true }),
  });

  readonly editTicketTypeForm = this.fb.group({
    name:  ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0)]],
    category: ['Visitor', Validators.required],
    validDays: this.fb.control<string[]>([], { nonNullable: true }),
    allowedCategories: this.fb.control<string[]>([], { nonNullable: true }),
  });

  // ── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.editionContext.load();
    this.route.paramMap
      .pipe(
        map(params => {
          const id = params.get('id')!;
          const sectionParam = params.get('section');
          const section = this.normalizeSection(sectionParam);
          if (sectionParam && !section) {
            void this.router.navigate(['/editions', id], { replaceUrl: true });
          }
          this.section.set(section ?? 'basics');
          return id;
        }),
        distinctUntilChanged()
      )
      .subscribe(id => this.loadData(id));
  }

  private normalizeSection(section: string | null): EditionDetailSection | null {
    if (!section) return 'basics';
    return EDITION_DETAIL_SECTIONS.includes(section as EditionDetailSection)
      ? section as EditionDetailSection
      : null;
  }

  private loadData(editionId: string): void {
    this.state.loading.set(true);
    this.svc.getEdition(editionId).subscribe({
      next: e => {
        this.edition.set(e);
        this.syncEditEditionForm(e);
        this.state.loading.set(false);
      },
      error: () => { this.state.error.set(ERROR.fetchEdition); this.state.loading.set(false); },
    });
    this.svc.listPersons().subscribe({
      next: p => this.persons.set(p.filter(x => x.isActive)),
    });
    this.regSvc.listTicketTypes(editionId).subscribe({
      next: tt => this.ticketTypes.set(tt),
    });
  }

  private reload(): void {
    const id = this.edition()!.id;
    this.svc.getEdition(id).subscribe({
      next: e => {
        this.edition.set(e);
        this.syncEditEditionForm(e);
      },
    });
    this.regSvc.listTicketTypes(id).subscribe({ next: tt => this.ticketTypes.set(tt) });
  }

  private syncEditEditionForm(edition: EditionDto): void {
    this.editEditionForm.setValue({
      name: edition.name,
      startDate: edition.start.substring(0, 10),
      endDate: edition.end.substring(0, 10),
      staffCoordinatorId: edition.staffCoordinatorId ?? '',
      eventCoordinatorId: edition.eventCoordinatorId ?? '',
    });
    this.scheduleDayTimes.set(Object.fromEntries(
      (edition.scheduleDays ?? []).map(day => [
        day.date.substring(0, 10),
        {
          startTime: this.toTimeInput(day.startTime),
          endTime: this.toTimeInput(day.endTime),
        },
      ])
    ));
  }

  personName(id: string): string {
    return this.persons().find(p => p.id === id)?.name ?? id;
  }

  stationsForArea(area: StaffAreaDto): { id: string; name: string; description: string | null }[] {
    return this.edition()?.stations.filter(s => s.staffAreaId === area.id) ?? [];
  }

  sortedVenues(venues: VenueDto[]): VenueDto[] {
    return sortBy(venues, this.venueSort(), {
      name: venue => venue.name,
      building: venue => venue.building,
      description: venue => venue.description ?? '',
    });
  }

  setVenueSort(key: VenueSortKey): void {
    this.venueSort.set(nextSort(this.venueSort(), key));
  }

  venueSortIcon(key: VenueSortKey): string {
    return sortIcon(this.venueSort(), key);
  }

  sortedStaffAreas(areas: StaffAreaDto[]): StaffAreaDto[] {
    return sortBy(areas, this.staffAreaSort(), {
      name: area => area.name,
      description: area => area.description ?? '',
      responsible: area => this.personName(area.responsibleId),
      stations: area => this.stationsForArea(area).length,
    });
  }

  setStaffAreaSort(key: StaffAreaSortKey): void {
    this.staffAreaSort.set(nextSort(this.staffAreaSort(), key));
  }

  staffAreaSortIcon(key: StaffAreaSortKey): string {
    return sortIcon(this.staffAreaSort(), key);
  }

  sortedCategories(categories: CategoryDto[]): CategoryDto[] {
    return sortBy(categories, this.categorySort(), {
      name: category => category.name,
      organizerInstructions: category => category.organizerInstructions ?? '',
      publicDescription: category => category.publicDescription ?? '',
      responsible: category => this.personName(category.responsibleId),
    });
  }

  setCategorySort(key: CategorySortKey): void {
    this.categorySort.set(nextSort(this.categorySort(), key));
  }

  categorySortIcon(key: CategorySortKey): string {
    return sortIcon(this.categorySort(), key);
  }

  sortedTicketTypes(): TicketTypeAdminDto[] {
    return sortBy(this.ticketTypes(), this.ticketTypeSort(), {
      name: ticketType => ticketType.name,
      category: ticketType => this.ticketTypeCategoryLabel(ticketType.category),
      validDays: ticketType => this.validDaysLabel(ticketType.validDays),
      allowedCategories: ticketType => this.allowedCategoriesLabel(ticketType.allowedCategories),
      price: ticketType => ticketType.price,
    });
  }

  setTicketTypeSort(key: TicketTypeSortKey): void {
    this.ticketTypeSort.set(nextSort(this.ticketTypeSort(), key));
  }

  ticketTypeSortIcon(key: TicketTypeSortKey): string {
    return sortIcon(this.ticketTypeSort(), key);
  }

  // ── Publicering, Aktiv upplaga & Registrering ────────────────────────────

  setActive(): void {
    this.state.saving.set(true);
    const editionId = this.edition()!.id;
    this.svc.setActiveEdition(editionId).subscribe({
      next: () => {
        this.editionContext.setActive(editionId);
        this.state.saving.set(false);
      },
      error: (err) => this.handleError(ERROR.setActiveEdition, err),
    });
  }

  publish(): void {
    this.confirmSvc.confirm({
      title:        this.PAGE.publishConfirmTitle,
      message:      this.PAGE.publishConfirmMessage,
      confirmLabel: this.PAGE.publishAction,
    }).subscribe(confirmed => {
      if (!confirmed) return;
      this.state.saving.set(true);
      this.svc.publishEdition(this.edition()!.id).subscribe({
        next: () => { this.reload(); this.state.saving.set(false); },
        error: (err) => this.handleError(ERROR.publishEdition, err),
      });
    });
  }

  unpublish(): void {
    this.confirmSvc.confirm({
      title:        this.PAGE.unpublishConfirmTitle,
      message:      this.PAGE.unpublishConfirmMessage,
      confirmLabel: this.PAGE.unpublishAction,
    }).subscribe(confirmed => {
      if (!confirmed) return;
      this.state.saving.set(true);
      this.svc.unpublishEdition(this.edition()!.id).subscribe({
        next: () => { this.reload(); this.state.saving.set(false); },
        error: (err) => this.handleError(ERROR.unpublishEdition, err),
      });
    });
  }

  registrationOpen(type: 'organiser' | 'staff' | 'visitor'): boolean {
    const e = this.edition();
    if (!e) return false;
    return { organiser: e.organiserRegistrationOpen, staff: e.staffRegistrationOpen, visitor: e.visitorRegistrationOpen }[type];
  }

  registrationStatusIcon(type: 'organiser' | 'staff' | 'visitor'): string {
    return this.registrationOpen(type) ? 'lock_open' : 'lock';
  }

  toggleRegistration(type: 'organiser' | 'staff' | 'visitor'): void {
    if (!this.isPublished()) {
      return;
    }

    this.state.saving.set(true);
    const open = this.registrationOpen(type);
    const call = open
      ? this.svc.closeRegistration(this.edition()!.id, type)
      : this.svc.openRegistration(this.edition()!.id, type);

    call.subscribe({
      next: () => { this.reload(); this.state.saving.set(false); },
      error: (err) => this.handleError(ERROR.toggleRegistration, err),
    });
  }

  // ── Redigera upplaga ─────────────────────────────────────────────────────

  saveEdition(): void {
    if (this.editEditionForm.invalid) return;
    const v = this.editEditionForm.value;
    this.state.saving.set(true);
    this.svc.updateEdition(this.edition()!.id, {
      name: v.name!,
      startDate: v.startDate!,
      endDate: v.endDate!,
      staffCoordinatorId: v.staffCoordinatorId!,
      eventCoordinatorId: v.eventCoordinatorId!,
      scheduleDays: this.editionDayOptions().map(day => ({
        date: day.value,
        startTime: this.toApiTime(this.scheduleDayTimes()[day.value]?.startTime),
        endTime: this.toApiTime(this.scheduleDayTimes()[day.value]?.endTime),
      })),
    }).subscribe({
      next: () => { this.reload(); this.state.saving.set(false); },
      error: (err) => this.handleError(ERROR.updateEdition, err),
    });
  }

  // ── Lokaler ──────────────────────────────────────────────────────────────

  openAddVenueForm(): void {
    this.venueForm.reset();
    this.showAddVenueForm.set(true);
  }

  cancelAddVenueForm(): void {
    this.venueForm.reset();
    this.showAddVenueForm.set(false);
  }

  addVenue(): void {
    if (this.venueForm.invalid) return;
    const v = this.venueForm.value;
    this.state.saving.set(true);
    this.svc.createVenue(this.edition()!.id, {
      name: v.name!, building: v.building!, description: v.description || null,
    }).subscribe({
      next: () => { this.reload(); this.cancelAddVenueForm(); this.state.saving.set(false); },
      error: (err) => this.handleError(ERROR.createVenue, err),
    });
  }

  startEditVenue(venue: VenueDto): void {
    this.editVenueForm.setValue({ name: venue.name, building: venue.building, description: venue.description ?? '' });
    this.editingVenue.set(venue);
  }

  saveVenue(): void {
    const target = this.editingVenue();
    if (!target || this.editVenueForm.invalid) return;
    const v = this.editVenueForm.value;
    this.state.saving.set(true);
    this.svc.updateVenue(this.edition()!.id, target.id, {
      name: v.name!, building: v.building!, description: v.description || null,
    }).subscribe({
      next: () => { this.reload(); this.editingVenue.set(null); this.state.saving.set(false); },
      error: (err) => this.handleError(ERROR.updateVenue, err),
    });
  }

  deleteVenue(venue: VenueDto): void {
    this.confirmSvc.confirm({ title: this.PAGE.deleteVenueTitle, message: this.PAGE.deleteVenueMessage(venue.name) })
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.state.saving.set(true);
        this.svc.removeVenue(this.edition()!.id, venue.id).subscribe({
          next: () => { this.reload(); this.state.saving.set(false); },
          error: (err) => this.handleError(ERROR.deleteVenue, err),
        });
      });
  }

  // ── Funktionsområden ─────────────────────────────────────────────────────

  openAddStaffAreaForm(): void {
    this.staffAreaForm.reset();
    this.showAddStaffAreaForm.set(true);
  }

  cancelAddStaffAreaForm(): void {
    this.staffAreaForm.reset();
    this.showAddStaffAreaForm.set(false);
  }

  addStaffArea(): void {
    if (this.staffAreaForm.invalid) return;
    const v = this.staffAreaForm.value;
    this.state.saving.set(true);
    this.svc.createStaffArea(this.edition()!.id, {
      name: v.name!, description: v.description || null, responsibleId: v.responsibleId!,
    }).subscribe({
      next: () => { this.reload(); this.cancelAddStaffAreaForm(); this.state.saving.set(false); },
      error: (err) => this.handleError(ERROR.createStaffArea, err),
    });
  }

  startEditStaffArea(area: StaffAreaDto): void {
    this.editStaffAreaForm.setValue({ name: area.name, description: area.description ?? '', responsibleId: area.responsibleId });
    this.editingStaffArea.set(area);
  }

  saveStaffArea(): void {
    const target = this.editingStaffArea();
    if (!target || this.editStaffAreaForm.invalid) return;
    const v = this.editStaffAreaForm.value;
    this.state.saving.set(true);
    this.svc.updateStaffArea(this.edition()!.id, target.id, {
      name: v.name!, description: v.description || null, responsibleId: v.responsibleId!,
    }).subscribe({
      next: () => { this.reload(); this.editingStaffArea.set(null); this.state.saving.set(false); },
      error: (err) => this.handleError(ERROR.updateStaffArea, err),
    });
  }

  deleteStaffArea(area: StaffAreaDto): void {
    this.confirmSvc.confirm({ title: this.PAGE.deleteStaffAreaTitle, message: this.PAGE.deleteStaffAreaMessage(area.name) })
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.state.saving.set(true);
        this.svc.removeStaffArea(this.edition()!.id, area.id).subscribe({
          next: () => { this.reload(); this.state.saving.set(false); },
          error: (err) => this.handleError(ERROR.deleteStaffArea, err),
        });
      });
  }

  // ── Kategorier ───────────────────────────────────────────────────────────

  openAddCategoryForm(): void {
    this.categoryForm.reset();
    this.showAddCategoryForm.set(true);
  }

  cancelAddCategoryForm(): void {
    this.categoryForm.reset();
    this.showAddCategoryForm.set(false);
  }

  addCategory(): void {
    if (this.categoryForm.invalid) return;
    const v = this.categoryForm.value;
    this.state.saving.set(true);
    this.svc.createCategory(this.edition()!.id, {
      name: v.name!,
      organizerInstructions: v.organizerInstructions || null,
      publicDescription: v.publicDescription || null,
      responsibleId: v.responsibleId!,
    }).subscribe({
      next: () => { this.reload(); this.cancelAddCategoryForm(); this.state.saving.set(false); },
      error: (err) => this.handleError(ERROR.createCategory, err),
    });
  }

  startEditCategory(category: CategoryDto): void {
    this.editCategoryForm.setValue({
      name: category.name,
      organizerInstructions: category.organizerInstructions ?? '',
      publicDescription: category.publicDescription ?? '',
      responsibleId: category.responsibleId,
    });
    this.editingCategory.set(category);
  }

  saveCategory(): void {
    const target = this.editingCategory();
    if (!target || this.editCategoryForm.invalid) return;
    const v = this.editCategoryForm.value;
    this.state.saving.set(true);
    this.svc.updateCategory(this.edition()!.id, target.id, {
      name: v.name!,
      organizerInstructions: v.organizerInstructions || null,
      publicDescription: v.publicDescription || null,
      responsibleId: v.responsibleId!,
    }).subscribe({
      next: () => { this.reload(); this.editingCategory.set(null); this.state.saving.set(false); },
      error: (err) => this.handleError(ERROR.updateCategory, err),
    });
  }

  deleteCategory(category: CategoryDto): void {
    this.confirmSvc.confirm({ title: this.PAGE.deleteCategoryTitle, message: this.PAGE.deleteCategoryMessage(category.name) })
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.state.saving.set(true);
        this.svc.removeCategory(this.edition()!.id, category.id).subscribe({
          next: () => { this.reload(); this.state.saving.set(false); },
          error: (err) => this.handleError(ERROR.deleteCategory, err),
        });
      });
  }

  // ── Biljettyper ──────────────────────────────────────────────────────────

  openAddTicketTypeForm(): void {
    this.resetAddTicketTypeForm();
    this.showAddTicketTypeForm.set(true);
  }

  cancelAddTicketTypeForm(): void {
    this.resetAddTicketTypeForm();
    this.showAddTicketTypeForm.set(false);
  }

  private resetAddTicketTypeForm(): void {
    this.addTicketTypeForm.reset({
      name: '',
      price: 0,
      category: 'Visitor',
      validDays: [],
      allowedCategories: [],
    });
  }

  addTicketType(): void {
    if (this.addTicketTypeForm.invalid) return;
    const v = this.addTicketTypeForm.value;
    const editionId = this.edition()!.id;

    this.state.saving.set(true);
    this.regSvc.createTicketType(editionId, {
      name: v.name!,
      price: Math.round((v.price ?? 0) * 100),
      category: v.category!,
      validDays: this.normalizeSelectedDays(v.validDays),
      allowedCategories: this.normalizeAllowedCategories(v.allowedCategories),
    }).subscribe({
      next: () => {
        this.reload();
        this.cancelAddTicketTypeForm();
        this.state.saving.set(false);
      },
      error: (err) => this.handleError(ERROR.createTicketType, err),
    });
  }

  startEditTicketType(tt: TicketTypeAdminDto): void {
    this.editTicketTypeForm.setValue({
      name: tt.name,
      price: tt.price / 100,
      category: tt.category,
      validDays: tt.validDays ?? [],
      allowedCategories: tt.allowedCategories ?? [],
    });
    this.editingTicketType.set(tt);
  }

  saveTicketType(): void {
    const target = this.editingTicketType();
    if (!target || this.editTicketTypeForm.invalid) return;
    const v = this.editTicketTypeForm.value;
    const editionId = this.edition()!.id;

    this.state.saving.set(true);
    this.regSvc.updateTicketType(editionId, target.id, {
      name: v.name!,
      price: Math.round((v.price ?? 0) * 100),
      category: v.category!,
      validDays: this.normalizeSelectedDays(v.validDays),
      allowedCategories: this.normalizeAllowedCategories(v.allowedCategories),
    }).subscribe({
      next: () => { this.reload(); this.editingTicketType.set(null); this.state.saving.set(false); },
      error: (err) => this.handleError(ERROR.updateTicketType, err),
    });
  }

  deleteTicketType(tt: TicketTypeAdminDto): void {
    this.confirmSvc.confirm({ title: this.PAGE.deleteTicketTypeTitle, message: this.PAGE.deleteTicketTypeMessage(tt.name) })
      .subscribe(confirmed => {
        if (!confirmed) return;
        const editionId = this.edition()!.id;
        this.state.saving.set(true);
        this.regSvc.deleteTicketType(editionId, tt.id).subscribe({
          next: () => { this.reload(); this.state.saving.set(false); },
          error: (err) => this.handleError(ERROR.deleteTicketType, err),
        });
      });
  }

  formatPrice(priceInOre: number): string {
    return (priceInOre / 100).toLocaleString('sv-SE', { style: 'currency', currency: 'SEK', maximumFractionDigits: 0 });
  }

  validDaysLabel(validDays: string[] | null): string {
    if (!validDays || validDays.length === 0) {
      return 'Alla dagar';
    }

    const labelMap = new Map(this.editionDayOptions().map(option => [option.value, option.label]));
    return validDays.map(day => labelMap.get(day) ?? day).join(', ');
  }

  allowedCategoriesLabel(allowedCategories: string[] | null): string {
    if (!allowedCategories || allowedCategories.length === 0) {
      return 'Alla kategorier';
    }

    const categoryMap = new Map((this.edition()?.categories ?? []).map(c => [c.id, c.name]));
    return allowedCategories.map(categoryId => categoryMap.get(categoryId) ?? categoryId).join(', ');
  }

  // ── Hjälpmetoder ─────────────────────────────────────────────────────────

  protected readonly formatDate = formatDate;

  toDateInput(isoDate: string): string {
    return isoDate.substring(0, 10);
  }

  scheduleStartTime(date: string): string {
    return this.scheduleDayTimes()[date]?.startTime ?? '';
  }

  scheduleEndTime(date: string): string {
    return this.scheduleDayTimes()[date]?.endTime ?? '';
  }

  setScheduleStartTime(date: string, startTime: string): void {
    this.scheduleDayTimes.update(days => ({
      ...days,
      [date]: { startTime: startTime || null, endTime: days[date]?.endTime ?? null },
    }));
  }

  setScheduleEndTime(date: string, endTime: string): void {
    this.scheduleDayTimes.update(days => ({
      ...days,
      [date]: { startTime: days[date]?.startTime ?? null, endTime: endTime || null },
    }));
  }

  private toTimeInput(value: string | null): string | null {
    return value?.substring(0, 5) ?? null;
  }

  private toApiTime(value: string | null | undefined): string | null {
    if (!value) return null;
    return value.length === 5 ? `${value}:00` : value;
  }

  private normalizeAllowedCategories(value: string[] | null | undefined): string[] | null {
    if (!value || value.length === 0) {
      return null;
    }

    return [...new Set(value)];
  }

  private normalizeSelectedDays(value: string[] | null | undefined): string[] | null {
    if (!value || value.length === 0) {
      return null;
    }

    return [...new Set(value)];
  }
}
