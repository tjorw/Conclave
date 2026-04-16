import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { map } from 'rxjs';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  CategoryDto,
  ConventionService,
  EditionDto,
  PersonDto,
  RegistrationService,
  StaffAreaDto,
  TicketTypeAdminDto,
  VenueDto,
} from 'shared';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ERROR } from '../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../labels/pages.labels';
import { ACTION, FIELD, TOOLTIP } from '../../../labels/ui.labels';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';

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
    MatCheckboxModule,
    MatTabsModule,
    MatTooltipModule,
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
  private readonly dialog = inject(MatDialog);

  private openConfirm(data: ConfirmDialogData) {
    return this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, { data, width: '400px' })
      .afterClosed()
      .pipe(map(result => result === true));
  }

  readonly edition = signal<EditionDto | null>(null);
  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);

  // Edit targets
  readonly editingEdition = signal(false);
  readonly editingVenue = signal<VenueDto | null>(null);
  readonly editingStaffArea = signal<StaffAreaDto | null>(null);
  readonly editingCategory   = signal<CategoryDto | null>(null);

  // Biljettyper (laddas separat – inte en del av EditionDto)
  readonly ticketTypes        = signal<TicketTypeAdminDto[]>([]);
  readonly editingTicketType  = signal<TicketTypeAdminDto | null>(null);

  private handleError(context: string, err: unknown): void {
    const detail = (err as { error?: { detail?: string } })?.error?.detail;
    this.error.set(detail ? `${context}: ${detail}` : context);
    this.saving.set(false);
  }

  readonly ACTION  = ACTION;
  readonly TOOLTIP = TOOLTIP;
  readonly FIELD   = FIELD;
  readonly PAGE    = EDITION_DETAIL;

  readonly isDraft = computed(() => this.edition()?.status === 'Draft');
  readonly isPublished = computed(() => this.edition()?.status === 'Published');

  readonly registrationTypes: { type: 'organiser' | 'staff' | 'visitor'; label: string }[] = [
    { type: 'organiser', label: 'Arrangörsregistrering' },
    { type: 'staff', label: 'Staffregistrering' },
    { type: 'visitor', label: 'Besökarregistrering' },
  ];

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
    description: [''],
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
    description: [''],
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
    name:              ['', Validators.required],
    price:             [0, [Validators.required, Validators.min(0)]],
    category:          ['Visitor', Validators.required],
    isSellable:        [false],
    isPubliclyVisible: [false],
  });

  readonly editTicketTypeForm = this.fb.group({
    name:              ['', Validators.required],
    price:             [0, [Validators.required, Validators.min(0)]],
    isSellable:        [false],
    isPubliclyVisible: [false],
  });

  // ── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loadData(id);
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.svc.getEdition(editionId).subscribe({
      next: e => { this.edition.set(e); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchEdition); this.loading.set(false); },
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
    this.svc.getEdition(id).subscribe({ next: e => this.edition.set(e) });
    this.regSvc.listTicketTypes(id).subscribe({ next: tt => this.ticketTypes.set(tt) });
  }

  personName(id: string): string {
    return this.persons().find(p => p.id === id)?.name ?? id;
  }

  stationsForArea(area: StaffAreaDto): { id: string; name: string; description: string | null }[] {
    return this.edition()?.stations.filter(s => s.staffAreaId === area.id) ?? [];
  }

  // ── Publicering, Aktiv upplaga & Registrering ────────────────────────────

  setActive(): void {
    this.saving.set(true);
    this.svc.setActiveEdition(this.edition()!.id).subscribe({
      next: () => { this.saving.set(false); },
      error: (err) => this.handleError(ERROR.setActiveEdition, err),
    });
  }

  publish(): void {
    this.openConfirm({
      title:        this.PAGE.publishConfirmTitle,
      message:      this.PAGE.publishConfirmMessage,
      confirmLabel: this.PAGE.publishAction,
    }).subscribe(confirmed => {
      if (!confirmed) return;
      this.saving.set(true);
      this.svc.publishEdition(this.edition()!.id).subscribe({
        next: () => { this.reload(); this.saving.set(false); },
        error: (err) => this.handleError(ERROR.publishEdition, err),
      });
    });
  }

  openRegistration(type: 'organiser' | 'staff' | 'visitor'): void {
    this.saving.set(true);
    this.svc.openRegistration(this.edition()!.id, type).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.openRegistration, err),
    });
  }

  registrationOpen(type: 'organiser' | 'staff' | 'visitor'): boolean {
    const e = this.edition();
    if (!e) return false;
    return { organiser: e.organiserRegistrationOpen, staff: e.staffRegistrationOpen, visitor: e.visitorRegistrationOpen }[type];
  }

  toggleEventSubmissions(): void {
    this.saving.set(true);
    const open = this.edition()!.organiserRegistrationOpen;
    const call = open
      ? this.svc.closeEventSubmissions(this.edition()!.id)
      : this.svc.openEventSubmissions(this.edition()!.id);
    call.subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.toggleEventSub, err),
    });
  }

  toggleStaffApplications(): void {
    this.saving.set(true);
    const open = this.edition()!.staffRegistrationOpen;
    const call = open
      ? this.svc.closeStaffApplications(this.edition()!.id)
      : this.svc.openStaffApplications(this.edition()!.id);
    call.subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.toggleStaffApps, err),
    });
  }

  // ── Redigera upplaga ─────────────────────────────────────────────────────

  startEditEdition(): void {
    const e = this.edition()!;
    this.editEditionForm.setValue({
      name: e.name,
      startDate: e.start.substring(0, 10),
      endDate: e.end.substring(0, 10),
      staffCoordinatorId: e.staffCoordinatorId ?? '',
      eventCoordinatorId: e.eventCoordinatorId ?? '',
    });
    this.editingEdition.set(true);
  }

  saveEdition(): void {
    if (this.editEditionForm.invalid) return;
    const v = this.editEditionForm.value;
    this.saving.set(true);
    this.svc.updateEdition(this.edition()!.id, {
      name: v.name!,
      startDate: v.startDate!,
      endDate: v.endDate!,
      staffCoordinatorId: v.staffCoordinatorId!,
      eventCoordinatorId: v.eventCoordinatorId!,
    }).subscribe({
      next: () => { this.reload(); this.editingEdition.set(false); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.updateEdition, err),
    });
  }

  // ── Lokaler ──────────────────────────────────────────────────────────────

  addVenue(): void {
    if (this.venueForm.invalid) return;
    const v = this.venueForm.value;
    this.saving.set(true);
    this.svc.createVenue(this.edition()!.id, {
      name: v.name!, building: v.building!, description: v.description || null,
    }).subscribe({
      next: () => { this.reload(); this.venueForm.reset(); this.saving.set(false); },
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
    this.saving.set(true);
    this.svc.updateVenue(this.edition()!.id, target.id, {
      name: v.name!, building: v.building!, description: v.description || null,
    }).subscribe({
      next: () => { this.reload(); this.editingVenue.set(null); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.updateVenue, err),
    });
  }

  deleteVenue(venue: VenueDto): void {
    this.openConfirm({ title: this.PAGE.deleteVenueTitle, message: this.PAGE.deleteVenueMessage(venue.name) })
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.saving.set(true);
        this.svc.removeVenue(this.edition()!.id, venue.id).subscribe({
          next: () => { this.reload(); this.saving.set(false); },
          error: (err) => this.handleError(ERROR.deleteVenue, err),
        });
      });
  }

  // ── Funktionsområden ─────────────────────────────────────────────────────

  addStaffArea(): void {
    if (this.staffAreaForm.invalid) return;
    const v = this.staffAreaForm.value;
    this.saving.set(true);
    this.svc.createStaffArea(this.edition()!.id, {
      name: v.name!, description: v.description || null, responsibleId: v.responsibleId!,
    }).subscribe({
      next: () => { this.reload(); this.staffAreaForm.reset(); this.saving.set(false); },
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
    this.saving.set(true);
    this.svc.updateStaffArea(this.edition()!.id, target.id, {
      name: v.name!, description: v.description || null, responsibleId: v.responsibleId!,
    }).subscribe({
      next: () => { this.reload(); this.editingStaffArea.set(null); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.updateStaffArea, err),
    });
  }

  deleteStaffArea(area: StaffAreaDto): void {
    this.openConfirm({ title: this.PAGE.deleteStaffAreaTitle, message: this.PAGE.deleteStaffAreaMessage(area.name) })
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.saving.set(true);
        this.svc.removeStaffArea(this.edition()!.id, area.id).subscribe({
          next: () => { this.reload(); this.saving.set(false); },
          error: (err) => this.handleError(ERROR.deleteStaffArea, err),
        });
      });
  }

  // ── Kategorier ───────────────────────────────────────────────────────────

  addCategory(): void {
    if (this.categoryForm.invalid) return;
    const v = this.categoryForm.value;
    this.saving.set(true);
    this.svc.createCategory(this.edition()!.id, {
      name: v.name!, description: v.description || null, responsibleId: v.responsibleId!,
    }).subscribe({
      next: () => { this.reload(); this.categoryForm.reset(); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.createCategory, err),
    });
  }

  startEditCategory(category: CategoryDto): void {
    this.editCategoryForm.setValue({ name: category.name, description: category.description ?? '', responsibleId: category.responsibleId });
    this.editingCategory.set(category);
  }

  saveCategory(): void {
    const target = this.editingCategory();
    if (!target || this.editCategoryForm.invalid) return;
    const v = this.editCategoryForm.value;
    this.saving.set(true);
    this.svc.updateCategory(this.edition()!.id, target.id, {
      name: v.name!, description: v.description || null, responsibleId: v.responsibleId!,
    }).subscribe({
      next: () => { this.reload(); this.editingCategory.set(null); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.updateCategory, err),
    });
  }

  deleteCategory(category: CategoryDto): void {
    this.openConfirm({ title: this.PAGE.deleteCategoryTitle, message: this.PAGE.deleteCategoryMessage(category.name) })
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.saving.set(true);
        this.svc.removeCategory(this.edition()!.id, category.id).subscribe({
          next: () => { this.reload(); this.saving.set(false); },
          error: (err) => this.handleError(ERROR.deleteCategory, err),
        });
      });
  }

  // ── Biljettyper ──────────────────────────────────────────────────────────

  addTicketType(): void {
    if (this.addTicketTypeForm.invalid) return;
    const v = this.addTicketTypeForm.value;
    const editionId = this.edition()!.id;
    this.saving.set(true);
    this.regSvc.createTicketType(editionId, {
      name: v.name!,
      price: Math.round((v.price ?? 0) * 100),
      category: v.category!,
      isSellable: v.isSellable ?? false,
      isPubliclyVisible: v.isPubliclyVisible ?? false,
    }).subscribe({
      next: () => { this.reload(); this.addTicketTypeForm.reset({ category: 'Visitor', price: 0 }); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.createTicketType, err),
    });
  }

  startEditTicketType(tt: TicketTypeAdminDto): void {
    this.editTicketTypeForm.setValue({
      name: tt.name,
      price: tt.price / 100,
      isSellable: tt.isSellable,
      isPubliclyVisible: tt.isPubliclyVisible,
    });
    this.editingTicketType.set(tt);
  }

  saveTicketType(): void {
    const target = this.editingTicketType();
    if (!target || this.editTicketTypeForm.invalid) return;
    const v = this.editTicketTypeForm.value;
    const editionId = this.edition()!.id;
    this.saving.set(true);
    this.regSvc.updateTicketType(editionId, target.id, {
      name: v.name!,
      price: Math.round((v.price ?? 0) * 100),
      isSellable: v.isSellable ?? false,
      isPubliclyVisible: v.isPubliclyVisible ?? false,
    }).subscribe({
      next: () => { this.reload(); this.editingTicketType.set(null); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.updateTicketType, err),
    });
  }

  deleteTicketType(tt: TicketTypeAdminDto): void {
    this.openConfirm({ title: this.PAGE.deleteTicketTypeTitle, message: this.PAGE.deleteTicketTypeMessage(tt.name) })
      .subscribe(confirmed => {
        if (!confirmed) return;
        const editionId = this.edition()!.id;
        this.saving.set(true);
        this.regSvc.deleteTicketType(editionId, tt.id).subscribe({
          next: () => { this.reload(); this.saving.set(false); },
          error: (err) => this.handleError(ERROR.deleteTicketType, err),
        });
      });
  }

  formatPrice(priceInOre: number): string {
    return (priceInOre / 100).toLocaleString('sv-SE', { style: 'currency', currency: 'SEK', maximumFractionDigits: 0 });
  }

  // ── Hjälpmetoder ─────────────────────────────────────────────────────────

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('sv-SE');
  }

  toDateInput(isoDate: string): string {
    return isoDate.substring(0, 10);
  }
}
