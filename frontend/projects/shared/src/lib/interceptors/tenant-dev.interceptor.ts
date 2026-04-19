import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ENVIRONMENT } from '../environment/environment.token';

const tenantIdHeader = 'X-Tenant-ID';

export const tenantDevInterceptor: HttpInterceptorFn = (req, next) => {
  const environment = inject(ENVIRONMENT);
  const multitenancy = environment.multitenancy;

  if (environment.production || !multitenancy?.enabled || !multitenancy.devTenantId || req.headers.has(tenantIdHeader)) {
    return next(req);
  }

  return next(req.clone({
    setHeaders: {
      [tenantIdHeader]: multitenancy.devTenantId,
    },
  }));
};
