import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { combineLatest, map } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ConventionService, EditionDto, MarkdownEditorComponent, PersonDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../../labels/pages.labels';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-category-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MarkdownEditorComponent,
  ],
  templateUrl: './category-detail.component.html',
  styleUrl: './category-detail.component.scss',
})
export class CategoryDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(ConventionService);
  private readonly dialog = inject(MatDialog);

  readonly edition = signal<EditionDto | null>(null);
  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly PAGE = EDITION_DETAIL;

  private editionId = '';
  private categoryId = '';

  readonly isNew = computed(() => this.categoryId === 'new');

  readonly form = this.fb.group({
    name: ['', Validators.required],
    organizerInstructions: [''],
    publicDescription: [''],
    responsibleId: ['', Validators.required],
  });

  ngOnInit(): void {
    combineLatest([
      this.route.paramMap.pipe(map((p) => p.get('id')!)),
      this.route.paramMap.pipe(map((p) => p.get('categoryId')!)),
    ]).subscribe(([editionId, categoryId]) => {
      this.editionId = editionId;
      this.categoryId = categoryId;
      this.loadData();
    });
  }

  private loadData(): void {
    this.loading.set(true);
    this.svc.getEdition(this.editionId).subscribe({
      next: (e) => {
        this.edition.set(e);
        if (!this.isNew()) {
          const category = e.categories.find((c) => c.id === this.categoryId);
          if (category) {
            this.form.setValue({
              name: category.name,
              organizerInstructions: category.organizerInstructions ?? '',
              publicDescription: category.publicDescription ?? '',
              responsibleId: category.responsibleId,
            });
          } else {
            this.error.set('Kategorin hittades inte.');
          }
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set(ERROR.fetchEdition);
        this.loading.set(false);
      },
    });
    this.svc
      .listPersons()
      .subscribe({ next: (p) => this.persons.set(p.filter((x) => x.isActive)) });
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    const payload = {
      name: v.name!,
      organizerInstructions: v.organizerInstructions || null,
      publicDescription: v.publicDescription || null,
      responsibleId: v.responsibleId!,
    };
    this.saving.set(true);
    const onError = (err: unknown, label: string) => {
      this.error.set(toContextErrorMessage(err, label));
      this.saving.set(false);
    };
    if (this.isNew()) {
      this.svc.createCategory(this.editionId, payload).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => onError(err, ERROR.createCategory),
      });
    } else {
      this.svc.updateCategory(this.editionId, this.categoryId, payload).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => onError(err, ERROR.updateCategory),
      });
    }
  }

  delete(): void {
    const category = this.edition()?.categories.find((c) => c.id === this.categoryId);
    if (!category) return;

    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: {
          title: this.PAGE.deleteCategoryTitle,
          message: this.PAGE.deleteCategoryMessage(category.name),
        },
        width: '400px',
      })
      .afterClosed()
      .pipe(map((r) => r === true))
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.saving.set(true);
        this.svc.removeCategory(this.editionId, this.categoryId).subscribe({
          next: () => this.navigateBack(),
          error: (err: unknown) => {
            this.error.set(toContextErrorMessage(err, ERROR.deleteCategory));
            this.saving.set(false);
          },
        });
      });
  }

  navigateBack(): void {
    void this.router.navigate(['/editions', this.editionId, 'categories']);
  }
}
