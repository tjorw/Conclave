import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { authGuard } from 'shared';
import { EditionService } from './services/edition.service';

const editionReadyGuard = () => inject(EditionService).load().then(() => true);

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/shell.component').then(m => m.ShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/home/home.component').then(m => m.HomeComponent),
      },
      {
        path: 'program',
        loadComponent: () =>
          import('./features/program/program.component').then(m => m.ProgramComponent),
      },
      {
        path: 'program/:id',
        loadComponent: () =>
          import('./features/program/event-detail/event-detail.component').then(
            m => m.EventDetailComponent
          ),
      },
      {
        path: 'pages/:slug',
        loadComponent: () =>
          import('./features/pages/public-page.component').then(
            m => m.PublicPageComponent
          ),
      },
      {
        path: 'login',
        loadComponent: () =>
          import('./features/login/login.component').then(m => m.LoginComponent),
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./features/register/register.component').then(m => m.RegisterComponent),
      },
      {
        path: 'confirm-email',
        loadComponent: () =>
          import('./features/confirm-email/confirm-email.component').then(
            m => m.ConfirmEmailComponent
          ),
      },
      {
        path: 'accept-invitation',
        loadComponent: () =>
          import('./features/accept-invitation/accept-invitation.component').then(
            m => m.AcceptInvitationComponent
          ),
      },
      {
        path: 'forgot-password',
        loadComponent: () =>
          import('./features/forgot-password/forgot-password.component').then(
            m => m.ForgotPasswordComponent
          ),
      },
      {
        path: 'reset-password',
        loadComponent: () =>
          import('./features/reset-password/reset-password.component').then(
            m => m.ResetPasswordComponent
          ),
      },
      {
        path: 'my-pages',
        canActivate: [authGuard, editionReadyGuard],
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./features/my-pages/my-pages.component').then(
                m => m.MyPagesComponent
              ),
          },
          {
            path: 'profile',
            loadComponent: () =>
              import('./features/my-pages/profile/profile.component').then(
                m => m.ProfileComponent
              ),
          },
          {
            path: 'ticket',
            loadComponent: () =>
              import('./features/my-pages/ticket/my-ticket.component').then(
                m => m.MyTicketComponent
              ),
          },
          {
            path: 'program',
            loadComponent: () =>
              import('./features/my-pages/program/my-program.component').then(
                m => m.MyProgramComponent
              ),
          },
          {
            path: 'staff',
            loadComponent: () =>
              import('./features/my-pages/staff/my-staff.component').then(
                m => m.MyStaffComponent
              ),
          },
          {
            path: 'events/new',
            loadComponent: () =>
              import('./features/my-pages/my-events/create/create-event.component').then(
                m => m.CreateEventComponent
              ),
          },
          {
            path: 'events/:id',
            loadComponent: () =>
              import('./features/my-pages/my-events/detail/my-event-detail.component').then(
                m => m.MyEventDetailComponent
              ),
          },
          {
            path: 'events',
            loadComponent: () =>
              import('./features/my-pages/my-events/my-events.component').then(
                m => m.MyEventsComponent
              ),
          },
        ],
      },
      { path: '**', redirectTo: '' },
    ],
  },
];
