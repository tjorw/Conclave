import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { SessionStateService } from '../services/session-state.service';

@Component({
  selector: 'app-global-status-banner',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  template: `
    @if (sessionState.hasBanner()) {
      <div class="banner-stack">
        @if (sessionState.showExpiryWarning()) {
          <section class="banner banner-warning" role="status" aria-live="polite">
            <div class="banner-copy">
              <mat-icon>schedule</mat-icon>
              <span>{{ sessionState.expiryWarningMessage() }}</span>
            </div>
            <div class="banner-actions">
              <button mat-button type="button" (click)="goToLogin()">Logga in igen</button>
              <button mat-icon-button type="button" aria-label="Dölj varning" (click)="sessionState.dismissExpiryWarning()">
                <mat-icon>close</mat-icon>
              </button>
            </div>
          </section>
        }

        @if (sessionState.sessionExpired()) {
          <section class="banner banner-danger" role="alert">
            <div class="banner-copy">
              <mat-icon>lock_clock</mat-icon>
              <span>Sessionen har gått ut. Logga in igen för att fortsätta.</span>
            </div>
            <div class="banner-actions">
              <button mat-button type="button" (click)="goToLogin()">Till login</button>
              <button mat-icon-button type="button" aria-label="Stäng meddelande" (click)="sessionState.dismissSessionExpired()">
                <mat-icon>close</mat-icon>
              </button>
            </div>
          </section>
        }

        @if (sessionState.forbiddenMessage(); as forbiddenMessage) {
          <section class="banner banner-neutral" role="alert">
            <div class="banner-copy">
              <mat-icon>block</mat-icon>
              <span>{{ forbiddenMessage }}</span>
            </div>
            <div class="banner-actions">
              <button mat-icon-button type="button" aria-label="Stäng behörighetsmeddelande" (click)="sessionState.clearForbidden()">
                <mat-icon>close</mat-icon>
              </button>
            </div>
          </section>
        }

        @if (sessionState.networkError(); as networkError) {
          <section class="banner banner-neutral" role="alert">
            <div class="banner-copy">
              <mat-icon>wifi_off</mat-icon>
              <span>{{ networkError }}</span>
            </div>
            <div class="banner-actions">
              <button mat-icon-button type="button" aria-label="Stäng nätverksmeddelande" (click)="sessionState.clearNetworkError()">
                <mat-icon>close</mat-icon>
              </button>
            </div>
          </section>
        }
      </div>
    }
  `,
  styles: [`
    .banner-stack {
      display: grid;
      gap: 8px;
      padding: 12px 16px 0;
    }

    .banner {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      padding: 12px 16px;
      border-radius: 12px;
      border: 1px solid transparent;
      align-items: center;
      box-shadow: 0 8px 24px rgba(15, 23, 42, 0.08);
    }

    .banner-copy {
      display: flex;
      gap: 10px;
      align-items: flex-start;
      min-width: 0;
      line-height: 1.4;
    }

    .banner-copy mat-icon {
      flex: 0 0 auto;
      margin-top: 1px;
    }

    .banner-actions {
      display: flex;
      align-items: center;
      gap: 4px;
      flex: 0 0 auto;
    }

    .banner-warning {
      background: #fff7ed;
      border-color: #fdba74;
      color: #9a3412;
    }

    .banner-danger {
      background: #fef2f2;
      border-color: #fca5a5;
      color: #991b1b;
    }

    .banner-neutral {
      background: #eff6ff;
      border-color: #93c5fd;
      color: #1d4ed8;
    }

    @media (max-width: 720px) {
      .banner {
        flex-direction: column;
        align-items: stretch;
      }

      .banner-actions {
        justify-content: flex-end;
      }
    }
  `],
})
export class GlobalStatusBannerComponent {
  readonly sessionState = inject(SessionStateService);
  private readonly router = inject(Router);

  goToLogin(): void {
    const returnUrl = this.router.url || '/';
    void this.router.navigate(['/login'], {
      queryParams: {
        reason: 'session-expired',
        returnUrl,
      },
    });
  }
}
