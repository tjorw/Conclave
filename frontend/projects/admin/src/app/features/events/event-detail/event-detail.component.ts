import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
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
import {
  CategoryDto, ConventionService, DateTimeRangeComponent, EditionDto, EditionSessionDto, EventDto, EventService, VenueDto,
  EVENT_COMMENT_STATUS_LABEL, EVENT_STATUS_LABEL, REGISTRATION_KIND_LABEL, START_TYPE_LABEL, SESSION_STATUS_LABEL,
  MarkdownEditorComponent,
  OrganiserTicketAssignmentDto,
  OrganiserTicketTypeDto,
  RegistrationService,
  toErrorMessage,
} from 'shared';
import { ChangeCategoryDialogComponent } from './change-category-dialog.component';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { DraftBlock, SessionTimelineComponent } from '../../../shared/session-timeline/session-timeline.component';
import { ERROR } from '../../../labels/errors.labels';
import { EVENT_DETAIL } from '../../../labels/pages.labels';
import { ACTION, FIELD, TOOLTIP } from '../../../labels/ui.labels';
import { nextSort, sortBy, sortIcon, SortState } from '../../../shared/sort-utils';

type EventSessionSortKey = 'start' | 'end' | 'venue' | 'seats' | 'startType' | 'status';

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
    MarkdownEditorComponent,
    SessionTimelineComponent,
  ],
  templateUrl: './event-detail.component.html',
  styleUrl: './event-detail.component.scss',
})
export class EventDetailComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly svc        = inject(EventService);
  private readonly conSvc     = inject(ConventionService);
  private readonly regSvc     = inject(RegistrationService);
  private readonly fb         = inject(FormBuilder);
  private readonly dialog     = inject(MatDialog);

  private openConfirm(data: ConfirmDialogData) {
    return this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, { data, width: '400px' })
      .afterClosed()
      .pipe(map(result => result === true));
  }

  readonly ACTION        = ACTION;
  readonly TOOLTIP       = TOOLTIP;
  readonly FIELD         = FIELD;
  readonly PAGE          = EVENT_DETAIL;
  readonly registrationTypes = (Object.entries(REGISTRATION_KIND_LABEL) as [string, string][]).map(([value, label]) => ({ value, label }));
  readonly startTypes        = (Object.entries(START_TYPE_LABEL) as [string, string][]).map(([value, label]) => ({ value, label }));

  readonly event      = signal<EventDto | null>(null);
  readonly edition    = signal<EditionDto | null>(null);
  readonly venues     = signal<VenueDto[]>([]);
  readonly categories = signal<CategoryDto[]>([]);
  readonly loading    = signal(true);
  readonly saving     = signal(false);
  readonly deleting   = signal(false);
  readonly error      = signal<string | null>(null);
  readonly showRejectForm        = signal(false);
  readonly showAddSessionForm    = signal(false);
  readonly editingSessionId      = signal<string | null>(null);
  readonly commentResponses      = signal<Record<string, string>>({});
  readonly coOrganiserReviewComments = signal<Record<string, string>>({});
  readonly showTimeline          = signal(false);
  readonly editionSessions       = signal<EditionSessionDto[]>([]);
  readonly timelineLoading       = signal(false);
  private readonly timelineLoaded = signal(false);
  readonly sessionSort = signal<SortState<EventSessionSortKey>>({ key: 'start', direction: 'desc' });
  readonly organiserTicketTypes = signal<OrganiserTicketTypeDto[]>([]);
  readonly organiserTicketAssignments = signal<OrganiserTicketAssignmentDto[]>([]);
  readonly organiserTicketSelection = signal<Record<string, string | null>>({});

  readonly rejectForm = this.fb.group({
    comment: ['', [Validators.required, Validators.minLength(5)]],
  });

  readonly editForm = this.fb.group({
    title:            ['', Validators.required],
    description:      ['', Validators.required],
    registrationType: ['DropIn', Validators.required],
    dropInRules:      [''],
    scheduleRequestText: [''],
  });

  readonly sessionForm = this.fb.group({
    venueId:   ['', Validators.required],
    startTime: ['', Validators.required],
    endTime:   ['', Validators.required],
    maxSeats:  [20, [Validators.required, Validators.min(1)]],
    startType: ['FixedTime', Validators.required],
  });

  private readonly sessionFormValues = toSignal(this.sessionForm.valueChanges, {
    initialValue: this.sessionForm.value,
  });

  readonly timelineVenueId = computed(() => this.sessionFormValues()?.venueId ?? null);

  readonly timelineDraft = computed<DraftBlock | null>(() => {
    // Visa bara ett draft-block när formuläret faktiskt är öppet
    if (!this.showAddSessionForm() && !this.editingSessionId()) return null;
    const v = this.sessionFormValues();
    if (!v?.startTime || !v?.endTime) return null;
    return {
      start:     v.startTime,
      end:       v.endTime,
      sessionId: this.editingSessionId() ?? undefined,
      eventTitle: this.event()?.title ?? undefined,
      venueName:  v.venueId ? this.venueName(v.venueId) : undefined,
    };
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('eventId')!;
    this.svc.getEvent(id).subscribe({
      next: e => {
        this.event.set(e);
        this.loading.set(false);
        this.populateEditForm(e);
        this.loadOrganiserTicketState(e);
        this.conSvc.getEdition(e.editionId).subscribe({
          next: ed => {
            this.edition.set(ed);
            this.venues.set(ed.venues);
            this.categories.set(ed.categories);
          },
        });
      },
      error: () => { this.error.set(ERROR.fetchEvent); this.loading.set(false); },
    });
  }

  readonly pendingComments = computed(() =>
    (this.event()?.comments ?? []).filter(c => c.requiresHandling && c.status !== 'Responded' && c.status !== 'Acknowledged')
  );

  readonly coOrganiserApplications = computed(() =>
    [...(this.event()?.coOrganiserApplications ?? [])].sort((a, b) => {
      if (a.status === 'Pending' && b.status !== 'Pending') return -1;
      if (a.status !== 'Pending' && b.status === 'Pending') return 1;
      return new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime();
    })
  );

  readonly commentStatusLabel = EVENT_COMMENT_STATUS_LABEL;

  readonly eventOrganisers = computed(() => {
    const ev = this.event();
    if (!ev) return [];

    return [
      {
        personId: ev.leadOrganiserId,
        personName: ev.leadOrganiserName,
        role: 'Huvudarrangör',
      },
      ...(ev.coOrganisers ?? []).map(co => ({
        personId: co.personId,
        personName: co.personName,
        role: 'Medarrangör',
      })),
    ];
  });

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
        error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.changeCategory)); },
      });
    });
  }

  // ── Approve / Reject ────────────────────────────────────────────────────

  approve(): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.approveEvent(ev.id, this.approvalTicketAssignments()).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.approveEvent)); },
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
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.rejectEvent)); },
    });
  }

  // ── Cancel ──────────────────────────────────────────────────────────────

  cancelEvent(): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.cancelEvent(ev.id).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.cancelEvent)); },
    });
  }

  // ── Delete ───────────────────────────────────────────────────────────────

  deleteEvent(): void {
    const ev = this.event();
    if (!ev || this.deleting()) return;
    this.openConfirm({
      title:   this.PAGE.deleteTitle,
      message: this.PAGE.deleteMessage(ev.title || this.PAGE.noName),
    }).subscribe(confirmed => {
      if (!confirmed) return;
      this.deleting.set(true);
      this.svc.deleteEvent(ev.id).subscribe({
        next: () => this.router.navigate(['/events']),
        error: err => { this.deleting.set(false); this.error.set(toErrorMessage(err, ERROR.deleteEvent)); },
      });
    });
  }

  // ── Edit draft ──────────────────────────────────────────────────────────

  saveEdit(): void {
    const ev = this.event();
    if (!ev || this.editForm.invalid || this.saving()) return;
    const { title, description, registrationType, dropInRules, scheduleRequestText } = this.editForm.getRawValue();
    this.saving.set(true);
    this.svc.updateDraft(ev.id, title!, description!, registrationType!, dropInRules || null, scheduleRequestText || null).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.saveDraft)); },
    });
  }

  // ── Session requests ────────────────────────────────────────────────────

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
      next: () => { this.saving.set(false); this.showAddSessionForm.set(false); this.reload(); this.refreshEditionSessions(); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.scheduleSession)); },
    });
  }

  saveSessionEdit(): void {
    const ev = this.event();
    const sessionId = this.editingSessionId();
    if (!ev || !sessionId || this.sessionForm.invalid || this.saving()) return;
    const { venueId, startTime, endTime, maxSeats, startType } = this.sessionForm.getRawValue();
    this.saving.set(true);
    this.svc.updateSession(ev.id, sessionId, venueId!, startTime!, endTime!, maxSeats!, startType!).subscribe({
      next: () => { this.saving.set(false); this.editingSessionId.set(null); this.reload(); this.refreshEditionSessions(); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.saveSession)); },
    });
  }

  deactivateSession(sessionId: string): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.deactivateSession(ev.id, sessionId).subscribe({
      next: () => { this.saving.set(false); this.reload(); this.refreshEditionSessions(); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.deactivateSession)); },
    });
  }

  // ── Lifecycle ───────────────────────────────────────────────────────────

  returnToDraft(): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.returnToDraft(ev.id).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.returnToDraft)); },
    });
  }

  submitForReview(): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    this.saving.set(true);
    this.svc.submitForReview(ev.id).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.submitForReview)); },
    });
  }

  // ── Timeline ─────────────────────────────────────────────────────────────

  toggleTimeline(): void {
    this.showTimeline.update(v => !v);
    if (this.showTimeline() && !this.timelineLoaded()) {
      this.loadEditionSessions();
    }
  }

  private loadEditionSessions(): void {
    const ev = this.event();
    if (!ev) return;
    this.timelineLoading.set(true);
    this.svc.getEditionSessions(ev.editionId).subscribe({
      next: sessions => {
        this.editionSessions.set(sessions);
        this.timelineLoading.set(false);
        this.timelineLoaded.set(true);
      },
      error: () => this.timelineLoading.set(false),
    });
  }

  private refreshEditionSessions(): void {
    if (!this.timelineLoaded()) return;
    const ev = this.event();
    if (!ev) return;
    this.svc.getEditionSessions(ev.editionId).subscribe({
      next: sessions => this.editionSessions.set(sessions),
    });
  }

  // ── Helpers ─────────────────────────────────────────────────────────────

  private reload(): void {
    const id = this.route.snapshot.paramMap.get('eventId')!;
    this.svc.getEvent(id).subscribe({
      next: e => { this.event.set(e); this.populateEditForm(e); this.loadOrganiserTicketState(e); },
    });
  }

  private loadOrganiserTicketState(e: EventDto): void {
    this.regSvc.getOrganiserTicketTypes(e.editionId).subscribe({
      next: ticketTypes => this.organiserTicketTypes.set(ticketTypes),
      error: () => this.organiserTicketTypes.set([]),
    });

    this.regSvc.getEventOrganiserTicketAssignments(e.id).subscribe({
      next: assignments => {
        this.organiserTicketAssignments.set(assignments);
        this.organiserTicketSelection.set(
          Object.fromEntries(this.eventOrganisers().map(organiser => {
            const current = assignments.find(a => a.personId === organiser.personId);
            return [organiser.personId, current?.ticketTypeId ?? null];
          }))
        );
      },
      error: () => {
        this.organiserTicketAssignments.set([]);
        this.organiserTicketSelection.set(
          Object.fromEntries(this.eventOrganisers().map(organiser => [organiser.personId, null]))
        );
      },
    });
  }

  private approvalTicketAssignments(): { personId: string; ticketTypeId: string | null }[] {
    const selection = this.organiserTicketSelection();
    return this.eventOrganisers().map(organiser => ({
      personId: organiser.personId,
      ticketTypeId: selection[organiser.personId] ?? null,
    }));
  }

  setOrganiserTicketSelection(personId: string, ticketTypeId: string | null): void {
    this.organiserTicketSelection.update(selection => ({ ...selection, [personId]: ticketTypeId }));
  }

  currentOrganiserTicketLabel(personId: string): string {
    const assignment = this.organiserTicketAssignments().find(a => a.personId === personId);
    return assignment?.ticketTypeName ?? 'Ingen aktiv arrangörsbiljett';
  }

  ticketTypePriceLabel(price: number): string {
    if (price === 0) return 'Kostnadsfri';

    return new Intl.NumberFormat('sv-SE', {
      style: 'currency',
      currency: 'SEK',
      maximumFractionDigits: 0,
    }).format(price / 100);
  }

  private populateEditForm(e: EventDto): void {
    this.editForm.patchValue({
      title:            e.title ?? '',
      description:      e.description ?? '',
      registrationType: e.registrationType,
      dropInRules:      e.dropInRules ?? '',
      scheduleRequestText: e.scheduleRequestText ?? '',
    });
  }

  statusLabel(status: string): string {
    return EVENT_STATUS_LABEL[status] ?? status;
  }

  commentResponse(commentId: string): string {
    return this.commentResponses()[commentId] ?? '';
  }

  setCommentResponse(commentId: string, value: string): void {
    this.commentResponses.update(map => ({ ...map, [commentId]: value }));
  }

  respondToComment(commentId: string): void {
    const ev = this.event();
    if (!ev || this.saving()) return;
    const response = this.commentResponse(commentId).trim();
    if (!response) return;

    this.saving.set(true);
    this.svc.respondToEventComment(ev.id, commentId, response).subscribe({
      next: () => {
        this.saving.set(false);
        this.commentResponses.update(map => {
          const next = { ...map };
          delete next[commentId];
          return next;
        });
        this.reload();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, ERROR.respondToComment));
      },
    });
  }

  coOrganiserStatusLabel(status: string): string {
    switch (status) {
      case 'Pending': return 'Väntar';
      case 'Approved': return 'Godkänd';
      case 'Rejected': return 'Avslagen';
      case 'Cancelled': return 'Återkallad';
      default: return status;
    }
  }

  coOrganiserReviewComment(applicationId: string): string {
    return this.coOrganiserReviewComments()[applicationId] ?? '';
  }

  setCoOrganiserReviewComment(applicationId: string, value: string): void {
    this.coOrganiserReviewComments.update(map => ({ ...map, [applicationId]: value }));
  }

  approveCoOrganiserApplication(applicationId: string): void {
    const ev = this.event();
    if (!ev || this.saving()) return;

    this.saving.set(true);
    this.svc.approveCoOrganiserApplication(ev.id, applicationId).subscribe({
      next: () => {
        this.saving.set(false);
        this.reload();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, 'Kunde inte godkänna medarrangören'));
      },
    });
  }

  rejectCoOrganiserApplication(applicationId: string): void {
    const ev = this.event();
    if (!ev || this.saving()) return;

    const comment = this.coOrganiserReviewComment(applicationId).trim();
    this.saving.set(true);
    this.svc.rejectCoOrganiserApplication(ev.id, applicationId, comment || null).subscribe({
      next: () => {
        this.saving.set(false);
        this.coOrganiserReviewComments.update(map => {
          const next = { ...map };
          delete next[applicationId];
          return next;
        });
        this.reload();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(toErrorMessage(err, 'Kunde inte avslå medarrangören'));
      },
    });
  }

  registrationLabel(type: string): string {
    return REGISTRATION_KIND_LABEL[type] ?? type;
  }

  startTypeLabel(type: string): string {
    return START_TYPE_LABEL[type] ?? type;
  }

  sessionStatusLabel(status: string): string {
    return SESSION_STATUS_LABEL[status] ?? status;
  }

  readonly sortedSessions = computed(() =>
    sortBy(this.event()?.sessions ?? [], this.sessionSort(), {
      start: session => session.start,
      end: session => session.end,
      venue: session => this.venueName(session.venueId),
      seats: session => session.maxSeats,
      startType: session => this.startTypeLabel(session.startType),
      status: session => this.sessionStatusLabel(session.status),
    })
  );

  setSessionSort(key: EventSessionSortKey): void {
    this.sessionSort.set(nextSort(this.sessionSort(), key));
  }

  sessionSortIcon(key: EventSessionSortKey): string {
    return sortIcon(this.sessionSort(), key);
  }

  get sessionMin(): string | undefined { return this.edition()?.start.slice(0, 16); }
  get sessionMax(): string | undefined { return this.edition()?.end.slice(0, 16); }

  venueName(venueId: string): string {
    return this.venues().find(v => v.id === venueId)?.name ?? venueId;
  }
}
