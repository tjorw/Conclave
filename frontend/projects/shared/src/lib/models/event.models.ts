export interface EventSummaryDto {
  id: string;
  editionId: string;
  categoryId: string;
  categoryName: string | null;
  leadOrganiserId: string;
  leadOrganiserName: string | null;
  status: string;
  title: string | null;
  sessionCount: number;
}

export interface EventDto {
  id: string;
  editionId: string;
  categoryId: string;
  leadOrganiserId: string;
  leadOrganiserName: string | null;
  status: string;
  title: string;
  description: string;
  registrationType: string;
  dropInRules: string | null;
  coOrganiserIds: string[];
  sessionRequests: SessionRequestDto[];
  sessions: SessionDto[];
  comments: EventCommentDto[];
}

export interface SessionRequestDto {
  id: string;
  description: string;
  durationMinutes: number;
  seats: number;
  startType: string;
}

export interface SessionDto {
  id: string;
  venueId: string;
  start: string;
  end: string;
  maxSeats: number;
  startType: string;
  status: string;
}

export interface EventCommentDto {
  id: string;
  authorId: string;
  text: string;
  createdAt: string;
}
