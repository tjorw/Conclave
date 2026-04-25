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
import { ConventionService, EditionDto, PersonDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../../labels/pages.labels';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-edition-staff-area-detail',
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
  ],
  templateUrl: './edition-staff-area-detail.component.html',
  styleUrl: './edition-staff-area-detail.component.scss',
})
export class EditionStaffAreaDetailComponent implements OnInit {
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
  private areaId = '';

  readonly isNew = computed(() => this.areaId === 'new');

  readonly form = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    responsibleId: ['', Validators.required],
  });

  ngOnInit(): void {
    combineLatest([
      this.route.paramMap.pipe(map((p) => p.get('id')!)),
      this.route.paramMap.pipe(map((p) => p.get('areaId')!)),
    ]).subscribe(([editionId, areaId]) => {
      this.editionId = editionId;
      this.areaId = areaId;
      this.loadData();
    });
  }

  private loadData(): void {
    this.loading.set(true);
    this.svc.getEdition(this.editionId).subscribe({
      next: (e) => {
        this.edition.set(e);
        if (!this.isNew()) {
          const area = e.staffAreas.find((a) => a.id === this.areaId);
          if (area) {
            this.form.setValue({
              name: area.name,
              description: area.description ?? '',
              responsibleId: area.responsibleId,
            });
          } else {
            this.error.set('Funktionsområdet hittades inte.');
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
      description: v.description || null,
      responsibleId: v.responsibleId!,
    };
    this.saving.set(true);
    const onError = (err: unknown, label: string) => {
      this.error.set(toContextErrorMessage(err, label));
      this.saving.set(false);
    };
    if (this.isNew()) {
      this.svc.createStaffArea(this.editionId, payload).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => onError(err, ERROR.createStaffArea),
      });
    } else {
      this.svc.updateStaffArea(this.editionId, this.areaId, payload).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => onError(err, ERROR.updateStaffArea),
      });
    }
  }

  delete(): void {
    const area = this.edition()?.staffAreas.find((a) => a.id === this.areaId);
    if (!area) return;

    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: {
          title: this.PAGE.deleteStaffAreaTitle,
          message: this.PAGE.deleteStaffAreaMessage(area.name),
        },
        width: '400px',
      })
      .afterClosed()
      .pipe(map((r) => r === true))
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.saving.set(true);
        this.svc.removeStaffArea(this.editionId, this.areaId).subscribe({
          next: () => this.navigateBack(),
          error: (err: unknown) => {
            this.error.set(toContextErrorMessage(err, ERROR.deleteStaffArea));
            this.saving.set(false);
          },
        });
      });
  }

  navigateBack(): void {
    void this.router.navigate(['/editions', this.editionId, 'staff-areas']);
  }
}
