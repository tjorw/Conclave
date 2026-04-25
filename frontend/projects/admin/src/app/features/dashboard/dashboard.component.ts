import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ConventionDto, ConventionService, ImportWarningDto, PersonDto, EVENT_STATUS_LABEL, toErrorMessage } from 'shared';
import { EditionContextService } from '../../services/edition-context.service';
import { ERROR } from '../../labels/errors.labels';
import { ACTION, CHIP, FIELD, PLACEHOLDER } from '../../labels/ui.labels';
import { nextSort, sortBy, sortIcon, SortState } from '../../shared/sort-utils';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../shared/confirm-dialog/confirm-dialog.component';

type EditionSortKey = 'name' | 'start' | 'end' | 'status';
type CreateMode = 'manual' | 'import';

interface ExportDocumentPreview {
  schemaVersion: number | null;
  durationDays: number | null;
  venues: number;
  staffAreas: number;
  stations: number;
  shifts: number;
  categories: number;
  events: number;
  sessions: number;
  ticketTypes: number;
}

interface ImportWarningsDialogData {
  editionName: string;
  warnings: ImportWarningDto[];
}

@Component({
  selector: 'app-import-warnings-dialog',
  standalone: true,
  imports: [MatButtonModule, MatDialogModule, MatIconModule, MatListModule],
  template: `
    <h2 mat-dialog-title>Import klar</h2>
    <mat-dialog-content>
      @if (data.warnings.length === 0) {
        <div class="dialog-ok">
          <mat-icon>check_circle</mat-icon>
          <span>{{ data.editionName }} skapades utan varningar.</span>
        </div>
      } @else {
        <p class="dialog-summary">{{ data.editionName }} skapades med {{ data.warnings.length }} varningar.</p>
        <mat-list>
          @for (warning of data.warnings; track warning.code + warning.message) {
            <mat-list-item>
              <mat-icon matListItemIcon>warning</mat-icon>
              <span matListItemTitle>{{ warning.code }}</span>
              <span matListItemLine>{{ warning.message }}</span>
            </mat-list-item>
          }
        </mat-list>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-flat-button color="primary" type="button" (click)="close()">Öppna upplagan</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dialog-ok {
      display: flex;
      align-items: center;
      gap: 10px;
      color: #2e7d32;
      padding: 8px 0;
    }

    .dialog-summary {
      margin: 0 0 8px;
      color: #555;
    }

    mat-list {
      max-height: 360px;
      overflow: auto;
    }
  `],
})
export class ImportWarningsDialogComponent {
  readonly data = inject<ImportWarningsDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ImportWarningsDialogComponent>);

  close(): void {
    this.dialogRef.close();
  }
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatChipsModule,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly conventionService = inject(ConventionService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);
  readonly editionContext = inject(EditionContextService);

  readonly ACTION      = ACTION;
  readonly CHIP        = CHIP;
  readonly FIELD       = FIELD;
  readonly PLACEHOLDER = PLACEHOLDER;

  readonly convention = signal<ConventionDto | null>(null);
  readonly persons = signal<PersonDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly showCreateForm = signal(false);
  readonly createMode = signal<CreateMode>('manual');
  readonly importJsonText = signal('');
  readonly editionSort = signal<SortState<EditionSortKey>>({ key: 'start', direction: 'desc' });

  readonly createForm = this.fb.group({
    name: ['', Validators.required],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    staffCoordinatorId: ['', Validators.required],
    eventCoordinatorId: ['', Validators.required],
  });

  readonly importForm = this.fb.group({
    name: ['', Validators.required],
    startDate: ['', Validators.required],
    json: ['', Validators.required],
  });

  readonly importParseResult = computed(() => {
    const json = this.importJsonText().trim();
    if (!json) return { document: null as unknown, error: null as string | null, preview: null as ExportDocumentPreview | null };

    try {
      const document = JSON.parse(json) as Record<string, unknown>;
      return {
        document,
        error: null,
        preview: this.buildPreview(document),
      };
    } catch {
      return {
        document: null,
        error: 'JSON-dokumentet kunde inte tolkas.',
        preview: null,
      };
    }
  });

  readonly sortedEditions = computed(() =>
    sortBy(this.editionContext.editions(), this.editionSort(), {
      name: edition => edition.name,
      start: edition => edition.start,
      end: edition => edition.end,
      status: edition => this.statusLabel(edition.status),
    })
  );

  ngOnInit(): void {
    this.conventionService.getCurrentConvention().subscribe({
      next: c => { this.convention.set(c); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchDashboard); this.loading.set(false); },
    });
    this.conventionService.listPersons().subscribe({
      next: p => this.persons.set(p.filter(x => x.isActive)),
    });
  }

  openEdition(id: string): void {
    this.editionContext.setActive(id);
    this.router.navigate(['/editions', id]);
  }

  setEditionSort(key: EditionSortKey): void {
    this.editionSort.set(nextSort(this.editionSort(), key));
  }

  editionSortIcon(key: EditionSortKey): string {
    return sortIcon(this.editionSort(), key);
  }

  toggleCreateForm(): void {
    this.showCreateForm.update(open => !open);
    if (!this.showCreateForm()) {
      this.createForm.reset();
      this.importForm.reset();
      this.importJsonText.set('');
      this.createMode.set('manual');
    }
  }

  setCreateMode(mode: CreateMode): void {
    this.createMode.set(mode);
  }

  onImportJsonInput(value: string): void {
    this.importJsonText.set(value);
  }

  create(): void {
    if (this.createForm.invalid) return;
    const v = this.createForm.value;
    this.saving.set(true);
    this.conventionService.createEdition({
      name: v.name!,
      startDate: v.startDate!,
      endDate: v.endDate!,
      staffCoordinatorId: v.staffCoordinatorId!,
      eventCoordinatorId: v.eventCoordinatorId!,
    }).subscribe({
      next: ({ id }) => {
        this.editionContext.reload();
        this.createForm.reset();
        this.showCreateForm.set(false);
        this.saving.set(false);
        this.router.navigate(['/editions', id]);
      },
      error: err => {
        this.error.set(toErrorMessage(err, ERROR.createEdition));
        this.saving.set(false);
      },
    });
  }

  removeEdition(edition: { id: string; name: string }, event: MouseEvent): void {
    event.stopPropagation();

    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: {
          title: 'Ta bort upplaga',
          message: `Vill du ta bort ${edition.name}? All struktur och kopplad data för upplagan tas bort.`,
          confirmLabel: 'Ta bort',
        },
        width: '420px',
      })
      .afterClosed()
      .subscribe(confirmed => {
        if (!confirmed) return;

        this.saving.set(true);
        this.conventionService.removeEdition(edition.id).subscribe({
          next: () => {
            this.editionContext.reload();
            this.saving.set(false);
          },
          error: err => {
            this.error.set(toErrorMessage(err, 'Kunde inte ta bort upplagan.'));
            this.saving.set(false);
          },
        });
      });
  }

  canRemoveEdition(editionId: string): boolean {
    return this.editionContext.activeEdition()?.id !== editionId;
  }

  importEdition(): void {
    if (this.importForm.invalid) return;

    const parsed = this.importParseResult();
    if (parsed.error || !parsed.document) {
      this.error.set(parsed.error ?? 'JSON-dokumentet kunde inte tolkas.');
      return;
    }

    const value = this.importForm.value;
    this.saving.set(true);
    this.conventionService.importEdition(value.name!, value.startDate!, parsed.document).subscribe({
      next: result => {
        this.editionContext.reload();
        this.importForm.reset();
        this.importJsonText.set('');
        this.createForm.reset();
        this.showCreateForm.set(false);
        this.saving.set(false);

        this.dialog
          .open<ImportWarningsDialogComponent, ImportWarningsDialogData>(ImportWarningsDialogComponent, {
            data: {
              editionName: value.name!,
              warnings: result.warnings ?? [],
            },
            width: '640px',
          })
          .afterClosed()
          .subscribe(() => {
            this.editionContext.setActive(result.editionId);
            this.router.navigate(['/editions', result.editionId]);
          });
      },
      error: err => {
        this.error.set(toErrorMessage(err, 'Kunde inte importera upplagan.'));
        this.saving.set(false);
      },
    });
  }

  statusLabel(status: string): string {
    return EVENT_STATUS_LABEL[status] ?? status;
  }

  statusColor(status: string): string {
    return status === 'Published' ? 'primary' : 'default';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('sv-SE');
  }

  private buildPreview(document: Record<string, unknown>): ExportDocumentPreview {
    const staffAreas = this.asArray(document['staffAreas']);
    const stations = staffAreas.flatMap(area => this.asArray(this.asRecord(area)?.['stations']));
    const shifts = stations.flatMap(station => this.asArray(this.asRecord(station)?.['shifts']));
    const events = this.asArray(document['events']);
    const sessions = events.flatMap(event => this.asArray(this.asRecord(event)?.['sessions']));

    return {
      schemaVersion: this.asNumber(document['schemaVersion']),
      durationDays: this.asNumber(document['durationDays']),
      venues: this.asArray(document['venues']).length,
      staffAreas: staffAreas.length,
      stations: stations.length,
      shifts: shifts.length,
      categories: this.asArray(document['categories']).length,
      events: events.length,
      sessions: sessions.length,
      ticketTypes: this.asArray(document['ticketTypes']).length,
    };
  }

  private asArray(value: unknown): unknown[] {
    return Array.isArray(value) ? value : [];
  }

  private asRecord(value: unknown): Record<string, unknown> | null {
    return value !== null && typeof value === 'object' && !Array.isArray(value)
      ? value as Record<string, unknown>
      : null;
  }

  private asNumber(value: unknown): number | null {
    return typeof value === 'number' ? value : null;
  }
}
