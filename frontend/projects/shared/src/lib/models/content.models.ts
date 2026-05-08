export interface PageSummaryDto {
  id: string;
  slug: string;
  title: string;
  editionId: string | null;
  isPublished: boolean;
  showInPublicMenu: boolean;
  menuSortOrder: number;
  updatedAt: string;
}

export interface PageDto {
  id: string;
  slug: string;
  title: string;
  content: string;
  editionId: string | null;
  isPublished: boolean;
  showInPublicMenu: boolean;
  menuSortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface PublicPageDto {
  slug: string;
  title: string;
  content: string;
  editionId: string | null;
}

export interface PublicPageMenuItemDto {
  slug: string;
  title: string;
  menuSortOrder: number;
  editionId: string | null;
}

export interface SavePageRequest {
  slug: string;
  title: string;
  content: string;
  editionId: string | null;
  showInPublicMenu: boolean;
}

export interface UpdatePageMenuOrderRequest {
  menuSortOrder: number;
}

export interface MailTemplateSummaryDto {
  templateType: string;
  isCustomized: boolean;
  updatedAt: string | null;
}

export interface MailTemplateDto {
  templateType: string;
  subject: string;
  bodyMarkdown: string;
  isCustomized: boolean;
  updatedAt: string | null;
  availableVariables: string[];
}

export interface UpdateMailTemplateRequest {
  subject: string;
  bodyMarkdown: string;
}

export interface EditionContentDto {
  key: string;
  value: string;
}

export const EDITION_CONTENT_KEYS = {
  heroTitle:       'hero.title',
  heroIngress:     'hero.ingress',
  heroPrimaryActionLabel: 'hero.primaryActionLabel',
  ctaVisitorLabel: 'cta.visitor.label',
  ctaOrganiserLabel: 'cta.organiser.label',
  ctaStaffLabel:   'cta.staff.label',
  ctaVisitorDescription: 'cta.visitor.description',
  ctaOrganiserDescription: 'cta.organiser.description',
  ctaStaffDescription: 'cta.staff.description',
  ctaVisitorOpenLabel: 'cta.visitor.openLabel',
  ctaOrganiserOpenLabel: 'cta.organiser.openLabel',
  ctaStaffOpenLabel: 'cta.staff.openLabel',
  ctaVisitorClosedLabel: 'cta.visitor.closedLabel',
  ctaOrganiserClosedLabel: 'cta.organiser.closedLabel',
  ctaStaffClosedLabel: 'cta.staff.closedLabel',
  featuredSectionTitle: 'featured.sectionTitle',
  featuredViewAllLabel: 'featured.viewAllLabel',
} as const;
