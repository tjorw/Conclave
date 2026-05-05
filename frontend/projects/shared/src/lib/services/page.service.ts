import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { ENVIRONMENT } from '../environment/environment.token';
import { PageDto, PageSummaryDto, PublicPageDto, PublicPageMenuItemDto, SavePageRequest, UpdatePageMenuOrderRequest } from '../models/content.models';

@Injectable({ providedIn: 'root' })
export class PageService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  listPages() {
    return this.http.get<PageSummaryDto[]>(`${this.env.apiBaseUrl}/api/pages`);
  }

  listConventionPages() {
    return this.listPages();
  }

  listEditionPages(editionId: string) {
    const params = new HttpParams().set('editionId', editionId);
    return this.http.get<PageSummaryDto[]>(`${this.env.apiBaseUrl}/api/pages`, { params });
  }

  getPage(pageId: string) {
    return this.http.get<PageDto>(`${this.env.apiBaseUrl}/api/pages/${pageId}`);
  }

  getPublicPage(slug: string) {
    return this.http.get<PublicPageDto>(`${this.env.apiBaseUrl}/api/pages/${slug}`);
  }

  listPublicMenuPages() {
    return this.http.get<PublicPageMenuItemDto[]>(`${this.env.apiBaseUrl}/api/pages/menu`);
  }

  createPage(request: SavePageRequest) {
    return this.http.post<{ id: string }>(`${this.env.apiBaseUrl}/api/pages`, request);
  }

  createConventionPage(request: Omit<SavePageRequest, 'editionId'>) {
    return this.createPage({ ...request, editionId: null });
  }

  createEditionPage(editionId: string, request: Omit<SavePageRequest, 'editionId'>) {
    return this.createPage({ ...request, editionId });
  }

  updatePage(pageId: string, request: SavePageRequest) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/api/pages/${pageId}`, request);
  }

  updatePageMenuOrder(pageId: string, request: UpdatePageMenuOrderRequest) {
    return this.http.patch<void>(`${this.env.apiBaseUrl}/api/pages/${pageId}/menu-order`, request);
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
