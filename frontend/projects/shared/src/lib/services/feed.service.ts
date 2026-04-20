import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ENVIRONMENT } from '../environment/environment.token';
import { EditionFeedDto, EventFeedDto } from '../models/feed.models';
import { ConventionContextService } from './convention-context.service';

@Injectable({ providedIn: 'root' })
export class FeedService {
  private readonly http = inject(HttpClient);
  private readonly env  = inject(ENVIRONMENT);
  private readonly conventionContext = inject(ConventionContextService);

  feedBase(): string {
    return `${this.env.apiBaseUrl}/feed/${this.conventionContext.requireConventionId()}`;
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
