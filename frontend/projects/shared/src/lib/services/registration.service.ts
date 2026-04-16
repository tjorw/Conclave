import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from '../environment/environment.token';
import {
  MyVisitorRegistrationDto,
  MySessionRegistrationSummaryDto,
  MyWatchedSessionSummaryDto,
  MyStaffApplicationDto,
  TicketTypeAdminDto,
  VisitorTicketTypeDto,
  VisitorRegistrationAdminDto,
} from '../models/registration.models';

@Injectable({ providedIn: 'root' })
export class RegistrationService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  getMyVisitorRegistration(editionId: string) {
    return this.http.get<MyVisitorRegistrationDto | null>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-visitor-registration`
    );
  }

  getAvailableTicketTypes(editionId: string) {
    return this.http.get<VisitorTicketTypeDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/available-ticket-types`
    );
  }

  submitVisitorRegistration(editionId: string, personId: string, ticketTypeId: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/visitor-registrations`,
      { personId, ticketTypeId }
    );
  }

  getMySessionRegistrations(editionId: string) {
    return this.http.get<MySessionRegistrationSummaryDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-session-registrations`
    );
  }

  registerForSession(sessionId: string, personId: string, ticketId: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/sessions/${sessionId}/registrations`,
      { personId, ticketId }
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

  getMyStaffApplication(editionId: string) {
    return this.http.get<MyStaffApplicationDto | null>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-staff-application`
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
    isSellable: boolean; isPubliclyVisible: boolean;
  }) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/ticket-types`, body
    );
  }

  updateTicketType(editionId: string, ticketTypeId: string, body: {
    name: string; price: number;
    isSellable: boolean; isPubliclyVisible: boolean;
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
}
