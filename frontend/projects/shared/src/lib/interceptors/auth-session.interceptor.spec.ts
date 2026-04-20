import {
  HttpEvent,
  HttpErrorResponse,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Observable, firstValueFrom, of, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { SessionStateService } from '../services/session-state.service';
import { authSessionInterceptor } from './auth-session.interceptor';

describe('authSessionInterceptor', () => {
  const router = {
    url: '/events/42',
    navigate: vi.fn().mockResolvedValue(true),
  };

  const auth = {
    logout: vi.fn(),
  };

  const sessionState = {
    clearNetworkError: vi.fn(),
    reportNetworkError: vi.fn(),
    reportForbidden: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: router },
        { provide: AuthService, useValue: auth },
        { provide: SessionStateService, useValue: sessionState },
      ],
    });
  });

  async function runInterceptor(
    request: HttpRequest<unknown>,
    responseFactory: () => Observable<HttpEvent<unknown>>
  ): Promise<void> {
    await firstValueFrom(
      TestBed.runInInjectionContext(() =>
        authSessionInterceptor(request, () => responseFactory())
      )
    );
  }

  it('clears the network banner after a successful response', async () => {
    await runInterceptor(new HttpRequest('GET', '/api/me'), () =>
      of(new HttpResponse({ status: 200 }))
    );

    expect(sessionState.clearNetworkError).toHaveBeenCalledTimes(1);
  });

  it('reports network errors globally', async () => {
    await expect(
      runInterceptor(new HttpRequest('GET', '/api/me'), () =>
        throwError(() => new HttpErrorResponse({ status: 0 }))
      )
    ).rejects.toMatchObject({ status: 0 });

    expect(sessionState.reportNetworkError).toHaveBeenCalledTimes(1);
  });

  it('shows a forbidden banner without logging out on 403', async () => {
    await expect(
      runInterceptor(new HttpRequest('GET', '/api/admin'), () =>
        throwError(() => new HttpErrorResponse({ status: 403 }))
      )
    ).rejects.toMatchObject({ status: 403 });

    expect(sessionState.reportForbidden).toHaveBeenCalledTimes(1);
    expect(auth.logout).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('logs out and redirects on 401 outside auth endpoints', async () => {
    await expect(
      runInterceptor(new HttpRequest('GET', '/api/me'), () =>
        throwError(() => new HttpErrorResponse({ status: 401 }))
      )
    ).rejects.toMatchObject({ status: 401 });

    expect(auth.logout).toHaveBeenCalledTimes(1);
    expect(router.navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: {
        reason: 'session-expired',
        returnUrl: '/events/42',
      },
    });
  });

  it('does not redirect for 401 responses from auth endpoints', async () => {
    await expect(
      runInterceptor(new HttpRequest('POST', '/auth/login', null), () =>
        throwError(() => new HttpErrorResponse({ status: 401 }))
      )
    ).rejects.toMatchObject({ status: 401 });

    expect(auth.logout).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
