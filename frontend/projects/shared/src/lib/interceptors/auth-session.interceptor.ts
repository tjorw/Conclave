import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, tap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { SessionStateService } from '../services/session-state.service';

function isAuthEndpoint(url: string): boolean {
  return url.includes('/auth/');
}

export const authSessionInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const sessionState = inject(SessionStateService);

  return next(req).pipe(
    tap(event => {
      if (event instanceof HttpResponse) {
        sessionState.clearNetworkError();
      }
    }),
    catchError((error: HttpErrorResponse) => {
      if (error.status === 0) {
        sessionState.reportNetworkError();
      }

      if (error.status === 403 && !isAuthEndpoint(req.url)) {
        sessionState.reportForbidden();
      }

      if (error.status === 401 && !isAuthEndpoint(req.url)) {
        const returnUrl = router.url || '/';

        auth.logout();
        void router.navigate(['/login'], {
          queryParams: {
            reason: 'session-expired',
            returnUrl,
          },
        });
      }

      return throwError(() => error);
    })
  );
};
