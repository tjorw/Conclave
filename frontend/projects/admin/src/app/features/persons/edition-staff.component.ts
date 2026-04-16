import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, EditionStaffMemberDto, PersonDto, StaffService, STAFF_APPLICATION_STATUS_LABEL } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { ACTION, FIELD, PLACEHOLDER, TOOLTIP } from '../../labels/ui.labels';

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
  ],
  templateUrl: './edition-staff.component.html',
  styleUrl: './edition-staff.component.scss',
})
export class EditionStaffComponent {
  private readonly svc     = inject(ConventionService);
  private readonly staffSvc = inject(StaffService);
  private readonly fb      = inject(FormBuilder);
  readonly editionContext  = inject(EditionContextService);

  readonly ACTION      = ACTION;
  readonly TOOLTIP     = TOOLTIP;
  readonly FIELD       = FIELD;
  readonly PLACEHOLDER = PLACEHOLDER;

  readonly staff       = signal<EditionStaffMemberDto[]>([]);
  readonly loading     = signal(false);
  readonly error       = signal<string | null>(null);
  readonly searchQuery = signal('');

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
  }

  readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.staff() : this.staff().filter(
      s => s.personName.toLowerCase().includes(q) || s.email.toLowerCase().includes(q)
    );
  });

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  applicationStatusLabel(status: string): string {
    return STAFF_APPLICATION_STATUS_LABEL[status] ?? status;
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
        this.error.set(err?.error?.detail ?? ERROR.addStaffMember);
      },
    });
  }
}
