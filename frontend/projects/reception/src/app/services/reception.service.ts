import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ENVIRONMENT } from 'shared';
import {
  CollectTicketResultDto,
  PersonSearchResultDto,
  PersonTicketDto,
  VisitorTicketTypeDto,
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

  listWalkupTicketTypes(editionId: string) {
    return this.http.get<VisitorTicketTypeDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/walkup-ticket-types`
    );
  }

  createWalkupPerson(editionId: string, name: string, email: string, phone: string | null) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/walkup-persons`,
      { name, email, phone }
    );
  }

  walkupRegister(editionId: string, personId: string, ticketTypeId: string) {
    return this.http.post<{ ticketId: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/walkup-registrations`,
      { personId, ticketTypeId }
    );
  }
}
