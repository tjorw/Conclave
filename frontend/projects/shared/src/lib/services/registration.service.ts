import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from '../environment/environment.token';
import {
  MyVisitorRegistrationDto,
  MySessionRegistrationSummaryDto,
  MyWatchedSessionSummaryDto,
  MyOrganiserSessionSummaryDto,
  MyAssignedShiftSummaryDto,
  MyStaffApplicationDto,
  TicketTypeAdminDto,
  VisitorTicketTypeDto,
  VisitorRegistrationAdminDto,
  PromotionCodeAdminDto,
  PromotionCodeRedemptionHistoryDto,
  PromotionDiscountType,
  RedeemPromotionCodeResultDto,
} from '../models/registration.models';

@Injectable({ providedIn: 'root' })
export class RegistrationService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  getMyVisitorRegistration(editionId: string) {
    return this.http.get<MyVisitorRegistrationDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-visitor-registration`
    );
  }

  getAvailableTicketTypes(editionId: string) {
    return this.http.get<VisitorTicketTypeDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/available-ticket-types`
    );
  }

  submitVisitorRegistration(editionId: string, ticketTypeId: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/visitor-registrations`,
      { ticketTypeId }
    );
  }

  cancelVisitorRegistration(registrationId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/visitor-registrations/${registrationId}`
    );
  }

  getMySessionRegistrations(editionId: string) {
    return this.http.get<MySessionRegistrationSummaryDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-session-registrations`
    );
  }

  registerForSession(sessionId: string, ticketId: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/sessions/${sessionId}/registrations`,
      { ticketId }
    );
  }

  redeemPromotionCode(ticketId: string, code: string) {
    return this.http.post<RedeemPromotionCodeResultDto>(
      `${this.env.apiBaseUrl}/tickets/${ticketId}/redeem-promotion-code`,
      { code }
    );
  }

  cancelSessionRegistration(registrationId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/session-registrations/${registrationId}`
    );
  }

  watchSession(sessionId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/sessions/${sessionId}/watch`,
      {}
    );
  }

  unwatchSession(sessionId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/sessions/${sessionId}/watch`
    );
  }

  getMyWatchedSessions(editionId: string) {
    return this.http.get<MyWatchedSessionSummaryDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-watched-sessions`
    );
  }

  getMyOrganiserSessions(editionId: string) {
    return this.http.get<MyOrganiserSessionSummaryDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-organiser-sessions`
    );
  }

  getMyAssignedShifts(editionId: string) {
    return this.http.get<MyAssignedShiftSummaryDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-assigned-shifts`
    );
  }

  getMyStaffApplication(editionId: string) {
    return this.http.get<MyStaffApplicationDto | null>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-staff-application`
    );
  }

  submitStaffApplication(editionId: string, interestDescription: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/staff-applications`,
      { interestDescription }
    );
  }

  addStaffAvailability(applicationId: string, from: string, to: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/staff-applications/${applicationId}/availabilities`,
      { from, to }
    );
  }

  removeStaffAvailability(applicationId: string, availabilityId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/staff-applications/${applicationId}/availabilities/${availabilityId}`
    );
  }

  addStaffAreaPreference(applicationId: string, staffAreaId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/staff-applications/${applicationId}/staff-area-preferences`,
      { staffAreaId }
    );
  }

  removeStaffAreaPreference(applicationId: string, staffAreaId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/staff-applications/${applicationId}/staff-area-preferences/${staffAreaId}`
    );
  }

  // Admin

  listTicketTypes(editionId: string) {
    return this.http.get<TicketTypeAdminDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/ticket-types`
    );
  }

  createTicketType(editionId: string, body: {
    name: string; price: number; category: string;
    validDays?: string[] | null; allowedCategories?: string[] | null;
  }) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/ticket-types`, body
    );
  }

  updateTicketType(editionId: string, ticketTypeId: string, body: {
    name: string; price: number; category: string;
    validDays?: string[] | null; allowedCategories?: string[] | null;
  }) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/editions/${editionId}/ticket-types/${ticketTypeId}`, body
    );
  }

  deleteTicketType(editionId: string, ticketTypeId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/editions/${editionId}/ticket-types/${ticketTypeId}`
    );
  }

  listVisitorRegistrations(editionId: string) {
    return this.http.get<VisitorRegistrationAdminDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/visitor-registrations`
    );
  }

  confirmVisitorPayment(registrationId: string, externalReference: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/visitor-registrations/${registrationId}/confirm-payment`,
      { externalReference }
    );
  }

  revokeTicket(ticketId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/tickets/${ticketId}`
    );
  }

  listPromotionCodes(editionId: string) {
    return this.http.get<PromotionCodeAdminDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/promotion-codes`
    );
  }

  createPromotionCode(editionId: string, body: {
    code: string;
    description: string;
    discountType: PromotionDiscountType;
    discountValue: number;
    maxRedemptions?: number | null;
    validFrom?: string | null;
    validUntil?: string | null;
    allowedTicketTypeIds?: string[] | null;
  }) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/promotion-codes`,
      body
    );
  }

  deactivatePromotionCode(promotionCodeId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/promotion-codes/${promotionCodeId}/deactivate`,
      {}
    );
  }

  listPromotionCodeRedemptions(promotionCodeId: string) {
    return this.http.get<PromotionCodeRedemptionHistoryDto[]>(
      `${this.env.apiBaseUrl}/promotion-codes/${promotionCodeId}/redemptions`
    );
  }
}
