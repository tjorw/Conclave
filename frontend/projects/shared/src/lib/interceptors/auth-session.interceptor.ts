import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authSessionInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/auth/')) {
        const auth = inject(AuthService);
        const router = inject(Router);
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
