export type VisitorRegistrationStatus = 'PendingPayment' | 'Confirmed' | 'Cancelled';
export type SessionRegistrationStatus = 'Confirmed' | 'Cancelled';
export type StaffApplicationStatus = 'Received' | 'UnderReview' | 'Assigned' | 'Confirmed' | 'Rejected';
export type TicketStatus = 'Reserved' | 'Paid' | 'Collected' | 'Revoked';
export type TicketTypeCategory = 'Visitor' | 'Organiser' | 'Staff';
export type PromotionDiscountType = 'Percentage' | 'Fixed' | 'Free';
export type TeamRegistrationStatus = 'Pending' | 'Confirmed' | 'Cancelled';

export interface TicketTypeAdminDto {
  id: string;
  name: string;
  price: number;
  category: TicketTypeCategory;
  validDays: string[] | null;
  allowedCategories: string[] | null;
  description: string | null;
}

export interface VisitorTicketTypeDto {
  id: string;
  name: string;
  price: number;
  description: string | null;
}

export interface OrganiserTicketTypeDto {
  id: string;
  name: string;
  price: number;
  description: string | null;
}

export interface OrganiserTicketAssignmentDto {
  personId: string;
  ticketId: string | null;
  ticketTypeId: string | null;
  ticketTypeName: string | null;
  status: TicketStatus | null;
}

export interface StaffTicketTypeDto {
  id: string;
  name: string;
  price: number;
  description: string | null;
}

export interface StaffTicketAssignmentDto {
  personId: string;
  ticketId: string | null;
  ticketTypeId: string | null;
  ticketTypeName: string | null;
  status: TicketStatus | null;
}

export interface PromotionCodeAdminDto {
  id: string;
  code: string;
  description: string;
  discountType: PromotionDiscountType;
  discountValue: number;
  isActive: boolean;
  redemptionCount: number;
  maxRedemptions: number | null;
  validFrom: string | null;
  validUntil: string | null;
  allowedTicketTypeIds: string[] | null;
}

export interface PromotionCodeRedemptionHistoryDto {
  id: string;
  personId: string;
  ticketId: string;
  redeemedAt: string;
  discountApplied: number;
  finalPrice: number;
}

export interface RedeemPromotionCodeResultDto {
  ticketId: string;
  promotionCodeId: string;
  discountApplied: number;
  finalPrice: number;
  ticketStatus: TicketStatus;
}

export interface VisitorRegistrationAdminDto {
  id: string;
  personId: string;
  personName: string;
  ticketTypeName: string | null;
  status: VisitorRegistrationStatus;
  registeredAt: string;
  paymentReference: string | null;
}

export interface TeamRegistrationSummaryDto {
  id: string;
  teamId: string;
  teamName: string;
  captainPersonId: string;
  captainName: string;
  status: TeamRegistrationStatus;
  createdAt: string;
  updatedAt: string | null;
}

export interface VisitorRegistrationDto {
  id: string;
  personId: string;
  editionId: string;
  status: VisitorRegistrationStatus;
  registeredAt: string;
}

export interface StaffApplicationDto {
  id: string;
  personId: string;
  editionId: string;
  status: StaffApplicationStatus;
  appliedAt: string;
  availabilities: AvailabilityDto[];
  staffAreaPreferences: StaffAreaPreferenceDto[];
}

export interface AvailabilityDto {
  id: string;
  start: string;
  end: string;
}

export interface StaffAreaPreferenceDto {
  staffAreaId: string;
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
  status: VisitorRegistrationStatus | TicketStatus;
  ticketTypeName: string | null;
  ticketId: string;
  ticketPrice: number | null;
  ticketTypeCategory: TicketTypeCategory;
  ticketStatus: TicketStatus;
  ticketTypeDescription: string | null;
  validDays: string[] | null;
  canCancel: boolean;
}

export interface MySessionRegistrationSummaryDto {
  id: string;
  sessionId: string;
  eventTitle: string;
  start: string;
  end: string;
  venueName: string;
  status: SessionRegistrationStatus;
}

export interface MyWatchedSessionSummaryDto {
  sessionId: string;
  eventTitle: string;
  start: string;
  end: string;
  venueName: string;
  createdAt: string;
}

export interface MyOrganiserSessionSummaryDto {
  sessionId: string;
  eventTitle: string;
  start: string;
  end: string;
  venueName: string;
}

export interface MyAssignedShiftSummaryDto {
  shiftId: string;
  stationName: string;
  role: string;
  start: string;
  end: string;
}

export interface MyStaffApplicationDto {
  id: string;
  status: StaffApplicationStatus;
}

export interface MyScheduleItemDto {
  sessionId: string | null;
  shiftId: string | null;
  title: string;
  start: string;
  end: string;
  locationName: string | null;
  type: 'Booked' | 'Watching' | 'Organiser' | 'Shift';
  isPrimary: boolean;
}
