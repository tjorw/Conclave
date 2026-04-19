import { Routes } from '@angular/router';
import { authGuard, systemAdminGuard } from 'shared';

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
		canActivate: [authGuard, systemAdminGuard],
		children: [
			{
				path: 'dashboard',
				loadComponent: () =>
					import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
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
