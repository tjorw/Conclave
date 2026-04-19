import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const systemAdminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isSystemAdmin()) return true;

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
