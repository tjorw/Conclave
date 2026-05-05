import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PageService, PageSummaryDto } from 'shared';
import { PAGE_ADMIN } from '../../labels/pages.labels';

@Component({
  selector: 'app-pages',
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <div class="page-header">
      <div>
        <h1 class="page-title">{{ listTitle }}</h1>
        <p class="page-meta">{{ listMeta }}</p>
      </div>
      <a mat-flat-button color="primary" [routerLink]="newLink">
        <mat-icon>add</mat-icon>
        {{ LABELS.newPageAction }}
      </a>
    </div>

    @if (loading()) {
      <div class="spinner-center"><mat-spinner diameter="42" /></div>
    } @else if (error()) {
      <div class="error-banner">{{ error() }}</div>
    } @else if (pages().length === 0) {
      <p class="empty-state">{{ emptyText }}</p>
    } @else {
      <table class="data-table">
        <thead>
          <tr>
            <th>{{ LABELS.titleColumn }}</th>
            <th>{{ LABELS.slugColumn }}</th>
            <th>{{ LABELS.statusColumn }}</th>
            <th>{{ LABELS.publicMenuColumn }}</th>
            <th>{{ LABELS.updatedColumn }}</th>
          </tr>
        </thead>
        <tbody>
          @for (page of pages(); track page.id) {
            <tr class="clickable-row" [routerLink]="detailLink(page)">
              <td>{{ page.title }}</td>
              <td>{{ page.slug }}</td>
              <td>
                <span class="chip" [class.chip-green]="page.isPublished" [class.chip-grey]="!page.isPublished">
                  {{ page.isPublished ? LABELS.publishedStatus : LABELS.draftStatus }}
                </span>
              </td>
              <td>{{ page.showInPublicMenu ? LABELS.yes : LABELS.no }}</td>
              <td>{{ page.updatedAt | date:'yyyy-MM-dd HH:mm' }}</td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
})
export class PagesComponent {
  private readonly pageSvc = inject(PageService);
  private readonly route = inject(ActivatedRoute);

  readonly LABELS = PAGE_ADMIN;
  readonly editionId = this.route.snapshot.paramMap.get('id');
  readonly isEditionScope = this.editionId !== null;
  readonly listTitle = this.isEditionScope ? this.LABELS.editionListTitle : this.LABELS.conventionListTitle;
  readonly listMeta = this.isEditionScope ? this.LABELS.editionListMeta : this.LABELS.conventionListMeta;
  readonly emptyText = this.isEditionScope ? this.LABELS.editionEmpty : this.LABELS.conventionEmpty;
  readonly newLink = this.isEditionScope && this.editionId
    ? ['/editions', this.editionId, 'pages', 'new']
    : ['/pages', 'new'];

  readonly pages = signal<PageSummaryDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    const request = this.isEditionScope && this.editionId
      ? this.pageSvc.listEditionPages(this.editionId)
      : this.pageSvc.listConventionPages();

    request.subscribe({
      next: pages => { this.pages.set(pages); this.loading.set(false); },
      error: () => { this.error.set(this.LABELS.loadListError); this.loading.set(false); },
    });
  }

  detailLink(page: PageSummaryDto): string[] {
    return this.isEditionScope && this.editionId
      ? ['/editions', this.editionId, 'pages', page.id]
      : ['/pages', page.id];
  }
}
