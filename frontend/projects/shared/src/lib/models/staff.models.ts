import { EditionScheduleDayDto } from './convention.models';
import { StaffApplicationStatus } from './registration.models';
export type { StaffApplicationStatus };

export type ShiftStatus = 'Planned' | 'InProgress' | 'Cancelled' | 'Completed';
export type StaffAssignmentStatus = 'Assigned' | 'Confirmed' | 'Rejected' | 'Cancelled';
export type StaffingStatus = 'Cancelled' | 'Unstaffed' | 'UnderMin' | 'OverMax' | 'Full' | 'WithinRequirement';

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

export interface StaffScheduleDto {
  editionId: string;
  staffAreaFilterId: string | null;
  scheduleDays: EditionScheduleDayDto[];
  staffAreas: StaffScheduleAreaDto[];
}

export interface StaffScheduleAreaDto {
  staffAreaId: string;
  name: string;
  description: string | null;
  responsibleId: string;
  responsibleName: string | null;
  stations: StaffScheduleStationDto[];
}

export interface StaffScheduleStationDto {
  stationId: string;
  name: string;
  description: string | null;
  shifts: StaffScheduleShiftDto[];
}

export interface StaffScheduleShiftDto {
  shiftId: string;
  stationId: string;
  responsibleId: string;
  responsibleName: string | null;
  start: string;
  end: string;
  minPersons: number;
  maxPersons: number;
  activeAssignmentCount: number;
  confirmedAssignmentCount: number;
  status: ShiftStatus;
  staffingStatus: StaffingStatus;
}
