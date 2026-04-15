export interface TicketTypeDto {
  id: string;
  name: string;
  price: number;
  capacity: number | null;
}

export interface VisitorRegistrationDto {
  id: string;
  personId: string;
  editionId: string;
  status: string;
  registeredAt: string;
}

export interface StaffApplicationDto {
  id: string;
  personId: string;
  editionId: string;
  status: string;
  appliedAt: string;
  availabilities: AvailabilityDto[];
  stationPreferences: StationPreferenceDto[];
}

export interface AvailabilityDto {
  id: string;
  start: string;
  end: string;
}

export interface StationPreferenceDto {
  stationId: string;
  rank: number;
}

export interface SessionRegistrationDto {
  id: string;
  personId: string;
  sessionId: string;
  registeredAt: string;
}

export interface MyVisitorRegistrationDto {
  id: string;
  status: string;
  ticketTypeName: string | null;
}

export interface MySessionRegistrationSummaryDto {
  id: string;
  sessionId: string;
  eventTitle: string;
  start: string;
  end: string;
  venueName: string;
  status: string;
}

export interface MyStaffApplicationDto {
  id: string;
  status: string;
}
