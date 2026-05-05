import { signal } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeSv from '@angular/common/locales/sv';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EditionFeedDto, EventSummaryFeedDto } from 'shared';
import { HomeComponent } from './home.component';
import { EditionService } from '../../services/edition.service';
import { HomeContentStateService } from '../../services/home-content-state.service';

function createEdition(overrides: Partial<EditionFeedDto> = {}): EditionFeedDto {
  return {
    id: 'edition-1',
    name: 'Konvent 2027',
    startDate: '2027-05-01T00:00:00Z',
    endDate: '2027-05-03T00:00:00Z',
    organiserRegistrationOpen: false,
    staffRegistrationOpen: false,
    visitorRegistrationOpen: false,
    venues: [],
    categories: [],
    events: [],
    ...overrides,
  };
}

describe('HomeComponent', () => {
  let fixture: ComponentFixture<HomeComponent>;
  let component: HomeComponent;

  const editionSignal = signal<EditionFeedDto | null>(createEdition());
  const conventionNameSignal = signal('Fallback-konvent');
  const contentMapSignal = signal<Record<string, string>>({});
  const featuredFromApiSignal = signal<EventSummaryFeedDto[] | null>(null);

  beforeEach(async () => {
    registerLocaleData(localeSv);

    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        provideRouter([]),
        {
          provide: EditionService,
          useValue: {
            edition: editionSignal,
            conventionName: conventionNameSignal,
          },
        },
        {
          provide: HomeContentStateService,
          useValue: {
            contentMap: contentMapSignal,
            featuredEventsFromApi: featuredFromApiSignal,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
  });

  it('uses CMS values when keys exist', () => {
    contentMapSignal.set({
      'hero.title': 'CMS Hero',
      'hero.primaryActionLabel': 'Se allt',
      'cta.visitor.description': 'CMS Besökartext',
      'cta.organiser.description': 'CMS Arrangörstext',
      'cta.staff.description': 'CMS Funktionärstext',
      'cta.visitor.openLabel': 'Besökare öppet',
      'cta.organiser.openLabel': 'Arrangör öppet',
      'cta.staff.openLabel': 'Funktionär öppet',
      'cta.visitor.closedLabel': 'Stängd via CMS',
      'cta.organiser.closedLabel': 'Arrangör stängd via CMS',
      'cta.staff.closedLabel': 'Funktionär stängd via CMS',
      'featured.sectionTitle': 'Redaktionens val',
      'featured.viewAllLabel': 'Se hela listan',
    });

    fixture.detectChanges();

    expect(component.heroTitle()).toBe('CMS Hero');
    expect(component.heroPrimaryActionLabel()).toBe('Se allt');
    expect(component.ctaVisitorDescription()).toBe('CMS Besökartext');
    expect(component.ctaOrganiserDescription()).toBe('CMS Arrangörstext');
    expect(component.ctaStaffDescription()).toBe('CMS Funktionärstext');
    expect(component.ctaVisitorOpenLabel()).toBe('Besökare öppet');
    expect(component.ctaOrganiserOpenLabel()).toBe('Arrangör öppet');
    expect(component.ctaStaffOpenLabel()).toBe('Funktionär öppet');
    expect(component.ctaVisitorClosedLabel()).toBe('Stängd via CMS');
    expect(component.ctaOrganiserClosedLabel()).toBe('Arrangör stängd via CMS');
    expect(component.ctaStaffClosedLabel()).toBe('Funktionär stängd via CMS');
    expect(component.featuredSectionTitle()).toBe('Redaktionens val');
    expect(component.featuredViewAllLabel()).toBe('Se hela listan');

    const closedLabel = fixture.nativeElement.querySelector('.cta-card .cta-closed') as HTMLElement;
    expect(closedLabel.textContent?.trim()).toBe('Stängd via CMS');
  });

  it('uses fallback values when key is missing or empty', () => {
    contentMapSignal.set({
      'hero.title': '',
      'hero.primaryActionLabel': '',
    });

    fixture.detectChanges();

    expect(component.heroTitle()).toBe('Fallback-konvent');
    expect(component.heroPrimaryActionLabel()).toBe('Se programmet');
    expect(component.ctaVisitorDescription()).toBe('Köp en biljett och delta i massor av evenemang under helgen.');
    expect(component.ctaOrganiserDescription()).toBe('Har du en idé till ett rollspel, brädspelssession eller seminarium? Skicka in det!');
    expect(component.ctaStaffDescription()).toBe('Hjälp till att driva konventet och jobba bakom kulisserna.');
    expect(component.ctaVisitorOpenLabel()).toBe('Registrera dig nu');
    expect(component.ctaOrganiserOpenLabel()).toBe('Skicka in evenemang');
    expect(component.ctaStaffOpenLabel()).toBe('Ansök nu');
    expect(component.ctaVisitorClosedLabel()).toBe('Registrering inte öppen än');
    expect(component.ctaOrganiserClosedLabel()).toBe('Inlämning inte öppen än');
    expect(component.ctaStaffClosedLabel()).toBe('Ansökan inte öppen än');
    expect(component.featuredSectionTitle()).toBe('Utvalda evenemang');
    expect(component.featuredViewAllLabel()).toBe('Visa hela programmet');
  });
});
