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
  status: string;
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
  status: string;
  assignments: StaffAssignmentDto[];
}

export interface StaffAssignmentDto {
  id: string;
  personId: string;
  personName: string | null;
  status: string;
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
  status: string;
  createdAt: string;
  stationPreferenceIds: string[];
  availabilities: StaffApplicationAvailabilityDto[];
}
