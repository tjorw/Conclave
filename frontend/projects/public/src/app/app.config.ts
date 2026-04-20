import { APP_INITIALIZER, ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { ConventionContextService, ENVIRONMENT, tenantDevInterceptor, conventionInterceptor, authInterceptor, authSessionInterceptor } from 'shared';
import { environment } from '../environments/environment';
import { EditionService } from './services/edition.service';

function initEdition(conventionContext: ConventionContextService, svc: EditionService): () => Promise<void> {
  return async () => {
    await conventionContext.load();
    await svc.load();
  };
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
      useFactory: initEdition,
      deps: [ConventionContextService, EditionService],
      multi: true,
    },
  ],
};
