import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from 'shared';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './confirm-email.component.html',
  styleUrl: './confirm-email.component.scss',
})
export class ConfirmEmailComponent implements OnInit {
  private readonly auth  = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly success = signal(false);
  readonly resending = signal(false);
  readonly resent   = signal(false);

  private email = '';

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    this.email   = params.get('email') ?? '';
    const token  = params.get('token') ?? '';

    if (!this.email || !token) {
      this.loading.set(false);
      return;
    }

    this.auth.confirmEmail({ email: this.email, token }).subscribe({
      next: () => {
        this.success.set(true);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  resend(): void {
    if (!this.email || this.resending()) return;
    this.resending.set(true);
    this.auth.resendConfirmation(this.email).subscribe({
      next: () => {
        this.resending.set(false);
        this.resent.set(true);
      },
      error: () => {
        this.resending.set(false);
      },
    });
  }
}
