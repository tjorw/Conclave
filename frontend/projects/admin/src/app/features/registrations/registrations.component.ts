import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import {
  PromotionCodeAdminDto,
  PromotionCodeRedemptionHistoryDto,
  PromotionDiscountType,
  RegistrationService,
  TicketTypeAdminDto,
  VisitorRegistrationAdminDto,
  VISITOR_REGISTRATION_STATUS_LABEL,
  VISITOR_REGISTRATION_STATUS_CHIP,
  toContextErrorMessage,
} from 'shared';
import { createSortController, sortBy } from '../../shared/sort-utils';
import { HelpTooltipComponent } from '../../../help/components/help-tooltip/help-tooltip.component';

type RegistrationSortKey = 'person' | 'ticket' | 'status' | 'registered' | 'payment';
type PromotionSortKey = 'code' | 'description' | 'discount' | 'status' | 'redemptions' | 'validity' | 'tickets';
type PromotionHistorySortKey = 'person' | 'ticket' | 'discount' | 'finalPrice' | 'redeemed';
type RegistrationPage = 'visitors' | 'promotion-codes';

@Component({
  selector: 'app-registrations',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    HelpTooltipComponent,
  ],
  templateUrl: './registrations.component.html',
  styleUrl: './registrations.component.scss',
})
export class RegistrationsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(RegistrationService);
  private readonly route = inject(ActivatedRoute);
  readonly editionCtx = inject(EditionContextService);

  readonly page = toSignal(
    this.route.data.pipe(map(data => (data['page'] as RegistrationPage | undefined) ?? 'visitors')),
    { initialValue: 'visitors' as RegistrationPage }
  );
  readonly pageTitle = computed(() =>
    this.page() === 'visitors' ? 'Biljetter' : 'Kampanjkoder'
  );
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly showPromotionCodeForm = signal(false);

  readonly visitorRegistrations = signal<VisitorRegistrationAdminDto[]>([]);
  readonly promotionCodes = signal<PromotionCodeAdminDto[]>([]);
  readonly ticketTypes = signal<TicketTypeAdminDto[]>([]);
  readonly promotionHistory = signal<PromotionCodeRedemptionHistoryDto[]>([]);
  readonly selectedPromotionCodeId = signal<string | null>(null);
  readonly loadingHistoryFor = signal<string | null>(null);
  readonly registrationSort = createSortController<RegistrationSortKey>({ key: 'registered', direction: 'desc' });
  readonly promotionSort = createSortController<PromotionSortKey>({ key: 'code', direction: 'asc' });
  readonly promotionHistorySort = createSortController<PromotionHistorySortKey>({ key: 'redeemed', direction: 'desc' });

  readonly discountTypeOptions: { value: PromotionDiscountType; label: string }[] = [
    { value: 'Percentage', label: 'Procent' },
    { value: 'Fixed', label: 'Fast belopp' },
    { value: 'Free', label: 'Gratis' },
  ];

  readonly promotionCodeForm = this.fb.group({
    code: this.fb.control('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(64)] }),
    description: this.fb.control('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    discountType: this.fb.control<PromotionDiscountType>('Percentage', { nonNullable: true, validators: [Validators.required] }),
    discountValue: this.fb.control(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    maxRedemptions: this.fb.control<number | null>(null),
    validFrom: this.fb.control('', { nonNullable: true }),
    validUntil: this.fb.control('', { nonNullable: true }),
    allowedTicketTypeIds: this.fb.control<string[]>([], { nonNullable: true }),
  });

  readonly sortedVisitorRegistrations = computed(() =>
    sortBy(this.visitorRegistrations(), this.registrationSort.state(), {
      person: r => r.personName,
      ticket: r => r.ticketTypeName ?? '',
      status: r => this.statusLabel(r.status),
      registered: r => r.registeredAt,
      payment: r => r.paymentReference ?? '',
    })
  );

  readonly sortedPromotionCodes = computed(() =>
    sortBy(this.promotionCodes(), this.promotionSort.state(), {
      code: c => c.code,
      description: c => c.description,
      discount: c => this.promotionDiscountLabel(c),
      status: c => c.isActive,
      redemptions: c => c.redemptionCount,
      validity: c => c.validFrom ?? c.validUntil ?? '',
      tickets: c => this.allowedTicketTypeLabel(c.allowedTicketTypeIds),
    })
  );

  readonly sortedPromotionHistory = computed(() =>
    sortBy(this.promotionHistory(), this.promotionHistorySort.state(), {
      person: h => this.personNameFromRegistration(h.personId),
      ticket: h => h.ticketId,
      discount: h => h.discountApplied,
      finalPrice: h => h.finalPrice,
      redeemed: h => h.redeemedAt,
    })
  );

  constructor() {
    effect(() => {
      const edition = this.editionCtx.activeEdition();
      if (edition) this.load(edition.id);
    });
  }

  private load(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.selectedPromotionCodeId.set(null);
    this.promotionHistory.set([]);
    this.loadingHistoryFor.set(null);

    let remaining = 3;
    const complete = () => {
      remaining -= 1;
      if (remaining === 0) {
        this.loading.set(false);
      }
    };

    this.svc.listVisitorRegistrations(editionId).subscribe({
      next: vr => this.visitorRegistrations.set(vr),
      error: (err) => this.handleLoadError(ERROR.fetchRegistrations, err),
      complete,
    });

    this.svc.listPromotionCodes(editionId).subscribe({
      next: pc => this.promotionCodes.set(pc),
      error: (err) => this.handleLoadError(ERROR.fetchPromotionCodes, err),
      complete,
    });

    this.svc.listTicketTypes(editionId).subscribe({
      next: tt => this.ticketTypes.set(tt),
      error: (err) => this.handleLoadError(ERROR.fetchTicketTypes, err),
      complete,
    });
  }

  private reload(): void {
    const edition = this.editionCtx.activeEdition();
    if (edition) this.load(edition.id);
  }

  private handleError(context: string, err: unknown): void {
    this.error.set(toContextErrorMessage(err, context));
    this.saving.set(false);
  }

  private handleLoadError(context: string, err: unknown): void {
    this.error.set(toContextErrorMessage(err, context));
  }

  togglePromotionCodeForm(): void {
    this.showPromotionCodeForm.update(open => !open);
    if (!this.showPromotionCodeForm()) {
      this.resetPromotionCodeForm();
    }
  }

  private resetPromotionCodeForm(): void {
    this.promotionCodeForm.reset({
      code: '',
      description: '',
      discountType: 'Percentage',
      discountValue: 0,
      maxRedemptions: null,
      validFrom: '',
      validUntil: '',
      allowedTicketTypeIds: [],
    });
  }

  confirmPayment(reg: VisitorRegistrationAdminDto): void {
    const ref = prompt('Ange betalningsreferens:');
    if (ref === null) return;
    this.saving.set(true);
    this.svc.confirmVisitorPayment(reg.id, ref).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.confirmPayment, err),
    });
  }

  cancelRegistration(reg: VisitorRegistrationAdminDto): void {
    this.saving.set(true);
    this.svc.cancelVisitorRegistration(reg.id).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => this.handleError(ERROR.cancelRegistration, err),
    });
  }

  canCancelRegistration(reg: VisitorRegistrationAdminDto): boolean {
    return reg.status !== 'Cancelled';
  }







  createPromotionCode(): void {
    const edition = this.editionCtx.activeEdition();
    if (!edition || this.promotionCodeForm.invalid) return;

    const value = this.promotionCodeForm.getRawValue();
    const validFrom = value.validFrom?.trim() ? new Date(value.validFrom).toISOString() : null;
    const validUntil = value.validUntil?.trim() ? new Date(value.validUntil).toISOString() : null;

    this.saving.set(true);
    this.svc.createPromotionCode(edition.id, {
      code: value.code.trim(),
      description: value.description.trim(),
      discountType: value.discountType,
      discountValue: value.discountValue ?? 0,
      maxRedemptions: value.maxRedemptions,
      validFrom,
      validUntil,
      allowedTicketTypeIds: value.allowedTicketTypeIds.length > 0 ? value.allowedTicketTypeIds : null,
    }).subscribe({
      next: () => {
        this.resetPromotionCodeForm();
        this.showPromotionCodeForm.set(false);
        this.loadPromotionCodes(edition.id);
        this.saving.set(false);
      },
      error: (err) => this.handleError(ERROR.createPromotionCode, err),
    });
  }

  deactivatePromotionCode(code: PromotionCodeAdminDto): void {
    const edition = this.editionCtx.activeEdition();
    if (!edition || !confirm(`Deaktivera kampanjkoden ${code.code}?`)) return;

    this.saving.set(true);
    this.svc.deactivatePromotionCode(code.id).subscribe({
      next: () => {
        this.loadPromotionCodes(edition.id);
        this.saving.set(false);
      },
      error: (err) => this.handleError(ERROR.deactivatePromotionCode, err),
    });
  }

  togglePromotionHistory(code: PromotionCodeAdminDto): void {
    if (this.selectedPromotionCodeId() === code.id) {
      this.selectedPromotionCodeId.set(null);
      this.promotionHistory.set([]);
      return;
    }

    this.selectedPromotionCodeId.set(code.id);
    this.loadingHistoryFor.set(code.id);
    this.svc.listPromotionCodeRedemptions(code.id).subscribe({
      next: history => {
        this.promotionHistory.set(history);
        this.loadingHistoryFor.set(null);
      },
      error: (err) => {
        this.promotionHistory.set([]);
        this.loadingHistoryFor.set(null);
        this.handleLoadError(ERROR.fetchPromotionHistory, err);
      },
    });
  }

  isPromotionHistoryOpen(codeId: string): boolean {
    return this.selectedPromotionCodeId() === codeId;
  }

  promotionDiscountLabel(code: PromotionCodeAdminDto): string {
    if (code.discountType === 'Free') return 'Gratis';
    if (code.discountType === 'Percentage') return `${code.discountValue}%`;
    return `${code.discountValue} kr`;
  }

  allowedTicketTypeLabel(ids: string[] | null): string {
    if (!ids || ids.length === 0) return 'Alla biljettyper';

    const names = ids
      .map(id => this.ticketTypes().find(tt => tt.id === id)?.name)
      .filter((name): name is string => !!name);

    if (names.length === 0) return 'Valda biljettyper';
    return names.join(', ');
  }

  personNameFromRegistration(personId: string): string {
    return this.visitorRegistrations().find(reg => reg.personId === personId)?.personName ?? personId;
  }

  private loadPromotionCodes(editionId: string): void {
    this.svc.listPromotionCodes(editionId).subscribe({
      next: codes => this.promotionCodes.set(codes),
      error: (err) => this.handleLoadError(ERROR.fetchPromotionCodes, err),
    });
  }

  statusLabel(status: string): string {
    return VISITOR_REGISTRATION_STATUS_LABEL[status] ?? status;
  }

  statusChip(status: string): string {
    return VISITOR_REGISTRATION_STATUS_CHIP[status] ?? 'chip-orange';
  }
}
