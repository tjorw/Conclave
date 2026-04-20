import { TestBed } from '@angular/core/testing';
import { SessionStateService } from './session-state.service';
import { JwtClaims } from '../models/auth.models';

describe('SessionStateService', () => {
  let service: SessionStateService;

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-04-20T12:00:00Z'));

    TestBed.configureTestingModule({});
    service = TestBed.inject(SessionStateService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  function claimsExpiringAt(isoUtc: string): JwtClaims {
    return {
      person_id: 'person-1',
      exp: Math.floor(new Date(isoUtc).getTime() / 1000),
    };
  }

  it('shows a warning when the session enters the warning window', () => {
    service.syncSession('token-1', claimsExpiringAt('2026-04-20T12:06:00Z'), true);

    expect(service.showExpiryWarning()).toBe(false);

    vi.advanceTimersByTime(60_000);

    expect(service.showExpiryWarning()).toBe(true);
    expect(service.expiryWarningMessage()).toContain('Sessionen går ut');
  });

  it('tracks expired sessions without resetting them immediately', () => {
    service.syncSession('token-2', claimsExpiringAt('2026-04-20T11:59:00Z'), false);

    expect(service.sessionExpired()).toBe(true);
    expect(service.authStatusLabel()).toBe('Session utgången');
    expect(service.authStatusDetail()).toBe('Logga in igen för att fortsätta');
  });

  it('clears transient state when the session is removed', () => {
    service.syncSession('token-3', claimsExpiringAt('2026-04-20T12:10:00Z'), true);
    service.reportForbidden('Nope');
    service.reportNetworkError('Offline');

    service.syncSession(null, null, false);

    expect(service.showExpiryWarning()).toBe(false);
    expect(service.sessionExpired()).toBe(false);
    expect(service.forbiddenMessage()).toBeNull();
    expect(service.networkError()).toBeNull();
  });
});
