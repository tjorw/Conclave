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
  readonly json = signal('');
  readonly loading = signal(true);
  readonly exporting = signal(false);
  readonly error = signal<string | null>(null);
  readonly copied = signal(false);
  readonly includeEvents = signal(true);
  readonly includeTicketTypes = signal(true);

  readonly canUseJson = computed(() => this.json().length > 0 && !this.exporting());

  ngOnInit(): void {
    this.route.paramMap.pipe(map(p => p.get('id')!)).subscribe(id => this.load(id));
  }

  onIncludeEventsChange(change: MatCheckboxChange): void {
    this.includeEvents.set(change.checked);
    this.reloadExport();
  }

  onIncludeTicketTypesChange(change: MatCheckboxChange): void {
    this.includeTicketTypes.set(change.checked);
    this.reloadExport();
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
    this.svc.exportEdition(editionId, this.includeEvents(), this.includeTicketTypes()).subscribe({
      next: json => {
        this.json.set(json);
        this.exporting.set(false);
      },
      error: err => {
        this.error.set(toContextErrorMessage(err, 'Kunde inte skapa exporten.'));
        this.exporting.set(false);
      },
    });
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
