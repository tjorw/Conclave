import { HttpHeaders, HttpRequest, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { ENVIRONMENT } from '../environment/environment.token';
import { Environment } from '../environment/environment.model';
import { tenantDevInterceptor } from './tenant-dev.interceptor';

describe('tenantDevInterceptor', () => {
  async function runInterceptor(environment: Environment, request: HttpRequest<unknown>): Promise<HttpRequest<unknown>> {
    let forwardedRequest!: HttpRequest<unknown>;

    TestBed.configureTestingModule({
      providers: [
        { provide: ENVIRONMENT, useValue: environment },
      ],
    });

    await firstValueFrom(
      TestBed.runInInjectionContext(() =>
        tenantDevInterceptor(request, req => {
          forwardedRequest = req;
          return of(new HttpResponse({ status: 200 }));
        })
      )
    );

    return forwardedRequest;
  }

  it('sets X-Tenant-ID when enabled in development and devTenantId exists', async () => {
    const request = new HttpRequest('GET', '/api/test');
    const environment: Environment = {
      production: false,
      apiBaseUrl: 'http://localhost:5127',
      conventionId: '00000000-0000-0000-0000-000000000000',
      multitenancy: {
        enabled: true,
        devTenantId: '11111111-1111-1111-1111-111111111111',
      },
    };

    const forwarded = await runInterceptor(environment, request);

    expect(forwarded.headers.get('X-Tenant-ID')).toBe('11111111-1111-1111-1111-111111111111');
  });

  it('does not set X-Tenant-ID in production', async () => {
    const request = new HttpRequest('GET', '/api/test');
    const environment: Environment = {
      production: true,
      apiBaseUrl: 'http://localhost:5127',
      conventionId: '00000000-0000-0000-0000-000000000000',
      multitenancy: {
        enabled: true,
        devTenantId: '11111111-1111-1111-1111-111111111111',
      },
    };

    const forwarded = await runInterceptor(environment, request);

    expect(forwarded.headers.has('X-Tenant-ID')).toBe(false);
  });

  it('does not set X-Tenant-ID when devTenantId is missing', async () => {
    const request = new HttpRequest('GET', '/api/test');
    const environment: Environment = {
      production: false,
      apiBaseUrl: 'http://localhost:5127',
      conventionId: '00000000-0000-0000-0000-000000000000',
      multitenancy: {
        enabled: true,
      },
    };

    const forwarded = await runInterceptor(environment, request);

    expect(forwarded.headers.has('X-Tenant-ID')).toBe(false);
  });

  it('does not overwrite existing X-Tenant-ID header', async () => {
    const request = new HttpRequest('GET', '/api/test', null, {
      headers: new HttpHeaders({ 'X-Tenant-ID': 'existing-tenant' }),
    });
    const environment: Environment = {
      production: false,
      apiBaseUrl: 'http://localhost:5127',
      conventionId: '00000000-0000-0000-0000-000000000000',
      multitenancy: {
        enabled: true,
        devTenantId: '11111111-1111-1111-1111-111111111111',
      },
    };

    const forwarded = await runInterceptor(environment, request);

    expect(forwarded.headers.get('X-Tenant-ID')).toBe('existing-tenant');
  });
});
