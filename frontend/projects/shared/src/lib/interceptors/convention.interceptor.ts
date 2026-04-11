import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ENVIRONMENT } from '../environment/environment.token';

export const conventionInterceptor: HttpInterceptorFn = (req, next) => {
  const env = inject(ENVIRONMENT);
  return next(req.clone({
    setHeaders: { 'X-Convention-Id': env.conventionId }
  }));
};
