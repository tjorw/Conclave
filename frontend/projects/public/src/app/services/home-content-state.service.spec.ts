import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import {
  EditionContentService,
  EventService,
  EventSummaryFeedDto,
  PageService,
} from 'shared';
import { EditionService } from './edition.service';
import { HomeContentStateService } from './home-content-state.service';

describe('HomeContentStateService', () => {
  const editionId = signal<string | null>(null);

  const contentSvc = {
    getContent: vi.fn(),
  };

  const eventSvc = {
    getFeaturedEvents: vi.fn(),
  };

  const pageSvc = {
    listPublicMenuPages: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      providers: [
        HomeContentStateService,
        { provide: EditionService, useValue: { editionId } },
        { provide: EditionContentService, useValue: contentSvc },
        { provide: EventService, useValue: eventSvc },
        { provide: PageService, useValue: pageSvc },
      ],
    });

    editionId.set(null);
  });

  it('keeps rendering data when one API call fails', async () => {
    const featured: EventSummaryFeedDto[] = [
      {
        id: 'event-1',
        categoryId: 'cat-1',
        categoryName: 'Brädspel',
        title: 'Något kul',
        description: 'Beskrivning',
        programTags: [],
        leadOrganiserName: null,
        sessionCount: 1,
        sessions: [
          {
            id: 'session-1',
            venueName: 'Hall A',
            start: '2027-01-01T10:00:00Z',
            end: '2027-01-01T11:00:00Z',
            maxSeats: 8,
            bookedSeats: 0,
            startType: 'Scheduled',
          },
        ],
      },
    ];

    contentSvc.getContent.mockReturnValue(of([
      { key: 'hero.title', value: 'Välkommen' },
      { key: 'hero.ingress', value: '' },
    ]));
    eventSvc.getFeaturedEvents.mockReturnValue(of(featured));
    pageSvc.listPublicMenuPages.mockReturnValue(throwError(() => new Error('menu failed')));

    const service = TestBed.inject(HomeContentStateService);

    editionId.set('edition-1');
    (TestBed as unknown as { flushEffects?: () => void }).flushEffects?.();
    await Promise.resolve();

    expect(service.contentMap()['hero.title']).toBe('Välkommen');
    expect(service.contentMap()['hero.ingress']).toBeUndefined();
    expect(service.featuredEventsFromApi()).toEqual(featured);
    expect(service.menuPages()).toEqual([]);
  });
});
