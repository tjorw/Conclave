import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { authGuard, adminGuard } from 'shared';
import { EditionContextService } from './services/edition-context.service';

const editionContextReadyGuard = () => inject(EditionContextService).load().then(() => true);

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
    canActivate: [authGuard, adminGuard, editionContextReadyGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
      },

      // ── Upplaga ──────────────────────────────────────────────────────────────
      { path: 'editions/:id', redirectTo: 'editions/:id/basics', pathMatch: 'full' },
      {
        path: 'editions/:id/basics',
        loadComponent: () =>
          import('./features/editions/basics/edition-basics.component').then(m => m.EditionBasicsComponent),
      },
      {
        path: 'editions/:id/lifecycle',
        loadComponent: () =>
          import('./features/editions/lifecycle/edition-lifecycle.component').then(m => m.EditionLifecycleComponent),
      },
      {
        path: 'editions/:id/venues',
        loadComponent: () =>
          import('./features/editions/venues/venues.component').then(m => m.VenuesComponent),
      },
      {
        path: 'editions/:id/venues/:venueId',
        loadComponent: () =>
          import('./features/editions/venues/venue-detail/venue-detail.component').then(m => m.VenueDetailComponent),
      },
      {
        path: 'editions/:id/staff-areas',
        loadComponent: () =>
          import('./features/editions/edition-staff-areas/edition-staff-areas.component').then(m => m.EditionStaffAreasComponent),
      },
      {
        path: 'editions/:id/staff-areas/:areaId',
        loadComponent: () =>
          import('./features/editions/edition-staff-areas/edition-staff-area-detail/edition-staff-area-detail.component').then(m => m.EditionStaffAreaDetailComponent),
      },
      {
        path: 'editions/:id/categories',
        loadComponent: () =>
          import('./features/editions/categories/categories.component').then(m => m.CategoriesComponent),
      },
      {
        path: 'editions/:id/categories/:categoryId',
        loadComponent: () =>
          import('./features/editions/categories/category-detail/category-detail.component').then(m => m.CategoryDetailComponent),
      },
      {
        path: 'editions/:id/tags',
        loadComponent: () =>
          import('./features/editions/program-tag-definitions/program-tag-definitions.component').then(m => m.ProgramTagDefinitionsComponent),
      },
      {
        path: 'editions/:id/tags/:tagName',
        loadComponent: () =>
          import('./features/editions/program-tag-definitions/program-tag-definition-detail/program-tag-definition-detail.component').then(m => m.ProgramTagDefinitionDetailComponent),
      },
      {
        path: 'editions/:id/ticket-types',
        loadComponent: () =>
          import('./features/editions/ticket-types/ticket-types.component').then(m => m.TicketTypesComponent),
      },
      {
        path: 'editions/:id/export',
        loadComponent: () =>
          import('./features/editions/export/edition-export.component').then(m => m.EditionExportComponent),
      },
      {
        path: 'editions/:id/ticket-types/:ticketTypeId',
        loadComponent: () =>
          import('./features/editions/ticket-types/ticket-type-detail/ticket-type-detail.component').then(m => m.TicketTypeDetailComponent),
      },

      // ── Personer ─────────────────────────────────────────────────────────────
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
        path: 'persons/staff/:applicationId',
        loadComponent: () =>
          import('./features/staffing/staff-application-detail/staff-application-detail.component').then(m => m.StaffApplicationDetailComponent),
      },
      {
        path: 'persons/reception-staff',
        loadComponent: () =>
          import('./features/persons/edition-reception-staff.component').then(m => m.EditionReceptionStaffComponent),
      },
      {
        path: 'persons/:personId',
        loadComponent: () =>
          import('./features/persons/person-detail/person-detail.component').then(m => m.PersonDetailComponent),
      },

      // ── Evenemang ────────────────────────────────────────────────────────────
      {
        path: 'events',
        loadComponent: () =>
          import('./features/events/events.component').then(m => m.EventsComponent),
      },
      {
        path: 'events/:eventId',
        loadComponent: () =>
          import('./features/events/event-detail/event-detail.component').then(m => m.EventDetailComponent),
      },

      // ── Schema ───────────────────────────────────────────────────────────────
      {
        path: 'sessions',
        loadComponent: () =>
          import('./features/sessions/sessions-overview.component').then(m => m.SessionsOverviewComponent),
      },

      // ── Bemanning ────────────────────────────────────────────────────────────
      {
        path: 'staff-areas',
        loadComponent: () =>
          import('./features/staffing/staff-function-areas.component').then(m => m.StaffFunctionAreasComponent),
      },
      {
        path: 'staffing-schedule',
        loadComponent: () =>
          import('./features/staffing/staff-areas.component').then(m => m.StaffAreasComponent),
      },
      {
        path: 'staff-areas/:areaId',
        loadComponent: () =>
          import('./features/staffing/staff-area-detail/staff-area-detail.component').then(m => m.StaffAreaDetailComponent),
      },
      {
        path: 'staff-applications',
        redirectTo: 'persons/staff',
        pathMatch: 'full',
      },
      {
        path: 'staff-applications/:applicationId',
        loadComponent: () =>
          import('./features/staffing/staff-application-detail/staff-application-detail.component').then(m => m.StaffApplicationDetailComponent),
      },

      // ── Besökare ─────────────────────────────────────────────────────────────
      { path: 'registrations', redirectTo: 'registrations/visitors', pathMatch: 'full' },
      {
        path: 'registrations/visitors',
        loadComponent: () =>
          import('./features/registrations/registrations.component').then(m => m.RegistrationsComponent),
        data: { page: 'visitors' },
      },
      {
        path: 'registrations/promotion-codes',
        loadComponent: () =>
          import('./features/registrations/registrations.component').then(m => m.RegistrationsComponent),
        data: { page: 'promotion-codes' },
      },

      // ── Feeds ────────────────────────────────────────────────────────────────
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
