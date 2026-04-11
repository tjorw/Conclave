import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from '../environment/environment.token';
import { ConventionDto, EditionDto, EditionSummaryDto } from '../models/convention.models';

@Injectable({ providedIn: 'root' })
export class ConventionService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  getConvention() {
    return this.http.get<ConventionDto>(
      `${this.env.apiBaseUrl}/conventions/${this.env.conventionId}`
    );
  }

  listEditions() {
    return this.http.get<EditionSummaryDto[]>(
      `${this.env.apiBaseUrl}/conventions/${this.env.conventionId}/editions`
    );
  }

  getEdition(editionId: string) {
    return this.http.get<EditionDto>(
      `${this.env.apiBaseUrl}/editions/${editionId}`
    );
  }
}
