import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService, toErrorMessage } from 'shared';
import { LabelsService } from '../../services/labels.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss',
})
export class ResetPasswordComponent implements OnInit, OnDestroy {
  private readonly auth   = inject(AuthService);
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);
  readonly labels = inject(LabelsService).labels;

  readonly loading = signal(false);
  readonly error   = signal<string | null>(null);
  readonly success = signal(false);
  readonly invalidLink = signal(false);

  private email = '';
  private token = '';
  private redirectTimeout: ReturnType<typeof setTimeout> | null = null;

  readonly form = this.fb.group({
    password:        ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required],
  });

  ngOnDestroy(): void {
    if (this.redirectTimeout !== null) {
      clearTimeout(this.redirectTimeout);
    }
  }

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    this.email = params.get('email') ?? '';
    this.token = params.get('token') ?? '';

    if (!this.email || !this.token) {
      this.invalidLink.set(true);
    }
  }

  submit(): void {
    if (this.form.invalid || this.loading()) return;
    const { password, confirmPassword } = this.form.getRawValue();

    if (password !== confirmPassword) {
      this.error.set(this.labels().resetPasswordMismatchError);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth.resetPassword({
      email: this.email,
      token: this.token,
      newPassword: password!,
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
        this.redirectTimeout = setTimeout(() => this.router.navigateByUrl('/login'), 2000);
      },
      error: err => {
        this.error.set(toErrorMessage(err, this.labels().resetInvalidLinkError));
        this.loading.set(false);
      },
    });
  }
}
