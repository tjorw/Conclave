import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from '../environment/environment.token';
import { EditionSessionDto, EventDto, EventSummaryDto } from '../models/event.models';
import { ConventionContextService } from './convention-context.service';

@Injectable({ providedIn: 'root' })
export class EventService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);
  private readonly conventionContext = inject(ConventionContextService);

  listEvents(editionId: string) {
    return this.http.get<EventSummaryDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/events`
    );
  }

  getMyEvents(editionId: string) {
    return this.http.get<EventSummaryDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-events`
    );
  }

  getEvent(eventId: string) {
    return this.http.get<EventDto>(`${this.env.apiBaseUrl}/events/${eventId}`);
  }

  createEvent(editionId: string, categoryId: string, leadOrganiserId: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/events`,
      {
        categoryId,
        leadOrganiserId,
        conventionId: this.conventionContext.requireConventionId(),
      }
    );
  }

  cancelEvent(eventId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/cancel`,
      {}
    );
  }

  approveEvent(eventId: string, organizerTicketAssignments: { personId: string; ticketTypeId: string | null }[] = []) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/approve`,
      { organizerTicketAssignments }
    );
  }

  rejectEvent(eventId: string, comment: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/reject`,
      { comment }
    );
  }

  updateDraft(
    eventId: string,
    title: string,
    description: string,
    registrationType: string,
    dropInRules: string | null,
    scheduleRequestText: string | null,
    coOrganiserCount: number
  ) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/events/${eventId}`,
      { title, description, registrationType, dropInRules, scheduleRequestText, coOrganiserCount }
    );
  }

  changeCategory(eventId: string, categoryId: string) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/category`,
      { categoryId }
    );
  }

  deleteEvent(eventId: string) {
    return this.http.delete<void>(`${this.env.apiBaseUrl}/events/${eventId}`);
  }

  returnToDraft(eventId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/return-to-draft`,
      {}
    );
  }

  submitForReview(eventId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/submit`,
      {}
    );
  }

  removeCoOrganiser(eventId: string, personId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/co-organisers/${personId}`
    );
  }

  adjustCoOrganiserLimit(eventId: string, limit: number) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/co-organiser-limit`,
      { limit }
    );
  }

  createCoOrganiserInvitation(eventId: string, email: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/co-organiser-invitations`,
      { email }
    );
  }

  cancelCoOrganiserInvitation(eventId: string, invitationId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/co-organiser-invitations/${invitationId}`
    );
  }

  redeemCoOrganiserInvitation(code: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/co-organiser-invitations/redeem`,
      { code }
    );
  }

  addEventComment(eventId: string, comment: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/comments`,
      { comment }
    );
  }

  respondToEventComment(eventId: string, commentId: string, response: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/comments/${commentId}/respond`,
      { response }
    );
  }

  acknowledgeEventComment(eventId: string, commentId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/comments/${commentId}/acknowledge`,
      {}
    );
  }

  scheduleSession(eventId: string, venueId: string, startTime: string, endTime: string, maxSeats: number, startType: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/events/${eventId}/sessions`,
      { venueId, startTime, endTime, maxSeats, startType }
    );
  }

  updateSession(eventId: string, sessionId: string, venueId: string, startTime: string, endTime: string, maxSeats: number, startType: string) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/sessions/${sessionId}`,
      { venueId, startTime, endTime, maxSeats, startType }
    );
  }

  deactivateSession(eventId: string, sessionId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/sessions/${sessionId}`
    );
  }

  getEditionSessions(editionId: string) {
    return this.http.get<EditionSessionDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/sessions`
    );
  }
}
