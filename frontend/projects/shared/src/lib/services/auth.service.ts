import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { ENVIRONMENT } from '../environment/environment.token';
import {
  ChangePasswordRequest,
  ConfirmEmailRequest,
  ForgotPasswordRequest,
  JwtClaims,
  LoginRequest,
  LoginResponse,
  MyProfileResponse,
  RegisterRequest,
  ResetPasswordRequest,
  UpdateProfileRequest,
} from '../models/auth.models';
import { SessionStateService } from './session-state.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);
  private readonly sessionState = inject(SessionStateService);

  private readonly _token = signal<string | null>(sessionStorage.getItem(this.TOKEN_KEY));
  private readonly _expiryTick = signal(Date.now());
  private expiryTimerId: ReturnType<typeof setTimeout> | null = null;

  readonly claims = computed(() => this.parseClaims(this._token()));
  readonly expiresAt = computed(() => this.getExpirationDate(this.claims()));
  readonly isLoggedIn = computed(() => {
    this._expiryTick();
    const claims = this.claims();
    if (!claims) return false;

    const expiresAt = this.expiresAt();
    return !expiresAt || expiresAt.getTime() > Date.now();
  });
  readonly isAdmin = computed(() => this.claims()?.is_admin === 'true');
  readonly isSystemAdmin = computed(() => this.claims()?.is_system_admin === 'true');
  readonly personId = computed(() => this.claims()?.person_id ?? null);

  constructor() {
    effect(() => {
      const token = this._token();
      const claims = this.claims();
      const expiresAt = this.expiresAt();
      const isLoggedIn = this.isLoggedIn();

      this.scheduleExpiryTick(expiresAt);

      if (token && !claims) {
        this.clearStoredToken();
        this.sessionState.syncSession(null, null, false);
        return;
      }

      this.sessionState.syncSession(token, claims, isLoggedIn);
    });
  }

  login(request: LoginRequest) {
    return this.http
      .post<LoginResponse>(`${this.env.apiBaseUrl}/auth/login`, request)
      .pipe(tap(res => this.storeToken(res.token)));
  }

  loginSystem(request: LoginRequest) {
    return this.http
      .post<LoginResponse>(`${this.env.apiBaseUrl}/system/auth/login`, request)
      .pipe(tap(res => this.storeToken(res.token)));
  }

  register(request: RegisterRequest) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/auth/register`, request);
  }

  confirmEmail(request: ConfirmEmailRequest) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/auth/confirm-email`, request);
  }

  resendConfirmation(email: string) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/auth/resend-confirmation`, { email });
  }

  forgotPassword(request: ForgotPasswordRequest) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/auth/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/auth/reset-password`, request);
  }

  changePassword(request: ChangePasswordRequest) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/auth/password`, request);
  }

  getProfile() {
    return this.http.get<MyProfileResponse>(`${this.env.apiBaseUrl}/me/profile`);
  }

  updateProfile(request: UpdateProfileRequest) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/me/profile`, request);
  }

  logout(): void {
    this.clearStoredToken();
  }

  getToken(): string | null {
    return this._token();
  }

  private storeToken(token: string): void {
    sessionStorage.setItem(this.TOKEN_KEY, token);
    this._token.set(token);
  }

  private clearStoredToken(): void {
    if (this.expiryTimerId) {
      clearTimeout(this.expiryTimerId);
      this.expiryTimerId = null;
    }

    sessionStorage.removeItem(this.TOKEN_KEY);
    this._token.set(null);
    this._expiryTick.set(Date.now());
  }

  private scheduleExpiryTick(expiresAt: Date | null): void {
    if (this.expiryTimerId) {
      clearTimeout(this.expiryTimerId);
      this.expiryTimerId = null;
    }

    if (!expiresAt) return;

    const delayMs = expiresAt.getTime() - Date.now();
    if (delayMs <= 0) {
      this._expiryTick.set(Date.now());
      return;
    }

    this.expiryTimerId = setTimeout(() => {
      this._expiryTick.set(Date.now());
    }, delayMs);
  }

  private parseClaims(token: string | null): JwtClaims | null {
    if (!token) return null;

    try {
      const payload = token.split('.')[1];
      if (!payload) return null;

      return JSON.parse(this.decodeBase64Url(payload)) as JwtClaims;
    } catch {
      return null;
    }
  }

  private getExpirationDate(claims: JwtClaims | null): Date | null {
    if (!claims?.exp) return null;

    const timestamp = claims.exp * 1000;
    return Number.isFinite(timestamp) ? new Date(timestamp) : null;
  }

  private decodeBase64Url(value: string): string {
    const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
    const padding = normalized.length % 4 === 0
      ? ''
      : '='.repeat(4 - (normalized.length % 4));

    return atob(`${normalized}${padding}`);
  }
}
