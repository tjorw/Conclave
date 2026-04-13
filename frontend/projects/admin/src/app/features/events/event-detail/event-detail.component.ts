import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CategoryDto, ConventionService, DateTimeRangeComponent, EditionDto, EventDto, EventService, VenueDto } from 'shared';
import { ChangeCategoryDialogComponent } from './change-category-dialog.component';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    DateTimeRangeComponent,
    MatTabsModule,
    MatTooltipModule,
  ],
  templateUrl: './event-detail.component.html',
  styleUrl: './event-detail.component.scss',
})
export class EventDetailComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly svc        = inject(EventService);
  private readonly conSvc     = inject(ConventionService);
  private readonly fb         = inject(FormBuilder);
  private readonly dialog     = inject(MatDialog);

  readonly event      = signal<EventDto | null>(null);
  readonly edition    = signal<EditionDto | null>(null);
  readonly venues     = signal<VenueDto[]>([]);
  readonly categories = signal<CategoryDto[]>([]);
  readonly loading    = signal(true);
  readonly saving     = signal(false);
  readonly error      = signal<string | null>(null);
  readonly showRejectForm        = signal(false);
  readonly showAddRequestForm    = signal(false);
  readonly showAddSessionForm    = signal(false);
  readonly editingSessionId      = signal<string | null>(null);

  readonly rejectForm = this.fb.group({
    comment: ['', [Validators.required, Validators.minLength(5)]],
  });

  readonly editForm = this.fb.group({
    title:            ['', Validators.required],
    description:      ['', Validators.required],
    registrationType: ['DropIn', Validators.required],
    dropInRules:      [''],
  });

  readonly addRequestForm = this.fb.group({
    description:     ['', Validators.required],
    durationMinutes: [60, [Validators.required, Validators.min(1)]],
    seats:           [20, [Validators.required, Validators.min(1)]],
    startType:       ['FixedTime', Validators.required],
  });

  readonly sessionForm = this.fb.group({
    venueId:   ['', Validators.required],
    startTime: ['', Validators.required],
    endTime:   ['', Validators.required],
    maxSeats:  [20, [Validators.required, Validators.min(1)]],
    startType: ['FixedTime', Validators.required],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('eventId')!;
    this.svc.getEvent(id).subscribe({
      next: e => {
        this.event.set(e);
        this.loading.set(false);
        this.populateEditForm(e);
        this.conSvc.getEdition(e.editionId).subscribe({
          next: ed => {
            this.edition.set(ed);
            this.venues.set(ed.venues);
            this.categories.set(ed.categories);
          },
        });
      },
      error: () => { this.error.set('Kunde inte hämta evenemanget.'); this.loading.set(false); },
    });
  }

  // ── Category ────────────────────────────────────────────────────────────

  openChangeCategoryDialog(): void {
    const ev = this.event();
    if (!ev) return;
    const ref = this.dialog.open(ChangeCategoryDialogComponent, {
      width: '380px',
      data: { currentCategoryId: ev.categoryId, categories: this.categories() },
    });
    ref.afterClosed().subscribe((newCategoryId: string | undefined) => {
      if (!newCategoryId || newCategoryId === ev.categoryId) return;
      this.saving.set(true);
      this.svc.changeCategory(ev.id, newCategoryId).subscribe({
        next: () => { this.saving.set(false); this.reload(); },
        error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte byta kategori.'); },
      });
    });
  }

  // ── Approve / Reject ────────────────────────────────────────────────────

  approve(): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.approveEvent(ev.id).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte godkänna evenemanget.'); },
    });
  }

  openRejectForm(): void { this.showRejectForm.set(true); this.rejectForm.reset(); }
  cancelReject(): void   { this.showRejectForm.set(false); }

  submitReject(): void {
    const ev = this.event();
    if (!ev || this.rejectForm.invalid || this.saving()) return;
    const comment = this.rejectForm.getRawValue().comment!;
    this.saving.set(true);
    this.svc.rejectEvent(ev.id, comment).subscribe({
      next: () => { this.saving.set(false); this.showRejectForm.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte avvisa evenemanget.'); },
    });
  }

  // ── Cancel ──────────────────────────────────────────────────────────────

  cancelEvent(): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.cancelEvent(ev.id).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte ställa in evenemanget.'); },
    });
  }

  // ── Edit draft ──────────────────────────────────────────────────────────

  saveEdit(): void {
    const ev = this.event();
    if (!ev || this.editForm.invalid || this.saving()) return;
    const { title, description, registrationType, dropInRules } = this.editForm.getRawValue();
    this.saving.set(true);
    this.svc.updateDraft(ev.id, title!, description!, registrationType!, dropInRules || null).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte spara utkastet.'); },
    });
  }

  // ── Session requests ────────────────────────────────────────────────────

  toggleAddRequestForm(): void {
    this.showAddRequestForm.update(v => !v);
    if (!this.showAddRequestForm()) this.addRequestForm.reset({ durationMinutes: 60, seats: 20, startType: 'Scheduled' });
  }

  addSessionRequest(): void {
    const ev = this.event();
    if (!ev || this.addRequestForm.invalid || this.saving()) return;
    const { description, durationMinutes, seats, startType } = this.addRequestForm.getRawValue();
    this.saving.set(true);
    this.svc.addSessionRequest(ev.id, description!, durationMinutes!, seats!, startType!).subscribe({
      next: () => { this.saving.set(false); this.showAddRequestForm.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte lägga till sessionönskemål.'); },
    });
  }

  removeSessionRequest(requestId: string): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.removeSessionRequest(ev.id, requestId).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte ta bort sessionönskemål.'); },
    });
  }

  // ── Sessions ────────────────────────────────────────────────────────────

  toggleAddSessionForm(): void {
    this.showAddSessionForm.update(v => !v);
    this.editingSessionId.set(null);
    if (!this.showAddSessionForm()) this.sessionForm.reset({ maxSeats: 20, startType: 'FixedTime' });
  }

  startEditSession(sessionId: string): void {
    const ev = this.event();
    if (!ev) return;
    const session = ev.sessions.find(s => s.id === sessionId);
    if (!session) return;
    this.editingSessionId.set(sessionId);
    this.showAddSessionForm.set(false);
    this.sessionForm.patchValue({
      venueId:   session.venueId,
      startTime: session.start.slice(0, 16),
      endTime:   session.end.slice(0, 16),
      maxSeats:  session.maxSeats,
      startType: session.startType,
    });
  }

  cancelSessionEdit(): void {
    this.editingSessionId.set(null);
    this.sessionForm.reset({ maxSeats: 20, startType: 'FixedTime' });
  }

  scheduleSession(): void {
    const ev = this.event();
    if (!ev || this.sessionForm.invalid || this.saving()) return;
    const { venueId, startTime, endTime, maxSeats, startType } = this.sessionForm.getRawValue();
    this.saving.set(true);
    this.svc.scheduleSession(ev.id, venueId!, startTime!, endTime!, maxSeats!, startType!).subscribe({
      next: () => { this.saving.set(false); this.showAddSessionForm.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte schemalägga sessionen.'); },
    });
  }

  saveSessionEdit(): void {
    const ev = this.event();
    const sessionId = this.editingSessionId();
    if (!ev || !sessionId || this.sessionForm.invalid || this.saving()) return;
    const { venueId, startTime, endTime, maxSeats, startType } = this.sessionForm.getRawValue();
    this.saving.set(true);
    this.svc.updateSession(ev.id, sessionId, venueId!, startTime!, endTime!, maxSeats!, startType!).subscribe({
      next: () => { this.saving.set(false); this.editingSessionId.set(null); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte spara sessionen.'); },
    });
  }

  deactivateSession(sessionId: string): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.deactivateSession(ev.id, sessionId).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte inaktivera sessionen.'); },
    });
  }

  // ── Lifecycle ───────────────────────────────────────────────────────────

  returnToDraft(): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.returnToDraft(ev.id).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte återställa evenemanget till utkast.'); },
    });
  }

  submitForReview(): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.submitForReview(ev.id).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.detail ?? 'Kunde inte skicka in evenemanget för granskning.'); },
    });
  }

  // ── Helpers ─────────────────────────────────────────────────────────────

  private reload(): void {
    const id = this.route.snapshot.paramMap.get('eventId')!;
    this.svc.getEvent(id).subscribe({
      next: e => { this.event.set(e); this.populateEditForm(e); },
    });
  }

  private populateEditForm(e: EventDto): void {
    this.editForm.patchValue({
      title:            e.title ?? '',
      description:      e.description ?? '',
      registrationType: e.registrationType,
      dropInRules:      e.dropInRules ?? '',
    });
  }

  statusLabel(status: string): string {
    const map: Record<string, string> = {
      Draft: 'Utkast', UnderReview: 'Under granskning',
      Published: 'Publicerat', Cancelled: 'Inställt',
    };
    return map[status] ?? status;
  }

  registrationLabel(type: string): string {
    const map: Record<string, string> = {
      DropIn: 'Drop-in', PreRegistration: 'Föranmälan', Combined: 'Kombinerat',
    };
    return map[type] ?? type;
  }

  startTypeLabel(type: string): string {
    const map: Record<string, string> = {
      FixedTime: 'Fast tid', Rolling: 'Löpande', Tournament: 'Turneringsformat',
    };
    return map[type] ?? type;
  }

  sessionStatusLabel(status: string): string {
    const map: Record<string, string> = { Active: 'Aktiv', Inactive: 'Inaktiv' };
    return map[status] ?? status;
  }

  readonly sortedSessions = computed(() =>
    [...(this.event()?.sessions ?? [])].sort((a, b) => (a.start < b.start ? 1 : -1))
  );

  get sessionMin(): string | undefined { return this.edition()?.start.slice(0, 16); }
  get sessionMax(): string | undefined { return this.edition()?.end.slice(0, 16); }

  venueName(venueId: string): string {
    return this.venues().find(v => v.id === venueId)?.name ?? venueId;
  }
}
