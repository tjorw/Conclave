export interface EditionFeedDto {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  organiserRegistrationOpen: boolean;
  staffRegistrationOpen: boolean;
  visitorRegistrationOpen: boolean;
  venues: VenueFeedDto[];
  categories: CategoryFeedDto[];
  events: EventSummaryFeedDto[];
}

export interface VenueFeedDto {
  id: string;
  name: string;
  building: string;
  description: string | null;
}

export interface CategoryFeedDto {
  id: string;
  name: string;
  publicDescription: string | null;
}

export interface EventSummaryFeedDto {
  id: string;
  categoryId: string;
  categoryName: string | null;
  title: string;
  description: string;
  leadOrganiserName: string | null;
  sessionCount: number;
  sessions: SessionSummaryFeedDto[];
}

export interface SessionSummaryFeedDto {
  id: string;
  venueName: string;
  start: string;
  end: string;
  maxSeats: number;
  bookedSeats: number;
  startType: string;
}

export interface EventFeedDto {
  id: string;
  editionId: string;
  categoryId: string;
  categoryName: string | null;
  title: string;
  description: string;
  registrationType: string;
  dropInRules: string | null;
  sessions: SessionFeedDto[];
}

export interface SessionFeedDto {
  id: string;
  venueId: string;
  venueName: string;
  start: string;
  end: string;
  maxSeats: number;
  bookedSeats: number;
  startType: string;
}
