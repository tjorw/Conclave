import { Injectable, signal } from '@angular/core';

/**
 * Håller information om den aktiva upplagan för den publika appen.
 * Konventionsnamn och upplageår sätts via APP_INITIALIZER i kommande fas
 * när vi hämtar data från API:t. Upplage-ID sätts vid behov av funktioner
 * som anropar edition-specifika endpoints.
 */
@Injectable({ providedIn: 'root' })
export class EditionService {
  readonly conventionName = signal('Conclave');
  readonly editionYear    = signal('');
  readonly editionId      = signal<string | null>(null);
}
