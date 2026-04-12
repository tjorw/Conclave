import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from '../environment/environment.token';
import { EventDto, EventSummaryDto } from '../models/event.models';

@Injectable({ providedIn: 'root' })
export class EventService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  listEvents(editionId: string) {
    return this.http.get<EventSummaryDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/events`
    );
  }

  getEvent(eventId: string) {
    return this.http.get<EventDto>(`${this.env.apiBaseUrl}/events/${eventId}`);
  }

  createEvent(editionId: string, categoryId: string, leadOrganiserId: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/events`,
      { categoryId, leadOrganiserId, conventionId: this.env.conventionId }
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
      `${this.env.apiBaseUrl}/events/${eventId}/draft`,
      { title, description, registrationType, dropInRules }
    );
  }

  addSessionRequest(eventId: string, description: string, durationMinutes: number, seats: number, startType: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/events/${eventId}/draft/session-requests`,
      { description, durationMinutes, seats, startType }
    );
  }

  removeSessionRequest(eventId: string, requestId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/draft/session-requests/${requestId}`
    );
  }

  submitForReview(eventId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/events/${eventId}/submit`,
      {}
    );
  }
}
