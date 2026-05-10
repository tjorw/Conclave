import { Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { EDITION_CONTENT_KEYS, EventSummaryFeedDto } from 'shared';
import { EditionService } from '../../services/edition.service';
import { HomeContentStateService } from '../../services/home-content-state.service';
import { LocaleService } from '../../services/locale.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  readonly editionSvc   = inject(EditionService);
  private readonly homeContentState = inject(HomeContentStateService);
  private readonly localeSvc = inject(LocaleService);

  readonly featuredEvents = computed<EventSummaryFeedDto[]>(() =>
    this.homeContentState.featuredEventsFromApi() ?? (this.editionSvc.edition()?.events ?? []).slice(0, 3)
  );

  readonly heroTitle = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.heroTitle] || this.editionSvc.conventionName()
  );

  readonly heroIngress = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.heroIngress] || null
  );

  readonly heroPrimaryActionLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.heroPrimaryActionLabel] || 'Se programmet'
  );

  readonly ctaVisitorLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaVisitorLabel] || 'Bli besökare'
  );

  readonly ctaOrganiserLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaOrganiserLabel] || 'Arrangera ett evenemang'
  );

  readonly ctaStaffLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaStaffLabel] || 'Bli funktionär'
  );

  readonly ctaVisitorDescription = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaVisitorDescription] || 'Köp en biljett och delta i massor av evenemang under helgen.'
  );

  readonly ctaOrganiserDescription = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaOrganiserDescription] || 'Har du en idé till ett rollspel, brädspelssession eller seminarium? Skicka in det!'
  );

  readonly ctaStaffDescription = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaStaffDescription] || 'Hjälp till att driva konventet och jobba bakom kulisserna.'
  );

  readonly ctaVisitorOpenLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaVisitorOpenLabel] || 'Registrera dig nu'
  );

  readonly ctaOrganiserOpenLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaOrganiserOpenLabel] || 'Skicka in evenemang'
  );

  readonly ctaStaffOpenLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaStaffOpenLabel] || 'Ansök nu'
  );

  readonly ctaVisitorClosedLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaVisitorClosedLabel] || 'Registrering inte öppen än'
  );

  readonly ctaOrganiserClosedLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaOrganiserClosedLabel] || 'Inlämning inte öppen än'
  );

  readonly ctaStaffClosedLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.ctaStaffClosedLabel] || 'Ansökan inte öppen än'
  );

  readonly featuredSectionTitle = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.featuredSectionTitle] || 'Utvalda evenemang'
  );

  readonly featuredViewAllLabel = computed(() =>
    this.homeContentState.contentMap()[EDITION_CONTENT_KEYS.featuredViewAllLabel] || 'Visa hela programmet'
  );

  readonly firstSession = (event: EventSummaryFeedDto): string => {
    const s = event.sessions[0];
    if (!s) return '';
    return new Date(s.start).toLocaleDateString(this.localeSvc.localeTag(), { weekday: 'short', hour: '2-digit', minute: '2-digit' });
  };
}
