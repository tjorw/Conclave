import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ConventionDto } from '../models/convention.models';
import { ENVIRONMENT } from '../environment/environment.token';

@Injectable({ providedIn: 'root' })
export class ConventionContextService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  private readonly _convention = signal<ConventionDto | null>(null);
  private loadPromise: Promise<void> | null = null;

  readonly convention = computed(() => this._convention());
  readonly conventionId = computed(() => this._convention()?.id ?? this.env.conventionId ?? null);

  async load(): Promise<void> {
    if (this._convention()) {
      return;
    }

    if (this.loadPromise) {
      return this.loadPromise;
    }

    this.loadPromise = firstValueFrom(
      this.http.get<ConventionDto>(`${this.env.apiBaseUrl}/convention`)
    )
      .then(convention => {
        this._convention.set(convention);
      })
      .catch(() => {
        // Allow fallback to the environment value if the API is not ready yet.
      })
      .finally(() => {
        this.loadPromise = null;
      });

    return this.loadPromise;
  }

  requireConventionId(): string {
    const conventionId = this.conventionId();
    if (!conventionId) {
      throw new Error('Convention ID is not available. Load ConventionContextService before using convention-scoped services.');
    }

    return conventionId;
  }
}
