import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxChange, MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, EditionDto, toContextErrorMessage } from 'shared';

@Component({
  selector: 'app-edition-export',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './edition-export.component.html',
  styleUrl: './edition-export.component.scss',
})
export class EditionExportComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly svc = inject(ConventionService);

  readonly edition = signal<EditionDto | null>(null);
  readonly baseDocument = signal<Record<string, unknown> | null>(null);
  readonly json = signal('');
  readonly loading = signal(true);
  readonly exporting = signal(false);
  readonly error = signal<string | null>(null);
  readonly copied = signal(false);
  readonly includeScheduleDays = signal(true);
  readonly includeVenues = signal(true);
  readonly includeStaffAreas = signal(true);
  readonly includeStations = signal(true);
  readonly includeShifts = signal(true);
  readonly includeShiftStaffing = signal(false);
  readonly includeCategories = signal(true);
  readonly includeEvents = signal(true);
  readonly includeTicketTypes = signal(true);
  readonly includePages = signal(true);

  readonly canUseJson = computed(() => this.json().length > 0 && !this.exporting());

  ngOnInit(): void {
    this.route.paramMap.pipe(map(p => p.get('id')!)).subscribe(id => this.load(id));
  }

  onIncludeScheduleDaysChange(change: MatCheckboxChange): void {
    this.includeScheduleDays.set(change.checked);
    this.rebuildJson();
  }

  onIncludeVenuesChange(change: MatCheckboxChange): void {
    this.includeVenues.set(change.checked);
    this.rebuildJson();
  }

  onIncludeStaffAreasChange(change: MatCheckboxChange): void {
    this.includeStaffAreas.set(change.checked);
    this.rebuildJson();
  }

  onIncludeStationsChange(change: MatCheckboxChange): void {
    this.includeStations.set(change.checked);
    this.rebuildJson();
  }

  onIncludeShiftsChange(change: MatCheckboxChange): void {
    this.includeShifts.set(change.checked);
    this.rebuildJson();
  }

  onIncludeShiftStaffingChange(change: MatCheckboxChange): void {
    this.includeShiftStaffing.set(change.checked);
    this.rebuildJson();
  }

  onIncludeCategoriesChange(change: MatCheckboxChange): void {
    this.includeCategories.set(change.checked);
    this.rebuildJson();
  }

  onIncludeEventsChange(change: MatCheckboxChange): void {
    this.includeEvents.set(change.checked);
    this.rebuildJson();
  }

  onIncludeTicketTypesChange(change: MatCheckboxChange): void {
    this.includeTicketTypes.set(change.checked);
    this.rebuildJson();
  }

  onIncludePagesChange(change: MatCheckboxChange): void {
    this.includePages.set(change.checked);
    this.rebuildJson();
  }

  copyJson(): void {
    const value = this.json();
    if (!value) return;

    navigator.clipboard.writeText(value).then(() => {
      this.copied.set(true);
      window.setTimeout(() => this.copied.set(false), 1800);
    }).catch(() => {
      this.error.set('Kunde inte kopiera JSON till urklipp.');
    });
  }

  downloadJson(): void {
    const value = this.json();
    if (!value) return;

    const blob = new Blob([value], { type: 'application/json;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = this.fileName();
    link.click();
    URL.revokeObjectURL(url);
  }

  private load(editionId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.getEdition(editionId).subscribe({
      next: edition => {
        this.edition.set(edition);
        this.loading.set(false);
        this.reloadExport();
      },
      error: err => {
        this.error.set(toContextErrorMessage(err, 'Kunde inte hämta upplagan.'));
        this.loading.set(false);
      },
    });
  }

  private reloadExport(): void {
    const editionId = this.edition()?.id;
    if (!editionId) return;

    this.exporting.set(true);
    this.error.set(null);
    this.copied.set(false);
    this.svc.exportEdition(editionId, true, true, true).subscribe({
      next: json => {
        const parsed = this.parseDocument(json);
        if (!parsed) {
          this.baseDocument.set(null);
          this.json.set('');
          this.error.set('Kunde inte tolka exportdokumentet.');
          this.exporting.set(false);
          return;
        }

        this.baseDocument.set(parsed);
        this.rebuildJson();
        this.exporting.set(false);
      },
      error: err => {
        this.error.set(toContextErrorMessage(err, 'Kunde inte skapa exporten.'));
        this.exporting.set(false);
      },
    });
  }

  private rebuildJson(): void {
    const source = this.baseDocument();
    if (!source) {
      this.json.set('');
      return;
    }

    const filtered = this.buildFilteredDocument(source);
    this.json.set(JSON.stringify(filtered, null, 2));
  }

  private parseDocument(json: string): Record<string, unknown> | null {
    try {
      const parsed: unknown = JSON.parse(json);
      if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) {
        return null;
      }

      return parsed as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  private buildFilteredDocument(source: Record<string, unknown>): Record<string, unknown> {
    const document = JSON.parse(JSON.stringify(source)) as Record<string, unknown>;

    if (!this.includeScheduleDays()) {
      delete document['scheduleDays'];
    }

    if (!this.includeVenues()) {
      delete document['venues'];
    }

    if (!this.includeCategories()) {
      delete document['categories'];
    }

    if (!this.includeEvents()) {
      delete document['events'];
    }

    if (!this.includeTicketTypes()) {
      delete document['ticketTypes'];
    }

    if (!this.includePages()) {
      delete document['pages'];
    }

    if (!this.includeStaffAreas()) {
      delete document['staffAreas'];
      return document;
    }

    const areas = Array.isArray(document['staffAreas'])
      ? (document['staffAreas'] as Array<Record<string, unknown>>)
      : [];

    for (const area of areas) {
      const stations = Array.isArray(area['stations'])
        ? (area['stations'] as Array<Record<string, unknown>>)
        : [];

      if (!this.includeStations()) {
        area['stations'] = [];
        continue;
      }

      for (const station of stations) {
        const shifts = Array.isArray(station['shifts'])
          ? (station['shifts'] as Array<Record<string, unknown>>)
          : [];

        if (!this.includeShifts()) {
          station['shifts'] = [];
          continue;
        }

        if (!this.includeShiftStaffing()) {
          for (const shift of shifts) {
            delete shift['minPersons'];
            delete shift['maxPersons'];
          }
        }
      }
    }

    return document;
  }

  private fileName(): string {
    const slug = (this.edition()?.name ?? 'edition')
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9åäö]+/gi, '-')
      .replace(/^-+|-+$/g, '');

    return `${slug || 'edition'}-export.json`;
  }
}
