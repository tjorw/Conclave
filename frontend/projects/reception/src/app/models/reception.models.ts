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
  perks: string[];
  isCollected: boolean;
  collectedAt: string | null;
  createdAt: string;
}

export interface CollectTicketResultDto {
  ticketId: string;
  description: string | null;
}

export interface VisitorTicketTypeDto {
  id: string;
  name: string;
  price: number;
  description: string | null;
}
