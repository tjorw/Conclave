import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from '../environment/environment.token';
import { ShiftSummaryDto, ShiftDto, StaffApplicationSummaryDto, StaffScheduleDto } from '../models/staff.models';

@Injectable({ providedIn: 'root' })
export class StaffService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  listShifts(stationId: string) {
    return this.http.get<ShiftSummaryDto[]>(
      `${this.env.apiBaseUrl}/stations/${stationId}/shifts`
    );
  }

  getShift(shiftId: string) {
    return this.http.get<ShiftDto>(`${this.env.apiBaseUrl}/shifts/${shiftId}`);
  }

  createShift(stationId: string, responsibleId: string, startTime: string, endTime: string, minPersons: number, maxPersons: number) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/stations/${stationId}/shifts`,
      { responsibleId, startTime, endTime, minPersons, maxPersons }
    );
  }

  cancelShift(shiftId: string) {
    return this.http.post<void>(`${this.env.apiBaseUrl}/shifts/${shiftId}/cancel`, {});
  }

  updateShift(shiftId: string, stationId: string, responsibleId: string, startTime: string, endTime: string, minPersons: number, maxPersons: number) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/shifts/${shiftId}`,
      { stationId, responsibleId, startTime, endTime, minPersons, maxPersons }
    );
  }

  assignPerson(shiftId: string, personId: string) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/shifts/${shiftId}/assignments`,
      { personId }
    );
  }

  confirmAssignment(shiftId: string, assignmentId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/shifts/${shiftId}/assignments/${assignmentId}/confirm`,
      {}
    );
  }

  rejectAssignment(shiftId: string, assignmentId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/shifts/${shiftId}/assignments/${assignmentId}/reject`,
      {}
    );
  }

  cancelAssignment(shiftId: string, assignmentId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/shifts/${shiftId}/assignments/${assignmentId}`
    );
  }

  listStaffApplications(editionId: string) {
    return this.http.get<StaffApplicationSummaryDto[]>(
      `${this.env.apiBaseUrl}/editions/${editionId}/staff-applications`
    );
  }

  getStaffApplication(applicationId: string) {
    return this.http.get<StaffApplicationSummaryDto>(
      `${this.env.apiBaseUrl}/staff-applications/${applicationId}`
    );
  }

  updateApplication(
    applicationId: string,
    body: { interestDescription: string; availabilities: Array<{ from: string; to: string }>; staffAreaIds: string[] }
  ) {
    return this.http.put<void>(
      `${this.env.apiBaseUrl}/staff-applications/${applicationId}`,
      body
    );
  }

  deleteApplication(applicationId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/staff-applications/${applicationId}`
    );
  }

  getStaffSchedule(editionId: string, staffAreaId?: string | null) {
    return this.http.get<StaffScheduleDto>(
      `${this.env.apiBaseUrl}/editions/${editionId}/staff-schedule`,
      {
        params: staffAreaId ? { staffAreaId } : {},
      }
    );
  }

  addStaffMember(editionId: string, body: { name: string; email: string; phone?: string | null; note?: string | null }) {
    return this.http.post<{ id: string }>(
      `${this.env.apiBaseUrl}/editions/${editionId}/staff`,
      body
    );
  }

  acceptApplication(applicationId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/staff-applications/${applicationId}/accept`,
      {}
    );
  }

  rejectApplication(applicationId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/staff-applications/${applicationId}/reject`,
      {}
    );
  }
}
