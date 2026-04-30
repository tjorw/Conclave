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
import { ConventionService, EditionDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../../labels/pages.labels';
import { ConfirmDialogService } from '../../../../shared/confirm-dialog/confirm-dialog.service';

@Component({
  selector: 'app-venue-detail',
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
  templateUrl: './venue-detail.component.html',
  styleUrl: './venue-detail.component.scss',
})
export class VenueDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(ConventionService);
  private readonly confirmSvc = inject(ConfirmDialogService);

  readonly edition = signal<EditionDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly PAGE = EDITION_DETAIL;

  private editionId = '';
  private venueId = '';

  readonly isNew = computed(() => this.venueId === 'new');

  readonly form = this.fb.group({
    name: ['', Validators.required],
    building: ['', Validators.required],
    description: [''],
  });

  ngOnInit(): void {
    combineLatest([
      this.route.paramMap.pipe(map((p) => p.get('id')!)),
      this.route.paramMap.pipe(map((p) => p.get('venueId')!)),
    ]).subscribe(([editionId, venueId]) => {
      this.editionId = editionId;
      this.venueId = venueId;
      this.loadData();
    });
  }

  private loadData(): void {
    this.loading.set(true);
    this.svc.getEdition(this.editionId).subscribe({
      next: (e) => {
        this.edition.set(e);
        if (!this.isNew()) {
          const venue = e.venues.find((v) => v.id === this.venueId);
          if (venue) {
            this.form.setValue({
              name: venue.name,
              building: venue.building,
              description: venue.description ?? '',
            });
          } else {
            this.error.set('Lokalen hittades inte.');
          }
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set(ERROR.fetchEdition);
        this.loading.set(false);
      },
    });
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    const payload = { name: v.name!, building: v.building!, description: v.description || null };
    this.saving.set(true);
    const onError = (err: unknown, label: string) => {
      this.error.set(toContextErrorMessage(err, label));
      this.saving.set(false);
    };
    if (this.isNew()) {
      this.svc.createVenue(this.editionId, payload).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => onError(err, ERROR.createVenue),
      });
    } else {
      this.svc.updateVenue(this.editionId, this.venueId, payload).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => onError(err, ERROR.updateVenue),
      });
    }
  }

  delete(): void {
    const venue = this.edition()?.venues.find((v) => v.id === this.venueId);
    if (!venue) return;

    this.confirmSvc.confirm({
      title: this.PAGE.deleteVenueTitle,
      message: this.PAGE.deleteVenueMessage(venue.name),
    }).subscribe((confirmed) => {
        if (!confirmed) return;
        this.saving.set(true);
        this.svc.removeVenue(this.editionId, this.venueId).subscribe({
          next: () => this.navigateBack(),
          error: (err: unknown) => {
            this.error.set(toContextErrorMessage(err, ERROR.deleteVenue));
            this.saving.set(false);
          },
        });
      });
  }

  navigateBack(): void {
    void this.router.navigate(['/editions', this.editionId, 'venues']);
  }
}
