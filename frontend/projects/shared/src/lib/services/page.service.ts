import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { ENVIRONMENT } from '../environment/environment.token';
import { PageDto, PageSummaryDto, PublicPageDto, SavePageRequest } from '../models/content.models';

@Injectable({ providedIn: 'root' })
export class PageService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  listPages() {
    return this.http.get<PageSummaryDto[]>(`${this.env.apiBaseUrl}/api/pages`);
  }

  getPage(pageId: string) {
    return this.http.get<PageDto>(`${this.env.apiBaseUrl}/api/pages/${pageId}`);
  }

  getPublicPage(slug: string) {
    return this.http.get<PublicPageDto>(`${this.env.apiBaseUrl}/api/pages/${slug}`);
  }

  createPage(request: SavePageRequest) {
    return this.http.post<{ id: string }>(`${this.env.apiBaseUrl}/api/pages`, request);
  }

  updatePage(pageId: string, request: SavePageRequest) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/api/pages/${pageId}`, request);
  }

  publishPage(pageId: string) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/api/pages/${pageId}/publish`, {});
  }

  unpublishPage(pageId: string) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/api/pages/${pageId}/unpublish`, {});
  }

  deletePage(pageId: string) {
    return this.http.delete<void>(`${this.env.apiBaseUrl}/api/pages/${pageId}`);
  }
}
