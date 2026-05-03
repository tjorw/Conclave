import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MarkdownComponent } from 'ngx-markdown';
import { PageService, PublicPageDto } from 'shared';

@Component({
  selector: 'app-public-page',
  standalone: true,
  imports: [RouterLink, MatProgressSpinnerModule, MarkdownComponent],
  template: `
    <div class="page-container">
      @if (loading()) {
        <div class="spinner-center"><mat-spinner diameter="42" /></div>
      } @else if (error()) {
        <div class="error-banner">{{ error() }}</div>
        <a routerLink="/">Till startsidan</a>
      } @else if (page(); as p) {
        <article class="content-page">
          <h1>{{ p.title }}</h1>
          <markdown [data]="p.content" />
        </article>
      }
    </div>
  `,
  styles: [`
    .content-page {
      max-width: 840px;
      margin: 0 auto;
      line-height: 1.7;
    }
  `],
})
export class PublicPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly pageSvc = inject(PageService);

  readonly page = signal<PublicPageDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    const slug = this.route.snapshot.paramMap.get('slug') ?? '';
    this.pageSvc.getPublicPage(slug).subscribe({
      next: page => { this.page.set(page); this.loading.set(false); },
      error: () => { this.error.set('Sidan hittades inte.'); this.loading.set(false); },
    });
  }
}
