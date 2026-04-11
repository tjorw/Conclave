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
  status: string;
}

export interface EditionDto {
  id: string;
  conventionId: string;
  name: string;
  start: string;
  end: string;
  status: string;
  organiserRegistrationOpen: boolean;
  staffRegistrationOpen: boolean;
  visitorRegistrationOpen: boolean;
  staffCoordinatorId: string | null;
  eventCoordinatorId: string | null;
  venues: VenueDto[];
  staffAreas: StaffAreaDto[];
  stations: StationDto[];
  categories: CategoryDto[];
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
}
