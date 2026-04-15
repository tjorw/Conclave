import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ConventionDto, ConventionService, PersonDto, EVENT_STATUS_LABEL } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ACTION, CHIP, FIELD, PLACEHOLDER } from '../../labels/ui.labels';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly conventionService = inject(ConventionService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  readonly editionContext = inject(EditionContextService);

  readonly ACTION      = ACTION;
  readonly CHIP        = CHIP;
  readonly FIELD       = FIELD;
  readonly PLACEHOLDER = PLACEHOLDER;

  readonly convention = signal<ConventionDto | null>(null);
  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);

  readonly createForm = this.fb.group({
    name: ['', Validators.required],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    staffCoordinatorId: ['', Validators.required],
    eventCoordinatorId: ['', Validators.required],
  });

  ngOnInit(): void {
    this.conventionService.getCurrentConvention().subscribe({
      next: c => { this.convention.set(c); this.loading.set(false); },
      error: () => { this.error.set('Kunde inte hämta konventionsdata.'); this.loading.set(false); },
    });
    this.conventionService.listPersons().subscribe({
      next: p => this.persons.set(p.filter(x => x.isActive)),
    });
  }

  openEdition(id: string): void {
    this.editionContext.setActive(id);
    this.router.navigate(['/editions', id]);
  }

  create(): void {
    if (this.createForm.invalid) return;
    const v = this.createForm.value;
    this.saving.set(true);
    this.conventionService.createEdition({
      name: v.name!,
      startDate: v.startDate!,
      endDate: v.endDate!,
      staffCoordinatorId: v.staffCoordinatorId!,
      eventCoordinatorId: v.eventCoordinatorId!,
    }).subscribe({
      next: ({ id }) => {
        this.editionContext.reload();
        this.saving.set(false);
        this.router.navigate(['/editions', id]);
      },
      error: err => {
        const detail = (err as { error?: { detail?: string } })?.error?.detail;
        this.error.set(detail ?? 'Kunde inte skapa upplaga.');
        this.saving.set(false);
      },
    });
  }

  statusLabel(status: string): string {
    return EVENT_STATUS_LABEL[status] ?? status;
  }

  statusColor(status: string): string {
    return status === 'Published' ? 'primary' : 'default';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('sv-SE');
  }
}
