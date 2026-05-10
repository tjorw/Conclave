import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MarkdownComponent } from 'ngx-markdown';
import { catchError, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { PageService, PublicPageDto } from 'shared';
import { LocaleService } from '../../services/locale.service';
import { LabelsService } from '../../services/labels.service';

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
        <a routerLink="/">{{ labels().publicPageBackHome }}</a>
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
  private readonly localeSvc = inject(LocaleService);
  readonly labels = inject(LabelsService).labels;

  readonly page = signal<PublicPageDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.route.paramMap.pipe(
      map(params => params.get('slug') ?? ''),
      distinctUntilChanged(),
      tap(() => {
        this.loading.set(true);
        this.error.set(null);
        this.page.set(null);
      }),
      switchMap(slug => this.pageSvc.getPublicPage(slug, this.localeSvc.locale()).pipe(
        map(page => ({ page, error: null as string | null })),
        catchError(() => of({ page: null, error: this.labels().publicPageNotFound })),
      )),
    ).subscribe(result => {
      this.page.set(result.page);
      this.error.set(result.error);
      this.loading.set(false);
    });
  }
}
