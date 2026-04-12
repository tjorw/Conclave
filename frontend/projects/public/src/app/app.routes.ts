import { Routes } from '@angular/router';
import { authGuard } from 'shared';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/shell.component').then(m => m.ShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/hem/hem.component').then(m => m.HemComponent),
      },
      {
        path: 'login',
        loadComponent: () =>
          import('./features/login/login.component').then(m => m.LoginComponent),
      },
      {
        path: 'mina-sidor',
        canActivate: [authGuard],
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./features/mina-sidor/mina-sidor.component').then(
                m => m.MinaSidorComponent
              ),
          },
        ],
      },
      { path: '**', redirectTo: '' },
    ],
  },
];
