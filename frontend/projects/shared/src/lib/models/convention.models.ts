import { StaffApplicationStatus } from './registration.models';
export type { StaffApplicationStatus };

export type EditionStatus = 'Draft' | 'Published';

export interface ConventionDto {
  id: string;
  name: string;
  slug: string;
}

export interface EditionSummaryDto {
  id: string;
  name: string;
  start: string;
  end: string;
  status: EditionStatus;
}

export interface EditionDto {
  id: string;
  conventionId: string;
  name: string;
  start: string;
  end: string;
  status: EditionStatus;
  organiserRegistrationOpen: boolean;
  staffRegistrationOpen: boolean;
  visitorRegistrationOpen: boolean;
  staffCoordinatorId: string | null;
  eventCoordinatorId: string | null;
  scheduleDays: EditionScheduleDayDto[];
  venues: VenueDto[];
  staffAreas: StaffAreaDto[];
  stations: StationDto[];
  categories: CategoryDto[];
}

export interface EditionScheduleDayDto {
  date: string;
  startTime: string | null;
  endTime: string | null;
}

export interface VenueDto {
  id: string;
  name: string;
  building: string;
  description: string | null;
}

export interface StaffAreaDto {
  id: string;
  name: string;
  description: string | null;
  responsibleId: string;
}

export interface StationDto {
  id: string;
  staffAreaId: string;
  name: string;
  description: string | null;
}

export interface CategoryDto {
  id: string;
  name: string;
  description: string | null;
  responsibleId: string;
}

export interface PersonDto {
  id: string;
  name: string;
  email: string;
  phone: string | null;
  isActive: boolean;
  isAdmin: boolean;
  hasAccount: boolean;
  isLocked: boolean;
}

export interface EditionVisitorDto {
  personId: string;
  personName: string;
  email: string;
  phone: string | null;
}

export interface EditionOrganiserDto {
  personId: string;
  personName: string;
  email: string;
  phone: string | null;
  eventId: string;
  eventTitle: string;
  role: string;
}

export interface EditionStaffMemberDto {
  personId: string;
  personName: string;
  email: string;
  phone: string | null;
  applicationStatus: StaffApplicationStatus;
}

export interface EditionResponsibleDto {
  position: string;
  personId: string | null;
  personName: string | null;
  email: string | null;
}

export interface ReceptionStaffMemberDto {
  personId: string;
  name: string;
  email: string;
  addedAt: string;
  addedById: string;
}
