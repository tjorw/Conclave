import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, of, switchMap } from 'rxjs';
import { AuthService } from 'shared';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);

  readonly loading = signal(false);
  readonly error   = signal<string | null>(null);

  readonly form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  login(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);
    this.error.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login({ email: email!, password: password! }).pipe(
      switchMap(() => this.auth.getProfile().pipe(catchError(() => of(null))))
    ).subscribe({
      next: profile => {
        this.loading.set(false);
        const target = !profile?.name
          ? '/my-pages/profile?onboarding=true'
          : '/my-pages';
        this.router.navigateByUrl(target);
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 403) {
          this.error.set('E-postadressen är inte bekräftad. Kontrollera din inkorg.');
        } else {
          this.error.set('Felaktig e-post eller lösenord.');
        }
        this.loading.set(false);
      },
    });
  }
}
