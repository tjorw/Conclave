import { Routes } from '@angular/router';
import { authGuard, adminGuard } from 'shared';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    loadComponent: () =>
      import('./layout/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard, adminGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(
            m => m.DashboardComponent
          ),
      },
      {
        path: 'editions/:id',
        loadComponent: () =>
          import('./features/editions/edition-detail/edition-detail.component').then(
            m => m.EditionDetailComponent
          ),
      },
      {
        path: 'persons',
        loadComponent: () =>
          import('./features/persons/persons.component').then(m => m.PersonsComponent),
      },
      {
        path: 'events',
        loadComponent: () =>
          import('./features/events/events.component').then(m => m.EventsComponent),
      },
      {
        path: 'events/:eventId',
        loadComponent: () =>
          import('./features/events/event-detail/event-detail.component').then(
            m => m.EventDetailComponent
          ),
      },
      {
        path: 'staffing',
        loadComponent: () =>
          import('./features/staffing/staffing.component').then(
            m => m.StaffingComponent
          ),
      },
      {
        path: 'feeds',
        loadComponent: () =>
          import('./features/feeds/feeds.component').then(m => m.FeedsComponent),
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },
  { path: '**', redirectTo: '' },
];
