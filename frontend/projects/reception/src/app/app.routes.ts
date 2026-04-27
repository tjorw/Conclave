import { Routes } from '@angular/router';
import { authGuard, receptionGuard } from 'shared';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./features/errors/error-page.component').then(m => m.ErrorPageComponent),
    data: { errorCode: '401' },
  },
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./features/errors/error-page.component').then(m => m.ErrorPageComponent),
    data: { errorCode: '403' },
  },
  {
    path: '',
    loadComponent: () =>
      import('./layout/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard, receptionGuard],
    children: [
      {
        path: 'checkin',
        loadComponent: () =>
          import('./features/checkin/checkin.component').then(m => m.CheckinComponent),
      },
      {
        path: 'walkup',
        loadComponent: () =>
          import('./features/walkup/walkup.component').then(m => m.WalkupComponent),
      },
      {
        path: 'responsibles',
        loadComponent: () =>
          import('./features/responsibles/responsibles.component').then(m => m.ResponsiblesComponent),
      },
      {
        path: 'events',
        loadComponent: () =>
          import('./features/events/events.component').then(m => m.EventsComponent),
      },
      {
        path: 'staffing',
        loadComponent: () =>
          import('./features/staffing/staffing.component').then(m => m.StaffingComponent),
      },
      { path: '', redirectTo: 'checkin', pathMatch: 'full' },
    ],
  },
  {
    path: '**',
    loadComponent: () =>
      import('./features/errors/error-page.component').then(m => m.ErrorPageComponent),
    data: { errorCode: '404' },
  },
];
