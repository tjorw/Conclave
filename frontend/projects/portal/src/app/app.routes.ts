import { Routes } from '@angular/router';
import { authGuard, systemAdminGuard } from 'shared';

export const routes: Routes = [
	{
		path: 'signup',
		loadComponent: () =>
			import('./features/signup/signup.component').then(m => m.SignupComponent),
	},
	{
		path: 'signup/confirm-email',
		loadComponent: () =>
			import('./features/signup-confirm-email/signup-confirm-email.component').then(m => m.SignupConfirmEmailComponent),
	},
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
		canActivate: [authGuard, systemAdminGuard],
		children: [
			{
				path: 'dashboard',
				loadComponent: () =>
					import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
			},
			{
				path: 'tenants',
				loadComponent: () =>
					import('./features/tenants/tenants.component').then(m => m.TenantsComponent),
			},
			{
				path: 'tenants/:tenantId',
				loadComponent: () =>
					import('./features/tenant-detail/tenant-detail.component').then(m => m.TenantDetailComponent),
			},
			{
				path: 'tenants/:tenantId/admins',
				loadComponent: () =>
					import('./features/tenant-detail/tenant-detail.component').then(m => m.TenantDetailComponent),
			},
			{
				path: 'tenants/:tenantId/provision',
				loadComponent: () =>
					import('./features/tenant-provision/tenant-provision.component').then(m => m.TenantProvisionComponent),
			},
			{ path: '', redirectTo: 'tenants', pathMatch: 'full' },
		],
	},
	{
		path: '**',
		loadComponent: () =>
			import('./features/errors/error-page.component').then(m => m.ErrorPageComponent),
		data: { errorCode: '404' },
	},
];
