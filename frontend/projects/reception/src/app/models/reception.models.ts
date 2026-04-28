export interface TicketSummaryDto {
  ticketId: string;
  ticketTypeName: string;
  status: string;
}

export interface PersonSearchResultDto {
  personId: string;
  name: string;
  email: string;
  phone: string | null;
  tickets: TicketSummaryDto[];
}

export interface PersonTicketDto {
  ticketId: string;
  ticketTypeId: string;
  ticketTypeName: string;
  ticketTypeCategory: string;
  status: string;
  finalPrice: number | null;
  validDays: string[] | null;
  allowedCategories: string[] | null;
  description: string | null;
  isCollected: boolean;
  collectedAt: string | null;
  createdAt: string;
}

export interface CollectTicketResultDto {
  ticketId: string;
  description: string | null;
}

export interface PersonShiftItemDto {
  shiftId: string;
  areaName: string;
  stationName: string;
  date: string;
  start: string;
  end: string;
  status: string;
  role: string;
}

export interface PersonSessionItemDto {
  sessionId: string;
  eventId: string;
  eventTitle: string;
  role: string;
  venueName: string;
  date: string;
  start: string;
  end: string;
}

export interface ScheduleDaySummaryDto {
  date: string;
  shiftCount: number;
  shiftHours: number;
  sessionCount: number;
  sessionHours: number;
  totalHours: number;
}

export interface ScheduleTotalDto {
  totalShiftHours: number;
  totalSessionHours: number;
  totalHours: number;
  workDays: string[];
}

export interface PersonScheduleDto {
  shifts: PersonShiftItemDto[];
  sessions: PersonSessionItemDto[];
  dailySummary: ScheduleDaySummaryDto[];
  total: ScheduleTotalDto;
}

export interface VisitorTicketTypeDto {
  id: string;
  name: string;
  price: number;
  description: string | null;
}
