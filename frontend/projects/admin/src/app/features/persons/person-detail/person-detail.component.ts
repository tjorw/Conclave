import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService, ConventionService, PersonDto, toErrorMessage } from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { ACTION, CHIP, TOOLTIP } from '../../../labels/ui.labels';

@Component({
  selector: 'app-person-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './person-detail.component.html',
  styleUrl: './person-detail.component.scss',
})
export class PersonDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(ConventionService);
  private readonly auth = inject(AuthService);

  readonly ACTION  = ACTION;
  readonly TOOLTIP = TOOLTIP;
  readonly CHIP    = CHIP;

  readonly person  = signal<PersonDto | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);
  readonly saving  = signal(false);

  readonly currentPersonId = this.auth.personId;
  private personId = '';

  readonly isNew = computed(() => this.personId === 'new');

  readonly form = this.fb.group({
    name:  ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
  });

  ngOnInit(): void {
    this.route.paramMap.pipe(map(p => p.get('personId')!)).subscribe(id => {
      this.personId = id;
      this.loadData();
    });
  }

  private loadData(): void {
    if (this.isNew()) {
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.svc.listPersons().subscribe({
      next: persons => {
        const person = persons.find(p => p.id === this.personId);
        if (person) {
          this.person.set(person);
          this.form.setValue({ name: person.name, email: person.email, phone: person.phone ?? '' });
        } else {
          this.error.set('Personen hittades inte.');
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set(ERROR.fetchPersons);
        this.loading.set(false);
      },
    });
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    const { name, email, phone } = this.form.getRawValue();
    const payload = { name: name!, email: email!, phone: phone || null };
    this.saving.set(true);

    if (this.isNew()) {
      this.svc.createPerson(payload).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => {
          this.error.set(toErrorMessage(err, ERROR.createPerson));
          this.saving.set(false);
        },
      });
    } else {
      this.svc.updatePerson(this.personId, payload).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => {
          this.error.set(toErrorMessage(err, ERROR.updatePerson));
          this.saving.set(false);
        },
      });
    }
  }

  deactivate(): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.deactivatePerson(this.personId).subscribe({
      next: () => { this.saving.set(false); this.loadData(); },
      error: (err: unknown) => {
        this.error.set(toErrorMessage(err, ERROR.deactivatePerson));
        this.saving.set(false);
      },
    });
  }

  reactivate(): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.reactivatePerson(this.personId).subscribe({
      next: () => { this.saving.set(false); this.loadData(); },
      error: (err: unknown) => {
        this.error.set(toErrorMessage(err, ERROR.reactivatePerson));
        this.saving.set(false);
      },
    });
  }

  sendResetLink(): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.sendResetLink(this.personId).subscribe({
      next: () => { this.saving.set(false); this.error.set(null); },
      error: (err: unknown) => {
        this.error.set(toErrorMessage(err, ERROR.sendResetLink));
        this.saving.set(false);
      },
    });
  }

  toggleLock(): void {
    const p = this.person();
    if (!p || this.saving()) return;
    this.saving.set(true);
    const action = p.isLocked ? this.svc.unlockAccount(p.id) : this.svc.lockAccount(p.id);
    action.subscribe({
      next: () => { this.saving.set(false); this.loadData(); },
      error: (err: unknown) => {
        this.error.set(toErrorMessage(err, ERROR.setLock));
        this.saving.set(false);
      },
    });
  }

  makeAdmin(): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.svc.addAdministrator(this.personId).subscribe({
      next: () => { this.saving.set(false); this.loadData(); },
      error: (err: unknown) => {
        this.error.set(toErrorMessage(err, ERROR.setAdmin));
        this.saving.set(false);
      },
    });
  }

  removeAdmin(): void {
    if (this.saving()) return;
    if (this.personId === this.currentPersonId()) {
      this.error.set('Du kan inte ta bort dig själv som admin.');
      return;
    }
    this.saving.set(true);
    this.svc.removeAdministrator(this.personId).subscribe({
      next: () => { this.saving.set(false); this.loadData(); },
      error: (err: unknown) => {
        this.error.set(toErrorMessage(err, ERROR.setAdmin));
        this.saving.set(false);
      },
    });
  }

  navigateBack(): void {
    void this.router.navigate(['/persons']);
  }
}
