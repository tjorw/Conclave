export type EventStatus = 'Draft' | 'UnderReview' | 'Published' | 'Cancelled';
export type EventCommentStatus = 'New' | 'InProgress' | 'Responded' | 'Acknowledged';
export type CoOrganiserApplicationStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';
export type SessionStatus = 'Active' | 'Inactive';
export type StartType = 'FixedTime' | 'Rolling' | 'Tournament';
export type RegistrationType = 'DropIn' | 'PreRegistration' | 'Combined';

export interface EventSummaryDto {
  id: string;
  editionId: string;
  categoryId: string;
  categoryName: string | null;
  leadOrganiserId: string;
  leadOrganiserName: string | null;
  status: EventStatus;
  title: string | null;
  sessionCount: number;
  pendingCommentCount: number;
  pendingCoOrganiserApplicationCount: number;
}

export interface EventDto {
  id: string;
  editionId: string;
  categoryId: string;
  categoryName: string | null;
  categoryResponsibleId: string | null;
  categoryResponsibleName: string | null;
  categoryOrganizerInstructions: string | null;
  leadOrganiserId: string;
  leadOrganiserName: string | null;
  status: EventStatus;
  title: string;
  description: string;
  scheduleRequestText: string | null;
  registrationType: RegistrationType;
  dropInRules: string | null;
  coOrganiserIds: string[];
  coOrganisers: CoOrganiserDto[];
  coOrganiserApplications: CoOrganiserApplicationDto[];
  sessions: SessionDto[];
  comments: EventCommentDto[];
}

export interface CoOrganiserDto {
  personId: string;
  personName: string | null;
}

export interface CoOrganiserApplicationDto {
  id: string;
  email: string;
  name: string | null;
  message: string | null;
  status: CoOrganiserApplicationStatus;
  requestedById: string;
  requestedAt: string;
  reviewedById: string | null;
  reviewedAt: string | null;
  reviewComment: string | null;
  approvedPersonId: string | null;
}

export interface SessionDto {
  id: string;
  venueId: string;
  start: string;
  end: string;
  maxSeats: number;
  startType: StartType;
  status: SessionStatus;
}

export interface EditionSessionDto {
  sessionId: string;
  eventId: string;
  eventTitle: string;
  venueId: string;
  start: string;
  end: string;
  maxSeats: number;
  startType: StartType;
  status: SessionStatus;
}

export interface EventCommentDto {
  id: string;
  authorId: string;
  authorName: string | null;
  text: string;
  status: EventCommentStatus;
  requiresHandling: boolean;
  handlingComment: string | null;
  handledById: string | null;
  handledByName: string | null;
  handledAt: string | null;
  acknowledgedById: string | null;
  acknowledgedAt: string | null;
  createdAt: string;
}
