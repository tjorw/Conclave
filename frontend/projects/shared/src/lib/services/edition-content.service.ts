import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { ENVIRONMENT } from '../environment/environment.token';
import { EditionContentDto } from '../models/content.models';

@Injectable({ providedIn: 'root' })
export class EditionContentService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  getContent(editionId: string) {
    return this.http.get<EditionContentDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/content`
    );
  }

  setContent(editionId: string, items: EditionContentDto[]) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/editions/${editionId}/content`,
      { items }
    );
  }
}
