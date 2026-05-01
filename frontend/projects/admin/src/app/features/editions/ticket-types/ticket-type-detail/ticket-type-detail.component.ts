import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { combineLatest, map } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import {
  ConventionService,
  createAsyncState,
  EditionDto,
  MarkdownEditorComponent,
  RegistrationService,
  TicketTypeAdminDto,
  toContextErrorMessage,
} from 'shared';
import { ERROR } from '../../../../labels/errors.labels';
import { FIELD } from '../../../../labels/ui.labels';
import { EDITION_DETAIL } from '../../../../labels/pages.labels';
import { ConfirmDialogService } from '../../../../shared/confirm-dialog/confirm-dialog.service';
import { HelpTooltipComponent } from '../../../../../help/components/help-tooltip/help-tooltip.component';

@Component({
  selector: 'app-ticket-type-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MarkdownEditorComponent,
    HelpTooltipComponent,
  ],
  templateUrl: './ticket-type-detail.component.html',
  styleUrl: './ticket-type-detail.component.scss',
})
export class TicketTypeDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly svc = inject(ConventionService);
  private readonly regSvc = inject(RegistrationService);
  private readonly confirmSvc = inject(ConfirmDialogService);

  readonly edition = signal<EditionDto | null>(null);
  readonly ticketTypes = signal<TicketTypeAdminDto[]>([]);
  protected readonly state = createAsyncState(true);

  private editionId = '';
  private ticketTypeId = '';

  readonly isNew = computed(() => this.ticketTypeId === 'new');

  readonly FIELD = FIELD;
  readonly PAGE = EDITION_DETAIL;

  readonly ticketTypeCategories = [
    { value: 'Visitor', label: 'Besökare' },
    { value: 'Organiser', label: 'Arrangör' },
    { value: 'Staff', label: 'Funktionär' },
  ];

  readonly form = this.fb.group({
    name: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0)]],
    category: ['Visitor', Validators.required],
    validDays: this.fb.control<string[]>([], { nonNullable: true }),
    allowedCategories: this.fb.control<string[]>([], { nonNullable: true }),
    description: [''],
  });

  readonly editionDayOptions = computed(() => {
    const e = this.edition();
    if (!e) return [] as { value: string; label: string }[];
    const start = new Date(`${e.start.substring(0, 10)}T00:00:00`);
    const end = new Date(`${e.end.substring(0, 10)}T00:00:00`);
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || start > end) {
      return [] as { value: string; label: string }[];
    }
    const options: { value: string; label: string }[] = [];
    for (const cur = new Date(start); cur <= end; cur.setDate(cur.getDate() + 1)) {
      const value = `${cur.getFullYear()}-${String(cur.getMonth() + 1).padStart(2, '0')}-${String(cur.getDate()).padStart(2, '0')}`;
      const label = new Intl.DateTimeFormat('sv-SE', {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
      }).format(cur);
      options.push({ value, label });
    }
    return options;
  });

  ngOnInit(): void {
    combineLatest([
      this.route.paramMap.pipe(map((p) => p.get('id')!)),
      this.route.paramMap.pipe(map((p) => p.get('ticketTypeId')!)),
    ]).subscribe(([editionId, ticketTypeId]) => {
      this.editionId = editionId;
      this.ticketTypeId = ticketTypeId;
      this.loadData();
    });
  }

  private loadData(): void {
    this.state.loading.set(true);
    this.svc.getEdition(this.editionId).subscribe({
      next: (e) => {
        this.edition.set(e);
        this.checkTicketType();
      },
      error: () => {
        this.state.error.set(ERROR.fetchEdition);
        this.state.loading.set(false);
      },
    });
    this.regSvc.listTicketTypes(this.editionId).subscribe({
      next: (tt) => {
        this.ticketTypes.set(tt);
        this.checkTicketType();
      },
    });
  }

  private checkTicketType(): void {
    if (!this.edition() || (this.ticketTypes().length === 0 && !this.isNew())) return;
    if (!this.isNew()) {
      const tt = this.ticketTypes().find((t) => t.id === this.ticketTypeId);
      if (tt) {
        this.form.setValue({
          name: tt.name,
          price: tt.price / 100,
          category: tt.category,
          validDays: tt.validDays ?? [],
          allowedCategories: tt.allowedCategories ?? [],
          description: tt.description ?? '',
        });
      } else if (this.edition()) {
        this.state.error.set('Biljetttypen hittades inte.');
      }
    }
    this.state.loading.set(false);
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    const payload = {
      name: v.name!,
      price: Math.round((v.price ?? 0) * 100),
      category: v.category!,
      validDays: this.normalize(v.validDays),
      allowedCategories: this.normalize(v.allowedCategories),
      description: this.normalizeText(v.description),
    };
    this.state.saving.set(true);
    const onError = (err: unknown, label: string) => {
      this.state.error.set(toContextErrorMessage(err, label));
      this.state.saving.set(false);
    };
    if (this.isNew()) {
      this.regSvc.createTicketType(this.editionId, payload).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => onError(err, ERROR.createTicketType),
      });
    } else {
      this.regSvc.updateTicketType(this.editionId, this.ticketTypeId, payload).subscribe({
        next: () => this.navigateBack(),
        error: (err: unknown) => onError(err, ERROR.updateTicketType),
      });
    }
  }

  delete(): void {
    const ticketType = this.ticketTypes().find((t) => t.id === this.ticketTypeId);
    if (!ticketType) return;

    this.confirmSvc.confirm({
      title: this.PAGE.deleteTicketTypeTitle,
      message: this.PAGE.deleteTicketTypeMessage(ticketType.name),
    }).subscribe((confirmed) => {
        if (!confirmed) return;
        this.state.saving.set(true);
        this.regSvc.deleteTicketType(this.editionId, this.ticketTypeId).subscribe({
          next: () => this.navigateBack(),
          error: (err: unknown) => {
            this.state.error.set(toContextErrorMessage(err, ERROR.deleteTicketType));
            this.state.saving.set(false);
          },
        });
      });
  }

  private normalize(value: string[] | null | undefined): string[] | null {
    if (!value || value.length === 0) return null;
    return [...new Set(value)];
  }

  private normalizeText(value: string | null | undefined): string | null {
    const normalized = value?.trim();
    return normalized ? normalized : null;
  }

  navigateBack(): void {
    void this.router.navigate(['/editions', this.editionId, 'ticket-types']);
  }
}
