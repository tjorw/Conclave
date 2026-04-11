export interface ShiftSummaryDto {
  id: string;
  stationId: string;
  responsibleId: string;
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
  assignedById: string;
  status: string;
  assignedAt: string;
}
