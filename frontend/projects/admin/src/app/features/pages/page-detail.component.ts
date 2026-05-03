import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MarkdownEditorComponent, PageDto, PageService, SavePageRequest } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';

@Component({
  selector: 'app-page-detail',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MarkdownEditorComponent,
  ],
  template: `
    <div class="page-header">
      <a routerLink="/pages" class="back-link">
        <mat-icon>arrow_back</mat-icon>
        Informationssidor
      </a>
      <div class="title-row">
        <h1 class="page-title">{{ isNew ? 'Ny informationssida' : (page()?.title ?? 'Informationssida') }}</h1>
        @if (page(); as p) {
          <span class="chip" [class.chip-green]="p.isPublished" [class.chip-grey]="!p.isPublished">
            {{ p.isPublished ? 'Publicerad' : 'Utkast' }}
          </span>
        }
      </div>
    </div>

    @if (loading()) {
      <div class="spinner-center"><mat-spinner diameter="42" /></div>
    } @else {
      @if (error()) {
        <div class="error-banner">{{ error() }}</div>
      }

      <form [formGroup]="form" class="edit-form">
        <div class="form-row">
          <mat-form-field appearance="outline" class="flex-grow">
            <mat-label>Titel</mat-label>
            <input matInput formControlName="title" />
          </mat-form-field>
          <mat-form-field appearance="outline" class="flex-grow">
            <mat-label>Slug</mat-label>
            <input matInput formControlName="slug" />
            @if (form.controls.slug.hasError('duplicateSlug')) {
              <mat-error>Sluggen finns redan. Välj en annan slug.</mat-error>
            }
          </mat-form-field>
          <mat-form-field appearance="outline" class="flex-grow">
            <mat-label>Scope</mat-label>
            <mat-select formControlName="editionId">
              <mat-option [value]="null">Konvention</mat-option>
              @for (edition of editionContext.editions(); track edition.id) {
                <mat-option [value]="edition.id">{{ edition.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </div>

        <lib-markdown-editor formControlName="content" label="Innehåll" [rows]="16" [maxLength]="20000" />
      </form>

      <div class="action-bar">
        <button mat-flat-button color="primary" (click)="save()" [disabled]="form.invalid || saving()">
          @if (saving()) { <mat-spinner diameter="18" /> } @else { Spara }
        </button>
        @if (page(); as p) {
          @if (!p.isPublished) {
            <button mat-stroked-button (click)="publish()" [disabled]="saving()">Publicera</button>
          } @else {
            <button mat-stroked-button (click)="unpublish()" [disabled]="saving()">Avpublicera</button>
          }
          <button mat-stroked-button color="warn" (click)="delete()" [disabled]="saving()">Ta bort</button>
        }
      </div>
    }
  `,
})
export class PageDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly pageSvc = inject(PageService);
  readonly editionContext = inject(EditionContextService);

  readonly page = signal<PageDto | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly pageId = this.route.snapshot.paramMap.get('pageId');
  readonly isNew = this.pageId === 'new';

  readonly form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(300)]],
    slug: ['', [Validators.required, Validators.maxLength(200), Validators.pattern(/^[a-z0-9-]+$/)]],
    editionId: [null as string | null],
    content: ['', Validators.maxLength(20000)],
  });

  constructor() {
    this.form.controls.slug.valueChanges.subscribe(() => {
      if (this.form.controls.slug.hasError('duplicateSlug')) {
        this.form.controls.slug.setErrors(removeControlError(this.form.controls.slug.errors, 'duplicateSlug'));
      }
    });

    if (this.isNew || !this.pageId) {
      this.loading.set(false);
      return;
    }

    this.pageSvc.getPage(this.pageId).subscribe({
      next: page => {
        this.page.set(page);
        this.form.setValue({
          title: page.title,
          slug: page.slug,
          editionId: page.editionId,
          content: page.content,
        });
        this.loading.set(false);
      },
      error: () => { this.error.set('Kunde inte hämta informationssidan.'); this.loading.set(false); },
    });
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);
    this.form.controls.slug.setErrors(removeControlError(this.form.controls.slug.errors, 'duplicateSlug'));
    const request = this.toRequest();

    if (this.isNew || !this.pageId) {
      this.pageSvc.createPage(request).subscribe({
        next: () => {
          this.saving.set(false);
          this.router.navigate(['/pages']);
        },
        error: error => this.handleSaveError(error),
      });
      return;
    }

    this.pageSvc.updatePage(this.pageId, request).subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/pages']);
      },
      error: error => this.handleSaveError(error),
    });
  }

  publish(): void {
    if (!this.pageId || this.saving()) return;
    this.saving.set(true);
    this.pageSvc.publishPage(this.pageId).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: () => { this.saving.set(false); this.error.set('Kunde inte publicera informationssidan.'); },
    });
  }

  unpublish(): void {
    if (!this.pageId || this.saving()) return;
    this.saving.set(true);
    this.pageSvc.unpublishPage(this.pageId).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: () => { this.saving.set(false); this.error.set('Kunde inte avpublicera informationssidan.'); },
    });
  }

  delete(): void {
    if (!this.pageId || this.saving()) return;
    this.saving.set(true);
    this.pageSvc.deletePage(this.pageId).subscribe({
      next: () => this.router.navigate(['/pages']),
      error: () => { this.saving.set(false); this.error.set('Kunde inte ta bort informationssidan.'); },
    });
  }

  private reload(): void {
    if (!this.pageId) return;
    this.pageSvc.getPage(this.pageId).subscribe({
      next: page => this.page.set(page),
    });
  }

  private toRequest(): SavePageRequest {
    const value = this.form.getRawValue();
    return {
      title: value.title!,
      slug: value.slug!,
      editionId: value.editionId ?? null,
      content: value.content ?? '',
    };
  }

  private handleSaveError(error: unknown): void {
    this.saving.set(false);

    if (isPageSlugAlreadyExists(error)) {
      const slug = this.form.controls.slug;
      slug.setErrors({ ...slug.errors, duplicateSlug: true });
      slug.markAsTouched();
      this.error.set('Sluggen finns redan. Välj en annan slug.');
      return;
    }

    this.error.set('Kunde inte spara informationssidan.');
  }
}

function isPageSlugAlreadyExists(error: unknown): boolean {
  return error instanceof HttpErrorResponse
    && error.status === 422
    && error.error?.errorCode === 'page_slug_already_exists';
}

function removeControlError(
  errors: Record<string, unknown> | null,
  key: string,
): Record<string, unknown> | null {
  if (!errors || !(key in errors)) return errors;
  const next = { ...errors };
  delete next[key];
  return Object.keys(next).length > 0 ? next : null;
}
