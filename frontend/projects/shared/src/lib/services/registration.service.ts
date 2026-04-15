import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from '../environment/environment.token';
import {
  MyVisitorRegistrationDto,
  MySessionRegistrationSummaryDto,
  MyStaffApplicationDto,
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

  getMySessionRegistrations(editionId: string) {
    return this.http.get<MySessionRegistrationSummaryDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-session-registrations`
    );
  }

  getMyStaffApplication(editionId: string) {
    return this.http.get<MyStaffApplicationDto | null>(
      `${this.env.apiBaseUrl}/editions/${editionId}/my-staff-application`
    );
  }
}
