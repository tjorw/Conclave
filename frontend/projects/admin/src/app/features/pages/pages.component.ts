import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PageService, PageSummaryDto } from 'shared';

@Component({
  selector: 'app-pages',
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <div class="page-header">
      <div>
        <h1 class="page-title">Informationssidor</h1>
        <p class="page-meta">Redaktionella markdown-sidor för den publika webbplatsen.</p>
      </div>
      <a mat-flat-button color="primary" routerLink="/pages/new">
        <mat-icon>add</mat-icon>
        Ny sida
      </a>
    </div>

    @if (loading()) {
      <div class="spinner-center"><mat-spinner diameter="42" /></div>
    } @else if (error()) {
      <div class="error-banner">{{ error() }}</div>
    } @else if (pages().length === 0) {
      <p class="empty-state">Inga informationssidor skapade ännu.</p>
    } @else {
      <table class="data-table">
        <thead>
          <tr>
            <th>Titel</th>
            <th>Slug</th>
            <th>Scope</th>
            <th>Status</th>
            <th>Uppdaterad</th>
          </tr>
        </thead>
        <tbody>
          @for (page of pages(); track page.id) {
            <tr class="clickable-row" [routerLink]="['/pages', page.id]">
              <td>{{ page.title }}</td>
              <td>{{ page.slug }}</td>
              <td>{{ page.editionId ? 'Upplaga' : 'Konvention' }}</td>
              <td>
                <span class="chip" [class.chip-green]="page.isPublished" [class.chip-grey]="!page.isPublished">
                  {{ page.isPublished ? 'Publicerad' : 'Utkast' }}
                </span>
              </td>
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

  readonly pages = signal<PageSummaryDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.pageSvc.listPages().subscribe({
      next: pages => { this.pages.set(pages); this.loading.set(false); },
      error: () => { this.error.set('Kunde inte hämta informationssidor.'); this.loading.set(false); },
    });
  }
}
