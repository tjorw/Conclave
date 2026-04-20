import { Injectable, computed, signal } from '@angular/core';
import { JwtClaims } from '../models/auth.models';

const WARNING_LEAD_TIME_MS = 5 * 60_000;
const TIME_FORMATTER = new Intl.DateTimeFormat('sv-SE', {
  hour: '2-digit',
  minute: '2-digit',
});

@Injectable({ providedIn: 'root' })
export class SessionStateService {
  private readonly _isAuthenticated = signal(false);
  private readonly _claims = signal<JwtClaims | null>(null);
  private readonly _expiresAt = signal<Date | null>(null);
  private readonly _showExpiryWarning = signal(false);
  private readonly _sessionExpired = signal(false);
  private readonly _forbiddenMessage = signal<string | null>(null);
  private readonly _networkError = signal<string | null>(null);
  private readonly _warningDismissed = signal(false);
  private readonly _currentToken = signal<string | null>(null);

  private warningTimerId: ReturnType<typeof setTimeout> | null = null;
  private expiryTimerId: ReturnType<typeof setTimeout> | null = null;

  readonly isAuthenticated = computed(() => this._isAuthenticated());
  readonly claims = computed(() => this._claims());
  readonly expiresAt = computed(() => this._expiresAt());
  readonly showExpiryWarning = computed(
    () => this._showExpiryWarning() && !this._warningDismissed()
  );
  readonly sessionExpired = computed(() => this._sessionExpired());
  readonly forbiddenMessage = computed(() => this._forbiddenMessage());
  readonly networkError = computed(() => this._networkError());
  readonly hasBanner = computed(
    () =>
      this.showExpiryWarning() ||
      this.sessionExpired() ||
      this.forbiddenMessage() !== null ||
      this.networkError() !== null
  );
  readonly authStatusLabel = computed(() => {
    if (this.sessionExpired()) return 'Session utgången';
    return this.isAuthenticated() ? 'Inloggad' : 'Inte inloggad';
  });
  readonly authStatusDetail = computed(() => {
    if (this.sessionExpired()) return 'Logga in igen för att fortsätta';

    const expiresAt = this.expiresAt();
    if (!this.isAuthenticated() || !expiresAt) return null;

    return `Till ${TIME_FORMATTER.format(expiresAt)}`;
  });
  readonly expiryWarningMessage = computed(() => {
    const expiresAt = this.expiresAt();
    if (!this.showExpiryWarning() || !expiresAt) return null;

    return `Sessionen går ut ${TIME_FORMATTER.format(expiresAt)}. Spara det du gör och logga in igen om du vill fortsätta utan avbrott.`;
  });

  syncSession(token: string | null, claims: JwtClaims | null, isAuthenticated: boolean): void {
    const tokenChanged = token !== this._currentToken();
    this._currentToken.set(token);
    this._claims.set(claims);
    this._isAuthenticated.set(isAuthenticated);

    if (!token || !claims) {
      this.resetSessionState();
      return;
    }

    const expiresAt = this.toExpirationDate(claims);
    this._expiresAt.set(expiresAt);

    if (tokenChanged) {
      this._warningDismissed.set(false);
      this._forbiddenMessage.set(null);
      this._networkError.set(null);
    }

    if (!isAuthenticated) {
      this.clearTimers();
      this._showExpiryWarning.set(false);
      this._sessionExpired.set(true);
      return;
    }

    this._sessionExpired.set(false);
    this.scheduleSessionTimers(expiresAt);
  }

  reportForbidden(message = 'Du saknar behörighet för den här åtgärden.'): void {
    this._forbiddenMessage.set(message);
  }

  clearForbidden(): void {
    this._forbiddenMessage.set(null);
  }

  reportNetworkError(message = 'Nätverksfel. Kontrollera anslutningen och försök igen.'): void {
    this._networkError.set(message);
  }

  clearNetworkError(): void {
    this._networkError.set(null);
  }

  dismissExpiryWarning(): void {
    this._warningDismissed.set(true);
  }

  dismissSessionExpired(): void {
    this._sessionExpired.set(false);
  }

  resetSessionState(): void {
    this.clearTimers();
    this._expiresAt.set(null);
    this._showExpiryWarning.set(false);
    this._sessionExpired.set(false);
    this._warningDismissed.set(false);
    this._forbiddenMessage.set(null);
    this._networkError.set(null);
  }

  private scheduleSessionTimers(expiresAt: Date | null): void {
    this.clearTimers();

    if (!expiresAt) return;

    const expiresAtMs = expiresAt.getTime();
    const now = Date.now();

    if (expiresAtMs <= now) {
      this._showExpiryWarning.set(false);
      this._sessionExpired.set(true);
      return;
    }

    const warningAt = expiresAtMs - WARNING_LEAD_TIME_MS;
    if (warningAt <= now) {
      this._showExpiryWarning.set(true);
    } else {
      this._showExpiryWarning.set(false);
      this.warningTimerId = setTimeout(() => {
        this._showExpiryWarning.set(true);
      }, warningAt - now);
    }

    this.expiryTimerId = setTimeout(() => {
      this._showExpiryWarning.set(false);
      this._sessionExpired.set(true);
    }, expiresAtMs - now);
  }

  private clearTimers(): void {
    if (this.warningTimerId) {
      clearTimeout(this.warningTimerId);
      this.warningTimerId = null;
    }

    if (this.expiryTimerId) {
      clearTimeout(this.expiryTimerId);
      this.expiryTimerId = null;
    }
  }

  private toExpirationDate(claims: JwtClaims | null): Date | null {
    if (!claims?.exp) return null;

    const timestamp = claims.exp * 1000;
    return Number.isFinite(timestamp) ? new Date(timestamp) : null;
  }
}
