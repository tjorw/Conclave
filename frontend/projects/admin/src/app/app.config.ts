import { APP_INITIALIZER, ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideMarkdown } from 'ngx-markdown';

import { routes } from './app.routes';
import { ConventionContextService, ENVIRONMENT, tenantDevInterceptor, conventionInterceptor, authInterceptor, authSessionInterceptor } from 'shared';
import { environment } from '../environments/environment';

function loadConventionId(conventionContext: ConventionContextService) {
  return () => conventionContext.load();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([tenantDevInterceptor, conventionInterceptor, authInterceptor, authSessionInterceptor])),
    { provide: ENVIRONMENT, useValue: environment },
    {
      provide: APP_INITIALIZER,
      useFactory: loadConventionId,
      deps: [ConventionContextService],
      multi: true,
    },
    provideMarkdown(),
  ],
};
