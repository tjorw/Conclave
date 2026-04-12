import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ConventionService, PersonDto } from 'shared';

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

  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly showCreateForm = signal(false);

  readonly searchQuery = signal('');
  readonly editingPerson = signal<PersonDto | null>(null);

  readonly filteredPersons = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return this.persons().filter(
      p => !q || p.name.toLowerCase().includes(q) || p.email.toLowerCase().includes(q)
    );
  });

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
        this.error.set('Kunde inte ladda personlistan.');
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
        this.error.set(err?.error?.detail ?? 'Kunde inte skapa person.');
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
        this.error.set(err?.error?.detail ?? 'Kunde inte uppdatera person.');
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
        this.error.set(err?.error?.detail ?? 'Kunde inte avaktivera person.');
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
        this.error.set(err?.error?.detail ?? 'Kunde inte återaktivera person.');
      },
    });
  }
}
