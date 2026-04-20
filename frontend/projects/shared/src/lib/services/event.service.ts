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

  approveEvent(eventId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/approve`,
      {}
    );
  }

  rejectEvent(eventId: string, comment: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/reject`,
      { comment }
    );
  }

  updateDraft(eventId: string, title: string, description: string, registrationType: string, dropInRules: string | null) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/events/${eventId}`,
      { title, description, registrationType, dropInRules }
    );
  }

  changeCategory(eventId: string, categoryId: string) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/category`,
      { categoryId }
    );
  }

  addSessionRequest(eventId: string, description: string, durationMinutes: number, seats: number, startType: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/events/${eventId}/session-requests`,
      { description, durationMinutes, seats, startType }
    );
  }

  removeSessionRequest(eventId: string, requestId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/session-requests/${requestId}`
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
