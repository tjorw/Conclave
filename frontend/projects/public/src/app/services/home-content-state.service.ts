import { effect, inject, Injectable, signal } from '@angular/core';
import { catchError, forkJoin, of } from 'rxjs';
import {
  EditionContentDto,
  EditionContentService,
  EventService,
  EventSummaryFeedDto,
  PageService,
  PublicPageMenuItemDto,
} from 'shared';
import { EditionService } from './edition.service';
import { LocaleService } from './locale.service';

@Injectable({ providedIn: 'root' })
export class HomeContentStateService {
  private readonly editionSvc = inject(EditionService);
  private readonly contentSvc = inject(EditionContentService);
  private readonly eventSvc = inject(EventService);
  private readonly pageSvc = inject(PageService);
  private readonly localeSvc = inject(LocaleService);

  private lastLoadedEditionId: string | null = null;
  private lastLoadedLocale: string | null = null;

  readonly contentMap = signal<Record<string, string>>({});
  readonly featuredEventsFromApi = signal<EventSummaryFeedDto[] | null>(null);
  readonly menuPages = signal<PublicPageMenuItemDto[]>([]);

  constructor() {
    effect(() => {
      const editionId = this.editionSvc.editionId();
      const locale = this.localeSvc.locale();
      if (!editionId) return;

      const editionChanged = this.lastLoadedEditionId !== editionId;
      const localeChanged = this.lastLoadedLocale !== locale;
      if (!editionChanged && !localeChanged) return;

      this.lastLoadedEditionId = editionId;
      this.lastLoadedLocale = locale;
      this.load(editionId, locale);
    });
  }

  private load(editionId: string, locale: string): void {
    forkJoin({
      content: this.contentSvc.getContent(editionId).pipe(catchError(() => of([] as EditionContentDto[]))),
      featured: this.eventSvc.getFeaturedEvents().pipe(catchError(() => of(null))),
      menu: this.pageSvc.listPublicMenuPages(locale).pipe(catchError(() => of([] as PublicPageMenuItemDto[]))),
    }).subscribe(({ content, featured, menu }) => {
      this.contentMap.set(this.toContentMap(content));
      this.featuredEventsFromApi.set(featured);
      this.menuPages.set(menu);
    });
  }

  private toContentMap(items: EditionContentDto[]): Record<string, string> {
    const map: Record<string, string> = {};
    for (const item of items) {
      if (item.value) map[item.key] = item.value;
    }

    return map;
  }
}
