import { of } from 'rxjs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { convertToParamMap, ActivatedRoute, Router } from '@angular/router';
import { PageService } from 'shared';
import { PageDetailComponent } from './page-detail.component';

function createPage(overrides: Partial<{ editionId: string | null }> = {}) {
  return {
    id: 'page-1',
    slug: 'test-page',
    title: 'Testsida',
    content: 'Innehall',
    editionId: null,
    isPublished: false,
    showInPublicMenu: true,
    menuSortOrder: 0,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

describe('PageDetailComponent scope validation', () => {
  let fixture: ComponentFixture<PageDetailComponent>;

  async function setup(params: Record<string, string>, pageEditionId: string | null) {
    const navigate = vi.fn().mockResolvedValue(true);
    const getPage = vi.fn().mockReturnValue(of(createPage({ editionId: pageEditionId })));

    await TestBed.configureTestingModule({
      imports: [PageDetailComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap(params),
            },
          },
        },
        {
          provide: Router,
          useValue: {
            navigate,
          },
        },
        {
          provide: PageService,
          useValue: {
            getPage,
            createPage: vi.fn(),
            updatePage: vi.fn(),
            publishPage: vi.fn(),
            unpublishPage: vi.fn(),
            deletePage: vi.fn(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PageDetailComponent);

    return { navigate, getPage, component: fixture.componentInstance };
  }

  it('redirects to convention list when convention route loads an edition-scoped page', async () => {
    const { navigate, getPage } = await setup({ pageId: 'page-1' }, 'edition-1');

    expect(getPage).toHaveBeenCalledWith('page-1');
    expect(navigate).toHaveBeenCalledWith(['/pages']);
  });

  it('redirects to edition list when edition route loads page from another edition', async () => {
    const { navigate, getPage } = await setup({ id: 'edition-1', pageId: 'page-1' }, 'edition-2');

    expect(getPage).toHaveBeenCalledWith('page-1');
    expect(navigate).toHaveBeenCalledWith(['/editions', 'edition-1', 'pages']);
  });

  it('stays on detail when page scope matches route scope', async () => {
    const { navigate, component } = await setup({ id: 'edition-1', pageId: 'page-1' }, 'edition-1');

    expect(navigate).not.toHaveBeenCalled();
    expect(component.page()?.editionId).toBe('edition-1');
  });
});
