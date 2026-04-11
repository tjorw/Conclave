import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { ENVIRONMENT } from '../environment/environment.token';
import { JwtClaims, LoginRequest, LoginResponse } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  private readonly _token = signal<string | null>(sessionStorage.getItem(this.TOKEN_KEY));

  readonly isLoggedIn = computed(() => this._token() !== null);
  readonly isAdmin = computed(() => this.getClaims()?.is_admin === 'true');
  readonly personId = computed(() => this.getClaims()?.person_id ?? null);

  login(request: LoginRequest) {
    return this.http
      .post<LoginResponse>(`${this.env.apiBaseUrl}/auth/login`, request)
      .pipe(tap(res => this.storeToken(res.token)));
  }

  logout(): void {
    sessionStorage.removeItem(this.TOKEN_KEY);
    this._token.set(null);
  }

  getToken(): string | null {
    return this._token();
  }

  private storeToken(token: string): void {
    sessionStorage.setItem(this.TOKEN_KEY, token);
    this._token.set(token);
  }

  private getClaims(): JwtClaims | null {
    const token = this._token();
    if (!token) return null;
    try {
      return JSON.parse(atob(token.split('.')[1])) as JwtClaims;
    } catch {
      return null;
    }
  }
}
