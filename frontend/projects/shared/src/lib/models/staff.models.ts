import { StaffApplicationStatus } from './registration.models';
export type { StaffApplicationStatus };

export type ShiftStatus = 'Planned' | 'InProgress' | 'Cancelled' | 'Completed';
export type StaffAssignmentStatus = 'Assigned' | 'Confirmed' | 'Rejected' | 'Cancelled';

export interface ShiftSummaryDto {
  id: string;
  stationId: string;
  responsibleId: string;
  responsibleName: string | null;
  start: string;
  end: string;
  minPersons: number;
  maxPersons: number;
  activeAssignmentCount: number;
  status: ShiftStatus;
}

export interface ShiftDto {
  id: string;
  stationId: string;
  responsibleId: string;
  responsibleName: string | null;
  start: string;
  end: string;
  minPersons: number;
  maxPersons: number;
  status: ShiftStatus;
  assignments: StaffAssignmentDto[];
}

export interface StaffAssignmentDto {
  id: string;
  personId: string;
  personName: string | null;
  status: StaffAssignmentStatus;
  assignedAt: string;
}

export interface StaffApplicationAvailabilityDto {
  start: string;
  end: string;
}

export interface StaffApplicationSummaryDto {
  id: string;
  personId: string;
  personName: string | null;
  interestDescription: string;
  status: StaffApplicationStatus;
  createdAt: string;
  stationPreferenceIds: string[];
  availabilities: StaffApplicationAvailabilityDto[];
}
