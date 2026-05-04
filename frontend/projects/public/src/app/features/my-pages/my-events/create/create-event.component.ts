import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CategoryDto, ConventionService, EventService, AuthService, OrganiserTicketTypeDto, RegistrationService, toErrorMessage } from 'shared';
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
  private readonly regSvc        = inject(RegistrationService);
  private readonly authSvc       = inject(AuthService);
  private readonly router        = inject(Router);
  private readonly destroyRef    = inject(DestroyRef);

  readonly loading    = signal(true);
  readonly saving     = signal(false);
  readonly error      = signal<string | null>(null);
  readonly categories = signal<CategoryDto[]>([]);
  readonly organiserTicketTypes = signal<OrganiserTicketTypeDto[]>([]);

  readonly form = this.fb.group({
    categoryId: ['', Validators.required],
  });

  ngOnInit(): void {
    const editionId = this.editionSvc.editionId();
    if (!editionId) {
      this.loading.set(false);
      return;
    }
    this.conventionSvc.getEdition(editionId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: edition => {
        this.categories.set(edition.categories);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });

    this.regSvc.getOrganiserTicketTypes(editionId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ticketTypes => this.organiserTicketTypes.set(ticketTypes),
      error: () => this.organiserTicketTypes.set([]),
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

    this.eventSvc.createEvent(editionId, categoryId!, personId, []).subscribe({
      next: ({ id }) => this.router.navigateByUrl(`/my-pages/events/${id}`),
      error: err => {
        this.error.set(toErrorMessage(err, 'Kunde inte skapa arrangemanget.'));
        this.saving.set(false);
      },
    });
  }

  ticketPriceLabel(price: number): string {
    if (price === 0) return 'Kostnadsfri';

    return new Intl.NumberFormat('sv-SE', {
      style: 'currency',
      currency: 'SEK',
      maximumFractionDigits: 0,
    }).format(price / 100);
  }
}
