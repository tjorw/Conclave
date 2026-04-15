import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpErrorResponse } from '@angular/common/http';
import { CategoryDto, ConventionService, EventService, AuthService } from 'shared';
import { EditionService } from '../../../../services/edition.service';

@Component({
  selector: 'app-create-event',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './create-event.component.html',
  styleUrl: './create-event.component.scss',
})
export class CreateEventComponent implements OnInit {
  private readonly fb            = inject(FormBuilder);
  private readonly editionSvc    = inject(EditionService);
  private readonly conventionSvc = inject(ConventionService);
  private readonly eventSvc      = inject(EventService);
  private readonly authSvc       = inject(AuthService);
  private readonly router        = inject(Router);

  readonly loading    = signal(true);
  readonly saving     = signal(false);
  readonly error      = signal<string | null>(null);
  readonly categories = signal<CategoryDto[]>([]);

  readonly form = this.fb.group({
    categoryId: ['', Validators.required],
  });

  ngOnInit(): void {
    const editionId = this.editionSvc.editionId();
    if (!editionId) {
      this.loading.set(false);
      return;
    }
    this.conventionSvc.getEdition(editionId).subscribe({
      next: edition => {
        this.categories.set(edition.categories);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  create(): void {
    if (this.form.invalid || this.saving()) return;
    const editionId  = this.editionSvc.editionId();
    const personId   = this.authSvc.personId();
    if (!editionId || !personId) return;

    this.saving.set(true);
    this.error.set(null);

    const { categoryId } = this.form.getRawValue();

    this.eventSvc.createEvent(editionId, categoryId!, personId).subscribe({
      next: ({ id }) => this.router.navigateByUrl(`/my-pages/events/${id}`),
      error: (err: HttpErrorResponse) => {
        const detail = err.error?.detail ?? err.error?.title ?? 'Kunde inte skapa arrangemanget.';
        this.error.set(detail);
        this.saving.set(false);
      },
    });
  }
}
