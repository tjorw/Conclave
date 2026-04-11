import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
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
  StaffAreaDto,
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
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
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
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);

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

  readonly stationForm = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    staffAreaId: ['', Validators.required],
  });

  readonly categoryForm = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    responsibleId: ['', Validators.required],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loadData(id);
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.svc.getEdition(editionId).subscribe({
      next: e => {
        this.edition.set(e);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Kunde inte hämta upplagedata.');
        this.loading.set(false);
      },
    });
    this.svc.listPersons().subscribe({
      next: p => this.persons.set(p.filter(x => x.isActive)),
    });
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

  addVenue(): void {
    if (this.venueForm.invalid) return;
    const v = this.venueForm.value;
    this.saving.set(true);
    this.svc.createVenue(this.edition()!.id, {
      name: v.name!,
      building: v.building!,
      description: v.description || null,
    }).subscribe({
      next: () => { this.reload(); this.venueForm.reset(); this.saving.set(false); },
      error: (err) => this.handleError('Kunde inte skapa lokal', err),
    });
  }

  addStaffArea(): void {
    if (this.staffAreaForm.invalid) return;
    const v = this.staffAreaForm.value;
    this.saving.set(true);
    this.svc.createStaffArea(this.edition()!.id, {
      name: v.name!,
      description: v.description || null,
      responsibleId: v.responsibleId!,
    }).subscribe({
      next: () => { this.reload(); this.staffAreaForm.reset(); this.saving.set(false); },
      error: (err) => this.handleError('Kunde inte skapa funktionsområde', err),
    });
  }

  addStation(): void {
    if (this.stationForm.invalid) return;
    const v = this.stationForm.value;
    this.saving.set(true);
    this.svc.createStation(this.edition()!.id, {
      name: v.name!,
      description: v.description || null,
      staffAreaId: v.staffAreaId!,
    }).subscribe({
      next: () => { this.reload(); this.stationForm.reset(); this.saving.set(false); },
      error: (err) => this.handleError('Kunde inte skapa station', err),
    });
  }

  addCategory(): void {
    if (this.categoryForm.invalid) return;
    const v = this.categoryForm.value;
    this.saving.set(true);
    this.svc.createCategory(this.edition()!.id, {
      name: v.name!,
      description: v.description || null,
      responsibleId: v.responsibleId!,
    }).subscribe({
      next: () => { this.reload(); this.categoryForm.reset(); this.saving.set(false); },
      error: (err) => this.handleError('Kunde inte skapa kategori', err),
    });
  }

  changeResponsible(categoryId: string, newId: string): void {
    this.svc.changeCategoryResponsible(this.edition()!.id, categoryId, newId).subscribe({
      next: () => this.reload(),
      error: (err) => this.handleError('Kunde inte byta ansvarig', err),
    });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('sv-SE');
  }

  registrationOpen(type: 'organiser' | 'staff' | 'visitor'): boolean {
    const e = this.edition();
    if (!e) return false;
    return { organiser: e.organiserRegistrationOpen, staff: e.staffRegistrationOpen, visitor: e.visitorRegistrationOpen }[type];
  }
}
