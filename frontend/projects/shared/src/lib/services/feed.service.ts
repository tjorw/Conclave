import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ENVIRONMENT } from '../environment/environment.token';
import { EditionFeedDto, EventFeedDto } from '../models/feed.models';

@Injectable({ providedIn: 'root' })
export class FeedService {
  private readonly http = inject(HttpClient);
  private readonly env  = inject(ENVIRONMENT);

  feedBase(): string {
    return `${this.env.apiBaseUrl}/feed/${this.env.conventionId}`;
  }

  getEdition(editionId: string): Observable<EditionFeedDto> {
    return this.http.get<EditionFeedDto>(`${this.feedBase()}/editions/${editionId}`);
  }

  getActiveEdition(): Observable<EditionFeedDto> {
    return this.http.get<EditionFeedDto>(`${this.feedBase()}/active-edition`);
  }

  getEvent(eventId: string): Observable<EventFeedDto> {
    return this.http.get<EventFeedDto>(`${this.feedBase()}/events/${eventId}`);
  }
}
