import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const receptionGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isReception() || auth.isAdmin()) return true;

  if (auth.isLoggedIn()) {
    auth.logout();
    return router.createUrlTree(['/forbidden'], {
      queryParams: { reason: 'role' },
    });
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};
