import { Component, computed, inject, OnInit, signal } from '@angular/core';
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
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  CategoryDto,
  ConventionService,
  EditionDto,
  EditionOrganiserDto,
  EditionResponsibleDto,
  EditionStaffMemberDto,
  EditionVisitorDto,
  PersonDto,
  StaffAreaDto,
  VenueDto,
} from 'shared';

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
    MatTabsModule,
    MatTooltipModule,
  ],
  templateUrl: './edition-detail.component.html',
  styleUrl: './edition-detail.component.scss',
})
export class EditionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(ConventionService);

  readonly edition = signal<EditionDto | null>(null);
  readonly persons = signal<PersonDto[]>([]);
  readonly visitors = signal<EditionVisitorDto[]>([]);
  readonly organisers = signal<EditionOrganiserDto[]>([]);
  readonly staff = signal<EditionStaffMemberDto[]>([]);
  readonly responsibles = signal<EditionResponsibleDto[]>([]);
  readonly roleViewsLoading = signal(false);
  readonly roleViewsSearch = signal('');
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);

  // Edit targets
  readonly editingEdition = signal(false);
  readonly editingVenue = signal<VenueDto | null>(null);
  readonly editingStaffArea = signal<StaffAreaDto | null>(null);
  readonly editingCategory = signal<CategoryDto | null>(null);

  private handleError(context: string, err: unknown): void {
    const detail = (err as { error?: { detail?: string } })?.error?.detail;
    this.error.set(detail ? `${context}: ${detail}` : context);
    this.saving.set(false);
  }

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

  // ── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loadData(id);
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.svc.getEdition(editionId).subscribe({
      next: e => { this.edition.set(e); this.loading.set(false); },
      error: () => { this.error.set('Kunde inte hämta upplagedata.'); this.loading.set(false); },
    });
    this.svc.listPersons().subscribe({
      next: p => this.persons.set(p.filter(x => x.isActive)),
    });
    this.loadRoleViews(editionId);
  }

  private loadRoleViews(editionId: string): void {
    this.roleViewsLoading.set(true);
    let pending = 4;
    const done = () => { if (--pending === 0) this.roleViewsLoading.set(false); };

    this.svc.listEditionVisitors(editionId).subscribe({ next: v => { this.visitors.set(v); done(); }, error: done });
    this.svc.listEditionOrganisers(editionId).subscribe({ next: o => { this.organisers.set(o); done(); }, error: done });
    this.svc.listEditionStaff(editionId).subscribe({ next: s => { this.staff.set(s); done(); }, error: done });
    this.svc.listEditionResponsibles(editionId).subscribe({ next: r => { this.responsibles.set(r); done(); }, error: done });
  }

  readonly filteredVisitors = computed(() => {
    const q = this.roleViewsSearch().toLowerCase();
    return !q ? this.visitors() : this.visitors().filter(
      v => v.personName.toLowerCase().includes(q) || v.email.toLowerCase().includes(q)
    );
  });

  readonly filteredOrganisers = computed(() => {
    const q = this.roleViewsSearch().toLowerCase();
    return !q ? this.organisers() : this.organisers().filter(
      o => o.personName.toLowerCase().includes(q) || o.eventTitle.toLowerCase().includes(q)
    );
  });

  readonly filteredStaff = computed(() => {
    const q = this.roleViewsSearch().toLowerCase();
    return !q ? this.staff() : this.staff().filter(
      s => s.personName.toLowerCase().includes(q) || s.email.toLowerCase().includes(q)
    );
  });

  readonly filteredResponsibles = computed(() => {
    const q = this.roleViewsSearch().toLowerCase();
    return !q ? this.responsibles() : this.responsibles().filter(
      r => r.position.toLowerCase().includes(q) || (r.personName ?? '').toLowerCase().includes(q)
    );
  });

  onRoleViewsSearch(event: Event): void {
    this.roleViewsSearch.set((event.target as HTMLInputElement).value);
  }

  private reload(): void {
    const id = this.edition()!.id;
    this.svc.getEdition(id).subscribe({ next: e => this.edition.set(e) });
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
      error: (err) => this.handleError('Kunde inte sätta aktiv upplaga', err),
    });
  }

  publish(): void {
    if (!confirm('Publicera upplagan? Den kan inte återgå till utkast.')) return;
    this.saving.set(true);
    this.svc.publishEdition(this.edition()!.id).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError('Publicering misslyckades', err),
    });
  }

  openRegistration(type: 'organiser' | 'staff' | 'visitor'): void {
    this.saving.set(true);
    this.svc.openRegistration(this.edition()!.id, type).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError('Kunde inte öppna registrering', err),
    });
  }

  registrationOpen(type: 'organiser' | 'staff' | 'visitor'): boolean {
    const e = this.edition();
    if (!e) return false;
    return { organiser: e.organiserRegistrationOpen, staff: e.staffRegistrationOpen, visitor: e.visitorRegistrationOpen }[type];
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
      error: (err) => this.handleError('Kunde inte uppdatera upplagan', err),
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
      error: (err) => this.handleError('Kunde inte skapa lokal', err),
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
      error: (err) => this.handleError('Kunde inte uppdatera lokal', err),
    });
  }

  deleteVenue(venue: VenueDto): void {
    if (!confirm(`Ta bort lokalen "${venue.name}"?`)) return;
    this.saving.set(true);
    this.svc.removeVenue(this.edition()!.id, venue.id).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError('Kunde inte ta bort lokal', err),
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
      error: (err) => this.handleError('Kunde inte skapa funktionsområde', err),
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
      error: (err) => this.handleError('Kunde inte uppdatera funktionsområde', err),
    });
  }

  deleteStaffArea(area: StaffAreaDto): void {
    if (!confirm(`Ta bort funktionsområdet "${area.name}"? Alla tillhörande stationer tas också bort.`)) return;
    this.saving.set(true);
    this.svc.removeStaffArea(this.edition()!.id, area.id).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError('Kunde inte ta bort funktionsområde', err),
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
      error: (err) => this.handleError('Kunde inte skapa kategori', err),
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
      error: (err) => this.handleError('Kunde inte uppdatera kategori', err),
    });
  }

  deleteCategory(category: CategoryDto): void {
    if (!confirm(`Ta bort kategorin "${category.name}"?`)) return;
    this.saving.set(true);
    this.svc.removeCategory(this.edition()!.id, category.id).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError('Kunde inte ta bort kategori', err),
    });
  }

  // ── Hjälpmetoder ─────────────────────────────────────────────────────────

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('sv-SE');
  }

  toDateInput(isoDate: string): string {
    return isoDate.substring(0, 10);
  }
}
