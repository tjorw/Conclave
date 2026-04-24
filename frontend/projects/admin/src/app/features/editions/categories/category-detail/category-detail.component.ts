import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { combineLatest, map } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ConventionService, EditionDto, PersonDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../../labels/errors.labels';

@Component({
  selector: 'app-category-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './category-detail.component.html',
  styleUrl: './category-detail.component.scss',
})
export class CategoryDetailComponent implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);
  private readonly svc    = inject(ConventionService);

  readonly edition    = signal<EditionDto | null>(null);
  readonly persons    = signal<PersonDto[]>([]);
  readonly loading    = signal(true);
  readonly error      = signal<string | null>(null);
  readonly saving     = signal(false);

  private editionId  = '';
  private categoryId = '';

  readonly isNew = computed(() => this.categoryId === 'new');

  readonly form = this.fb.group({
    name:          ['', Validators.required],
    description:   [''],
    responsibleId: ['', Validators.required],
  });

  ngOnInit(): void {
    combineLatest([
      this.route.paramMap.pipe(map(p => p.get('id')!)),
      this.route.paramMap.pipe(map(p => p.get('categoryId')!)),
    ]).subscribe(([editionId, categoryId]) => {
      this.editionId  = editionId;
      this.categoryId = categoryId;
      this.loadData();
    });
  }

  private loadData(): void {
    this.loading.set(true);
    this.svc.getEdition(this.editionId).subscribe({
      next: e => {
        this.edition.set(e);
        if (!this.isNew()) {
          const category = e.categories.find(c => c.id === this.categoryId);
          if (category) {
            this.form.setValue({ name: category.name, description: category.description ?? '', responsibleId: category.responsibleId });
          } else {
            this.error.set('Kategorin hittades inte.');
          }
        }
        this.loading.set(false);
      },
      error: () => { this.error.set(ERROR.fetchEdition); this.loading.set(false); },
    });
    this.svc.listPersons().subscribe({ next: p => this.persons.set(p.filter(x => x.isActive)) });
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    const payload = { name: v.name!, description: v.description || null, responsibleId: v.responsibleId! };
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

  navigateBack(): void {
    void this.router.navigate(['/editions', this.editionId, 'categories']);
  }
}
