export interface PageSummaryDto {
  id: string;
  slug: string;
  title: string;
  editionId: string | null;
  isPublished: boolean;
  showInPublicMenu: boolean;
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
  editionId: string | null;
}

export interface SavePageRequest {
  slug: string;
  title: string;
  content: string;
  editionId: string | null;
  showInPublicMenu: boolean;
}
