export interface EventSummaryDto {
  id: string;
  editionId: string;
  categoryId: string;
  categoryName: string | null;
  leadOrganiserId: string;
  status: string;
  title: string | null;
  sessionCount: number;
}

export interface EventDto {
  id: string;
  editionId: string;
  categoryId: string;
  leadOrganiserId: string;
  status: string;
  coOrganiserIds: string[];
  publishedVersion: EventVersionDto | null;
  draftVersion: EventVersionDto | null;
  sessions: SessionDto[];
}

export interface EventVersionDto {
  id: string;
  title: string;
  description: string;
  registrationType: string;
  dropInRules: string | null;
  status: string;
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
