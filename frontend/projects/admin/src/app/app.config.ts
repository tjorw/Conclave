import { APP_INITIALIZER, ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { ENVIRONMENT, conventionInterceptor, authInterceptor, ConventionDto } from 'shared';
import { environment } from '../environments/environment';
import { httpErrorInterceptor } from './interceptors/http-error.interceptor';

function loadConventionId(http: HttpClient) {
  return () =>
    http.get<ConventionDto>(`${environment.apiBaseUrl}/convention`).toPromise().then(c => {
      if (c) environment.conventionId = c.id;
    }).catch(() => {
      // Faller tillbaka på värdet i environment.ts om API:t inte svarar
    });
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([conventionInterceptor, authInterceptor, httpErrorInterceptor])),
    { provide: ENVIRONMENT, useValue: environment },
    {
      provide: APP_INITIALIZER,
      useFactory: loadConventionId,
      deps: [HttpClient],
      multi: true,
    },
  ],
};
