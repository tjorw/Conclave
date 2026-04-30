import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { formatSekPrice, MyVisitorRegistrationDto, RegistrationService, TICKET_PAYMENT_STATUS_LABEL, VisitorTicketTypeDto, toErrorMessage } from 'shared';
import { MarkdownComponent } from 'ngx-markdown';
import { EditionService } from '../../../services/edition.service';

@Component({
  selector: 'app-my-ticket',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MarkdownComponent,
  ],
  templateUrl: './my-ticket.component.html',
  styleUrl: './my-ticket.component.scss',
})
export class MyTicketComponent implements OnInit {
  private readonly editionSvc = inject(EditionService);
  private readonly regSvc     = inject(RegistrationService);
  private readonly fb         = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly loadingRegistration = signal(true);
  readonly loadingTicketTypes = signal(true);
  readonly submitting = signal(false);
  readonly cancellingRegistrationId = signal<string | null>(null);
  readonly redeemingTicketId = signal<string | null>(null);
  readonly promotionCodeValues = signal<Record<string, string>>({});
  readonly redeemResultMessage = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly registrations = signal<MyVisitorRegistrationDto[]>([]);
  readonly ticketTypes = signal<VisitorTicketTypeDto[]>([]);
  readonly hasPaidRegistrations = computed(
    () => this.registrations().some(registration => registration.status === 'Confirmed')
  );
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

    if (!editionId) {
      this.error.set('Kunde inte identifiera upplaga. Ladda om sidan och försök igen.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.regSvc.submitVisitorRegistration(editionId, this.registrationForm.controls.ticketTypeId.value)
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.loadState();
        },
        error: err => {
          this.error.set(toErrorMessage(err, 'Kunde inte boka biljett just nu. Försök igen.'));
          this.submitting.set(false);
        },
      });
  }

  cancelRegistration(registration: MyVisitorRegistrationDto): void {
    if (!registration.id) {
      this.error.set('Kunde inte identifiera registreringen som ska avbokas.');
      return;
    }

    if (this.cancellingRegistrationId()) {
      return;
    }

    this.error.set(null);
    this.cancellingRegistrationId.set(registration.id);

    this.regSvc.cancelVisitorRegistration(registration.id)
      .subscribe({
        next: () => {
          this.cancellingRegistrationId.set(null);
          this.loadState();
        },
        error: err => {
          this.error.set(toErrorMessage(err, 'Kunde inte avboka biljetten just nu. Försök igen.'));
          this.cancellingRegistrationId.set(null);
        },
      });
  }

  canRedeemPromotionCode(registration: MyVisitorRegistrationDto): boolean {
    return registration.ticketTypeCategory === 'Visitor' && registration.status === 'PendingPayment' && !!registration.ticketId;
  }

  promotionCodeValue(ticketId: string): string {
    return this.promotionCodeValues()[ticketId] ?? '';
  }

  updatePromotionCodeValue(ticketId: string, value: string): void {
    this.promotionCodeValues.update(current => ({
      ...current,
      [ticketId]: value,
    }));
  }

  redeemPromotionCode(registration: MyVisitorRegistrationDto): void {
    if (!registration.ticketId) {
      this.error.set('Kunde inte identifiera biljetten för kampanjkodsinlösen.');
      return;
    }

    const code = this.promotionCodeValue(registration.ticketId).trim();
    if (!code) {
      this.error.set('Ange en kampanjkod innan du försöker lösa in.');
      return;
    }

    this.error.set(null);
    this.redeemResultMessage.set(null);
    this.redeemingTicketId.set(registration.ticketId);

    this.regSvc.redeemPromotionCode(registration.ticketId, code)
      .subscribe({
        next: result => {
          const discount = formatSekPrice(result.discountApplied);
          const finalPrice = formatSekPrice(result.finalPrice);

          this.redeemResultMessage.set(`Kampanjkod tillämpad. Rabatt: ${discount}. Nytt pris: ${finalPrice}.`);
          this.updatePromotionCodeValue(registration.ticketId, '');
          this.redeemingTicketId.set(null);
          this.loadState();
        },
        error: err => {
          this.error.set(toErrorMessage(err, 'Kunde inte lösa in kampanjkoden just nu. Försök igen.'));
          this.redeemingTicketId.set(null);
        },
      });
  }

  canCancelRegistration(registration: MyVisitorRegistrationDto): boolean {
    return registration.canCancel;
  }

  paymentStatusLabel(status: string): string {
    return TICKET_PAYMENT_STATUS_LABEL[status] ?? status;
  }

  statusChipClass(status: string): string {
    return status === 'Confirmed' || status === 'Paid' || status === 'Collected'
      ? 'status-chip green'
      : 'status-chip orange';
  }

  referenceNumber(id: string): string {
    return id.split('-')[0].toUpperCase();
  }

  ticketTypeLabel(ticketType: VisitorTicketTypeDto): string {
    const label = (ticketType.name ?? '').trim();
    return label.length > 0 ? label : 'Biljett';
  }

  ticketTypePriceLabel(ticketType: VisitorTicketTypeDto): string {
    const price = Number(ticketType.price);
    if (!Number.isFinite(price) || price < 0) return 'Pris saknas';
    return formatSekPrice(price);
  }

  registrationPriceLabel(registration: MyVisitorRegistrationDto): string {
    const price = Number(registration.ticketPrice);
    if (!Number.isFinite(price) || price < 0) return 'Pris saknas';
    return formatSekPrice(price);
  }

  ticketCategoryLabel(registration: MyVisitorRegistrationDto): string {
    switch (registration.ticketTypeCategory) {
      case 'Organiser': return 'Arrangörsbiljett';
      case 'Staff': return 'Funktionärsbiljett';
      default: return 'Besökarbiljett';
    }
  }

  validDaysLabel(registration: MyVisitorRegistrationDto): string | null {
    if (!registration.validDays || registration.validDays.length === 0) {
      return null;
    }

    return registration.validDays.join(', ');
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
    this.redeemResultMessage.set(null);

    this.regSvc.getMyVisitorRegistration(editionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: registrations => {
          const activeRegistrations = registrations.filter(registration => registration.status !== 'Cancelled' && registration.ticketStatus !== 'Revoked');

          this.registrations.set(activeRegistrations);
          this.loadingRegistration.set(false);
        },
        error: () => {
          this.error.set('Kunde inte läsa biljettinformation just nu.');
          this.loadingRegistration.set(false);
        },
      });

    this.regSvc.getAvailableTicketTypes(editionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ticketTypes => {
          if (!this.visitorRegistrationOpen()) {
            this.ticketTypes.set([]);
            this.loadingTicketTypes.set(false);
            return;
          }

          this.ticketTypes.set(ticketTypes);
          if (this.registrations().length === 0 && ticketTypes.length > 0) {
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
