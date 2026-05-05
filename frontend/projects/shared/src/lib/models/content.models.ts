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

export interface EditionContentDto {
  key: string;
  value: string;
}

export const EDITION_CONTENT_KEYS = {
  heroTitle:       'hero.title',
  heroIngress:     'hero.ingress',
  ctaVisitorLabel: 'cta.visitor.label',
  ctaOrganiserLabel: 'cta.organiser.label',
  ctaStaffLabel:   'cta.staff.label',
} as const;
