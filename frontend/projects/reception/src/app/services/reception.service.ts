import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ENVIRONMENT } from 'shared';
import {
  CollectTicketResultDto,
  PersonSearchResultDto,
  PersonTicketDto,
} from '../models/reception.models';

@Injectable({ providedIn: 'root' })
export class ReceptionService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  searchPersons(editionId: string, q: string) {
    const params = new HttpParams().set('q', q);
    return this.http.get<PersonSearchResultDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/persons/search`,
      { params }
    );
  }

  getPersonTickets(personId: string, editionId: string) {
    const params = new HttpParams().set('editionId', editionId);
    return this.http.get<PersonTicketDto[]>(
      `${this.env.apiBaseUrl}/persons/${personId}/tickets`,
      { params }
    );
  }

  collectTicket(ticketId: string) {
    return this.http.post<CollectTicketResultDto>(
      `${this.env.apiBaseUrl}/tickets/${ticketId}/collect`,
      {}
    );
  }
}
