import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from '../environment/environment.token';
import {
  CategoryDto,
  ConventionDto,
  EditionDto,
  EditionSummaryDto,
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

export interface CreateCategoryRequest {
  name: string;
  description?: string | null;
  responsibleId: string;
}

@Injectable({ providedIn: 'root' })
export class ConventionService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  private get base() {
    return `${this.env.apiBaseUrl}/conventions/${this.env.conventionId}`;
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

  createCategory(editionId: string, request: CreateCategoryRequest) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/categories`,
      request
    );
  }

  changeCategoryResponsible(editionId: string, categoryId: string, newResponsibleId: string) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/editions/${editionId}/categories/${categoryId}`,
      { newResponsibleId }
    );
  }
}
