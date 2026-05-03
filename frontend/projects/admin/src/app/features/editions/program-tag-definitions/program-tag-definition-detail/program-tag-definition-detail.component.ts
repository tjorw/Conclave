import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { combineLatest, map } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, createAsyncState, EditionDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../../labels/pages.labels';
import { ConfirmDialogService } from '../../../../shared/confirm-dialog/confirm-dialog.service';

@Component({
  selector: 'app-program-tag-definition-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './program-tag-definition-detail.component.html',
  styleUrl: './program-tag-definition-detail.component.scss',
})
export class ProgramTagDefinitionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(ConventionService);
  private readonly confirmSvc = inject(ConfirmDialogService);

  readonly edition = signal<EditionDto | null>(null);
  protected readonly state = createAsyncState(true);
  readonly PAGE = EDITION_DETAIL;

  private editionId = '';
  private tagName = '';

  readonly isNew = computed(() => this.tagName === 'new');

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(64)]],
  });

  ngOnInit(): void {
    combineLatest([
      this.route.paramMap.pipe(map((p) => p.get('id')!)),
      this.route.paramMap.pipe(map((p) => p.get('tagName')!)),
    ]).subscribe(([editionId, tagName]) => {
      this.editionId = editionId;
      this.tagName = decodeURIComponent(tagName);
      this.loadData();
    });
  }

  private loadData(): void {
    this.state.loading.set(true);
    this.svc.getEdition(this.editionId).subscribe({
      next: (e) => {
        this.edition.set(e);
        if (!this.isNew()) {
          const tag = e.programTagDefinitions.find((x) => x.name === this.tagName);
          if (tag) {
            this.form.setValue({ name: tag.name });
          } else {
            this.state.error.set('Taggen hittades inte.');
          }
        }
        this.state.loading.set(false);
      },
      error: () => {
        this.state.error.set(ERROR.fetchEdition);
        this.state.loading.set(false);
      },
    });
  }

  save(): void {
    if (this.form.invalid) return;
    const name = this.form.value.name!.trim();
    this.state.saving.set(true);

    const onError = (err: unknown, label: string) => {
      this.state.error.set(toContextErrorMessage(err, label));
      this.state.saving.set(false);
    };

    if (this.isNew()) {
      this.svc.createProgramTagDefinition(this.editionId, { name }).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => onError(err, ERROR.createProgramTagDefinition),
      });
    } else {
      this.svc.updateProgramTagDefinition(this.editionId, { currentName: this.tagName, newName: name }).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => onError(err, ERROR.updateProgramTagDefinition),
      });
    }
  }

  delete(): void {
    if (this.isNew()) return;

    this.confirmSvc.confirm({
      title: this.PAGE.deleteProgramTagDefinitionTitle,
      message: this.PAGE.deleteProgramTagDefinitionMessage(this.tagName),
    }).subscribe((confirmed) => {
      if (!confirmed) return;
      this.state.saving.set(true);
      this.svc.removeProgramTagDefinition(this.editionId, this.tagName).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => {
          this.state.error.set(toContextErrorMessage(err, ERROR.deleteProgramTagDefinition));
          this.state.saving.set(false);
        },
      });
    });
  }

  navigateBack(): void {
    void this.router.navigate(['/editions', this.editionId, 'tags']);
  }
}
