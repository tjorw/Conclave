import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from 'shared';

/** Fångar 401-svar från API:t (utgångna sessioner) och omdirigerar till /unauthorized.
 *  Auth-endpointen undantas – inloggningsformuläret hanterar sina egna 401-fel. */
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError(err => {
      if (err.status === 401 && !req.url.includes('/auth/')) {
        inject(AuthService).logout();
        inject(Router).navigateByUrl('/unauthorized');
      }
      return throwError(() => err);
    })
  );
