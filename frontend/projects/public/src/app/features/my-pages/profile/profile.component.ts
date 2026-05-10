import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService, toErrorMessage } from 'shared';
import { LabelsService } from '../../../services/labels.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {
  private readonly auth       = inject(AuthService);
  private readonly route      = inject(ActivatedRoute);
  private readonly fb         = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  readonly labels = inject(LabelsService).labels;

  readonly onboarding = signal(false);

  readonly loadingProfile  = signal(true);
  readonly savingProfile   = signal(false);
  readonly profileSaved    = signal(false);
  readonly profileError    = signal<string | null>(null);

  readonly savingPassword  = signal(false);
  readonly passwordSaved   = signal(false);
  readonly passwordError   = signal<string | null>(null);

  readonly profileForm = this.fb.group({
    name:  ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
  });

  readonly passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword:     ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required],
  });

  ngOnInit(): void {
    this.onboarding.set(this.route.snapshot.queryParamMap.get('onboarding') === 'true');

    this.auth.getProfile().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: profile => {
        this.profileForm.setValue({
          name:  profile.name,
          email: profile.email,
          phone: profile.phone ?? '',
        });
        this.loadingProfile.set(false);
      },
      error: () => this.loadingProfile.set(false),
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid || this.savingProfile()) return;
    this.savingProfile.set(true);
    this.profileSaved.set(false);
    this.profileError.set(null);
    const { name, email, phone } = this.profileForm.getRawValue();
    this.auth.updateProfile({ name: name!, email: email!, phone: phone || null }).subscribe({
      next: () => {
        this.savingProfile.set(false);
        this.profileSaved.set(true);
      },
      error: err => {
        this.profileError.set(toErrorMessage(err, 'Kunde inte spara profilen.'));
        this.savingProfile.set(false);
      },
    });
  }

  savePassword(): void {
    if (this.passwordForm.invalid || this.savingPassword()) return;
    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.getRawValue();

    if (newPassword !== confirmPassword) {
      this.passwordError.set('De nya lösenorden matchar inte.');
      return;
    }

    this.savingPassword.set(true);
    this.passwordSaved.set(false);
    this.passwordError.set(null);

    this.auth.changePassword({ currentPassword: currentPassword!, newPassword: newPassword! }).subscribe({
      next: () => {
        this.savingPassword.set(false);
        this.passwordSaved.set(true);
        this.passwordForm.reset();
      },
      error: err => {
        this.passwordError.set(toErrorMessage(err, 'Kunde inte byta lösenord.'));
        this.savingPassword.set(false);
      },
    });
  }
}
