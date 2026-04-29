import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService, EventService } from 'shared';

@Component({
  selector: 'app-accept-invitation',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './accept-invitation.component.html',
  styleUrl: './accept-invitation.component.scss',
})
export class AcceptInvitationComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly events = inject(EventService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly success = signal(false);
  readonly errorMessage = signal<string | null>(null);

  private code = '';

  get isLoggedIn() {
    return this.auth.isLoggedIn();
  }

  get loginQueryParams() {
    return { returnUrl: `/accept-invitation?code=${encodeURIComponent(this.code)}` };
  }

  static readonly SESSION_KEY = 'pendingInviteCode';

  ngOnInit(): void {
    this.code = this.route.snapshot.queryParamMap.get('code') ?? '';

    if (!this.code) {
      this.errorMessage.set('Ogiltig inbjudningslänk.');
      return;
    }

    if (this.auth.isLoggedIn()) {
      sessionStorage.removeItem(AcceptInvitationComponent.SESSION_KEY);
      this.redeem();
    } else {
      sessionStorage.setItem(AcceptInvitationComponent.SESSION_KEY, this.code);
    }
  }

  redeem(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.errorMessage.set(null);

    this.events.redeemCoOrganiserInvitation(this.code).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        const status = err?.status;
        if (status === 404) {
          this.errorMessage.set('Inbjudan hittades inte eller är inte längre giltig.');
        } else if (status === 400) {
          this.errorMessage.set('Inbjudan är inte kopplad till ditt konto. Kontrollera att du är inloggad med rätt e-postadress.');
        } else {
          this.errorMessage.set('Något gick fel. Försök igen senare.');
        }
      },
    });
  }
}
