import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { SystemTenantService, TenantSignupResponse } from '../../services/system-tenant.service';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.scss',
})
export class SignupComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(SystemTenantService);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<TenantSignupResponse | null>(null);

  readonly form = this.fb.group({
    organizationName: ['', Validators.required],
    subdomain: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]{3,63}$/)]],
    contactName: ['', Validators.required],
    contactEmail: ['', [Validators.required, Validators.email]],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) return;

    this.saving.set(true);
    this.error.set(null);

    this.service.signup({
      organizationName: this.form.value.organizationName!,
      subdomain: this.form.value.subdomain!,
      contactName: this.form.value.contactName!,
      contactEmail: this.form.value.contactEmail!,
    }).subscribe({
      next: result => {
        this.result.set(result);
        this.saving.set(false);
        this.form.reset();
      },
      error: err => {
        const detail = (err as { error?: { detail?: string; title?: string } })?.error?.detail
          ?? (err as { error?: { title?: string } })?.error?.title;

        this.error.set(detail ?? 'Kunde inte skapa signup-forfragan.');
        this.saving.set(false);
      },
    });
  }
}
