import { HttpInterceptorFn } from '@angular/common/http';

// Behålls för bakåtkompatibilitet med app.config – är nu en no-op.
// X-Convention-Id-headern togs bort när systemet gick från multi-tenant till deploy-per-konvention.
export const conventionInterceptor: HttpInterceptorFn = (req, next) => next(req);
