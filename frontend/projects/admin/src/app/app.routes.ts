import { Routes } from '@angular/router';
import { authGuard, adminGuard } from 'shared';

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
        path: 'editions/:id/:section',
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
        path: 'persons/visitors',
        loadComponent: () =>
          import('./features/persons/edition-visitors.component').then(m => m.EditionVisitorsComponent),
      },
      {
        path: 'persons/organisers',
        loadComponent: () =>
          import('./features/persons/edition-organisers.component').then(m => m.EditionOrganisersComponent),
      },
      {
        path: 'persons/staff',
        loadComponent: () =>
          import('./features/persons/edition-staff.component').then(m => m.EditionStaffComponent),
      },
      {
        path: 'persons/responsibles',
        loadComponent: () =>
          import('./features/persons/edition-responsibles.component').then(m => m.EditionResponsiblesComponent),
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
        path: 'sessions',
        loadComponent: () =>
          import('./features/sessions/sessions-overview.component').then(
            m => m.SessionsOverviewComponent
          ),
      },
      {
        path: 'staff-areas',
        loadComponent: () =>
          import('./features/staffing/staff-areas.component').then(
            m => m.StaffAreasComponent
          ),
      },
      {
        path: 'staff-areas/:areaId',
        loadComponent: () =>
          import('./features/staffing/staff-area-detail/staff-area-detail.component').then(
            m => m.StaffAreaDetailComponent
          ),
      },
      {
        path: 'staff-applications',
        loadComponent: () =>
          import('./features/staffing/staff-applications.component').then(
            m => m.StaffApplicationsComponent
          ),
      },
      {
        path: 'registrations',
        loadComponent: () =>
          import('./features/registrations/registrations.component').then(
            m => m.RegistrationsComponent
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
  {
    path: '**',
    loadComponent: () =>
      import('./features/errors/error-page.component').then(m => m.ErrorPageComponent),
    data: { errorCode: '404' },
  },
];
