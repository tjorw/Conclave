import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from '../environment/environment.token';
import {
  CategoryDto,
  ConventionDto,
  EditionDto,
  EditionOrganiserDto,
  EditionResponsibleDto,
  EditionStaffMemberDto,
  EditionSummaryDto,
  EditionVisitorDto,
  PersonDto,
  StaffAreaDto,
  StationDto,
  VenueDto,
} from '../models/convention.models';

export interface CreateEditionRequest {
  name: string;
  startDate: string;
  endDate: string;
  staffCoordinatorId: string;
  eventCoordinatorId: string;
}

export interface CreateVenueRequest {
  name: string;
  building: string;
  description?: string | null;
}

export interface CreateStaffAreaRequest {
  name: string;
  description?: string | null;
  responsibleId: string;
}

export interface CreateStationRequest {
  name: string;
  description?: string | null;
  staffAreaId: string;
}

export interface UpdateStationRequest {
  name: string;
  description?: string | null;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string | null;
  responsibleId: string;
}

export interface UpdateEditionRequest {
  name: string;
  startDate: string;
  endDate: string;
  staffCoordinatorId: string;
  eventCoordinatorId: string;
}

export interface UpdateVenueRequest {
  name: string;
  building: string;
  description?: string | null;
}

export interface UpdateStaffAreaRequest {
  name: string;
  description?: string | null;
  responsibleId: string;
}

export interface UpdateCategoryRequest {
  name: string;
  description?: string | null;
  responsibleId: string;
}

export interface CreatePersonRequest {
  name: string;
  email: string;
  phone?: string | null;
}

export interface UpdatePersonRequest {
  name: string;
  email: string;
  phone?: string | null;
}

@Injectable({ providedIn: 'root' })
export class ConventionService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  private get base() {
    return `${this.env.apiBaseUrl}/conventions/${this.env.conventionId}`;
  }

  getCurrentConvention() {
    return this.http.get<ConventionDto>(`${this.env.apiBaseUrl}/convention`);
  }

  getConvention() {
    return this.http.get<ConventionDto>(this.base);
  }

  listEditions() {
    return this.http.get<EditionSummaryDto[]>(`${this.base}/editions`);
  }

  createEdition(request: CreateEditionRequest) {
    return this.http.post<{ id: string }>(`${this.base}/editions`, request);
  }

  listPersons() {
    return this.http.get<PersonDto[]>(`${this.base}/persons`);
  }

  getEdition(editionId: string) {
    return this.http.get<EditionDto>(`${this.env.apiBaseUrl}/editions/${editionId}`);
  }

  publishEdition(editionId: string) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/editions/${editionId}/publish`, {});
  }

  openRegistration(editionId: string, type: 'organiser' | 'staff' | 'visitor') {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/editions/${editionId}/registrations/${type}/open`,
      {}
    );
  }

  createVenue(editionId: string, request: CreateVenueRequest) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/venues`,
      request
    );
  }

  createStaffArea(editionId: string, request: CreateStaffAreaRequest) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/staff-areas`,
      request
    );
  }

  createStation(editionId: string, request: CreateStationRequest) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/stations`,
      request
    );
  }

  updateStation(editionId: string, stationId: string, request: UpdateStationRequest) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/editions/${editionId}/stations/${stationId}`,
      request
    );
  }

  removeStation(editionId: string, stationId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/editions/${editionId}/stations/${stationId}`
    );
  }

  createCategory(editionId: string, request: CreateCategoryRequest) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/categories`,
      request
    );
  }

  changeCategoryResponsible(editionId: string, categoryId: string, newResponsibleId: string) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/editions/${editionId}/categories/${categoryId}/responsible`,
      { newResponsibleId }
    );
  }

  updateEdition(editionId: string, request: UpdateEditionRequest) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/editions/${editionId}`, request);
  }

  updateVenue(editionId: string, venueId: string, request: UpdateVenueRequest) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/editions/${editionId}/venues/${venueId}`, request);
  }

  removeVenue(editionId: string, venueId: string) {
    return this.http.delete<void>(`${this.env.apiBaseUrl}/editions/${editionId}/venues/${venueId}`);
  }

  updateStaffArea(editionId: string, staffAreaId: string, request: UpdateStaffAreaRequest) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/editions/${editionId}/staff-areas/${staffAreaId}`, request);
  }

  removeStaffArea(editionId: string, staffAreaId: string) {
    return this.http.delete<void>(`${this.env.apiBaseUrl}/editions/${editionId}/staff-areas/${staffAreaId}`);
  }

  updateCategory(editionId: string, categoryId: string, request: UpdateCategoryRequest) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/editions/${editionId}/categories/${categoryId}`, request);
  }

  removeCategory(editionId: string, categoryId: string) {
    return this.http.delete<void>(`${this.env.apiBaseUrl}/editions/${editionId}/categories/${categoryId}`);
  }

  createPerson(request: CreatePersonRequest) {
    return this.http.post<{ id: string }>(`${this.base}/persons`, request);
  }

  updatePerson(personId: string, request: UpdatePersonRequest) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/persons/${personId}`, request);
  }

  deactivatePerson(personId: string) {
    return this.http.delete<void>(`${this.env.apiBaseUrl}/persons/${personId}`);
  }

  reactivatePerson(personId: string) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/persons/${personId}/reactivate`, {});
  }

  sendResetLink(personId: string) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/persons/${personId}/send-reset-link`, {});
  }

  lockAccount(personId: string) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/persons/${personId}/lock`, {});
  }

  unlockAccount(personId: string) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/persons/${personId}/unlock`, {});
  }

  setActiveEdition(editionId: string) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/editions/${editionId}/set-active`, {});
  }

  listEditionVisitors(editionId: string) {
    return this.http.get<EditionVisitorDto[]>(`${this.env.apiBaseUrl}/editions/${editionId}/visitors`);
  }

  listEditionOrganisers(editionId: string) {
    return this.http.get<EditionOrganiserDto[]>(`${this.env.apiBaseUrl}/editions/${editionId}/organisers`);
  }

  listEditionStaff(editionId: string) {
    return this.http.get<EditionStaffMemberDto[]>(`${this.env.apiBaseUrl}/editions/${editionId}/staff`);
  }

  listEditionResponsibles(editionId: string) {
    return this.http.get<EditionResponsibleDto[]>(`${this.env.apiBaseUrl}/editions/${editionId}/responsibles`);
  }
}
