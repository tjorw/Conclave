import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService, MyVisitorRegistrationDto, RegistrationService, TICKET_PAYMENT_STATUS_LABEL, VisitorTicketTypeDto } from 'shared';
import { catchError, of } from 'rxjs';
import { EditionService } from '../../../services/edition.service';

@Component({
  selector: 'app-my-ticket',
  standalone: true,
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './my-ticket.component.html',
  styleUrl: './my-ticket.component.scss',
})
export class MyTicketComponent implements OnInit {
  private readonly editionSvc = inject(EditionService);
  private readonly authSvc = inject(AuthService);
  private readonly regSvc = inject(RegistrationService);
  private readonly fb = inject(FormBuilder);

  readonly loadingRegistration = signal(true);
  readonly loadingTicketTypes = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly registration = signal<MyVisitorRegistrationDto | null>(null);
  readonly ticketTypes = signal<VisitorTicketTypeDto[]>([]);
  readonly visitorRegistrationOpen = computed(
    () => this.editionSvc.edition()?.visitorRegistrationOpen ?? false
  );

  readonly registrationForm = this.fb.group({
    ticketTypeId: this.fb.control('', { validators: [Validators.required], nonNullable: true }),
    termsAccepted: this.fb.control(false, { validators: [Validators.requiredTrue], nonNullable: true }),
  });

  ngOnInit(): void {
    this.loadState();
  }

  submitRegistration(): void {
    if (!this.visitorRegistrationOpen()) {
      this.error.set('Besöksregistrering är inte öppen för denna upplaga.');
      return;
    }

    if (this.registrationForm.invalid || this.submitting()) {
      this.registrationForm.markAllAsTouched();
      return;
    }

    const editionId = this.editionSvc.editionId();
    const personId = this.authSvc.personId();

    if (!editionId || !personId) {
      this.error.set('Kunde inte identifiera upplaga eller användare. Logga in igen och försök på nytt.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.regSvc.submitVisitorRegistration(editionId, personId, this.registrationForm.controls.ticketTypeId.value)
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.loadState();
        },
        error: (err: HttpErrorResponse) => {
          const detail =
            err.error?.detail ??
            err.error?.title ??
            err.error?.message ??
            'Kunde inte boka biljett just nu. Försök igen.';
          this.error.set(detail);
          this.submitting.set(false);
        },
      });
  }

  paymentStatusLabel(status: string): string {
    return TICKET_PAYMENT_STATUS_LABEL[status] ?? status;
  }

  referenceNumber(id: string): string {
    return id.split('-')[0].toUpperCase();
  }

  private loadState(): void {
    const editionId = this.editionSvc.editionId();
    if (!editionId) {
      this.error.set('Ingen aktiv upplaga hittades.');
      this.loadingRegistration.set(false);
      this.loadingTicketTypes.set(false);
      return;
    }

    this.loadingRegistration.set(true);
    this.loadingTicketTypes.set(true);
    this.error.set(null);

    this.regSvc.getMyVisitorRegistration(editionId)
      .pipe(catchError(() => of(null)))
      .subscribe({
        next: registration => {
          this.registration.set(registration);
          this.loadingRegistration.set(false);
        },
        error: () => {
          this.error.set('Kunde inte läsa biljettinformation just nu.');
          this.loadingRegistration.set(false);
        },
      });

    this.regSvc.getAvailableTicketTypes(editionId)
      .pipe(catchError(() => of([])))
      .subscribe({
        next: ticketTypes => {
          if (!this.visitorRegistrationOpen()) {
            this.ticketTypes.set([]);
            this.loadingTicketTypes.set(false);
            return;
          }

          this.ticketTypes.set(ticketTypes);
          if (!this.registration() && ticketTypes.length > 0) {
            this.registrationForm.patchValue({ ticketTypeId: ticketTypes[0].id });
          }
          this.loadingTicketTypes.set(false);
        },
        error: () => {
          this.error.set('Kunde inte läsa biljettinformation just nu.');
          this.loadingTicketTypes.set(false);
        },
      });
  }
}
