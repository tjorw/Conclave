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
  EVENT_COMMENT_STATUS_LABEL, EVENT_STATUS_LABEL, REGISTRATION_KIND_LABEL, REGISTRATION_MODE_LABEL, START_TYPE_LABEL, SESSION_STATUS_LABEL,
  formatTicketPrice,
  MarkdownEditorComponent,
  OrganiserTicketAssignmentDto,
  OrganiserTicketTypeDto,
  RegistrationService,
  TeamRegistrationSummaryDto,
  TEAM_REGISTRATION_STATUS_CHIP,
  TEAM_REGISTRATION_STATUS_LABEL,
  toErrorMessage,
} from 'shared';
import { ChangeCategoryDialogComponent } from './change-category-dialog.component';
import { ConfirmDialogService } from '../../../shared/confirm-dialog/confirm-dialog.service';
import { DraftBlock, SessionTimelineComponent } from '../../../shared/session-timeline/session-timeline.component';
import { ERROR } from '../../../labels/errors.labels';
import { EVENT_DETAIL } from '../../../labels/pages.labels';
import { ACTION, FIELD, TOOLTIP } from '../../../labels/ui.labels';
import { createSortController, sortBy } from '../../../shared/sort-utils';
import { HelpTooltipComponent } from '../../../../help/components/help-tooltip/help-tooltip.component';
import { EditionContextService } from '../../../services/edition-context.service';
import { getSuggestedDateTimeRange } from '../../../shared/schedule-defaults';

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
    HelpTooltipComponent,
  ],
  templateUrl: './event-detail.component.html',
  styleUrl: './event-detail.component.scss',
})
export class EventDetailComponent implements OnInit {
  readonly descriptionMaxLength = 10_000;

  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly svc        = inject(EventService);
  private readonly conSvc     = inject(ConventionService);
  private readonly regSvc     = inject(RegistrationService);
  private readonly fb         = inject(FormBuilder);
  private readonly dialog      = inject(MatDialog);
  private readonly confirmSvc  = inject(ConfirmDialogService);
  private readonly editionContext = inject(EditionContextService);

  readonly ACTION        = ACTION;
  readonly TOOLTIP       = TOOLTIP;
  readonly FIELD         = FIELD;
  readonly PAGE          = EVENT_DETAIL;
  readonly registrationTypes = (Object.entries(REGISTRATION_KIND_LABEL) as [string, string][]).map(([value, label]) => ({ value, label }));
  readonly registrationModes = (Object.entries(REGISTRATION_MODE_LABEL) as [string, string][]).map(([value, label]) => ({ value, label }));
  readonly startTypes        = (Object.entries(START_TYPE_LABEL) as [string, string][]).map(([value, label]) => ({ value, label }));
  readonly routeEditionId = this.route.snapshot.paramMap.get('id');

  readonly event      = signal<EventDto | null>(null);
  readonly edition    = signal<EditionDto | null>(null);
  readonly venues     = signal<VenueDto[]>([]);
  readonly categories = signal<CategoryDto[]>([]);
  readonly loading    = signal(true);
  readonly saving     = signal(false);
  readonly registrationModeSaving = signal(false);
  readonly deleting   = signal(false);
  readonly error      = signal<string | null>(null);
  readonly showRejectForm        = signal(false);
  readonly showAddSessionForm    = signal(false);
  readonly editingSessionId      = signal<string | null>(null);
  readonly commentResponses      = signal<Record<string, string>>({});
  readonly showTimeline          = signal(false);
  readonly editionSessions       = signal<EditionSessionDto[]>([]);
  readonly timelineLoading       = signal(false);
  private readonly timelineLoaded = signal(false);
  readonly sessionSort = createSortController<EventSessionSortKey>({ key: 'start', direction: 'desc' });
  readonly organiserTicketTypes = signal<OrganiserTicketTypeDto[]>([]);
  readonly organiserTicketAssignments = signal<OrganiserTicketAssignmentDto[]>([]);
  readonly organiserTicketSelection = signal<Record<string, string | null>>({});
  readonly teamRegistrations = signal<TeamRegistrationSummaryDto[]>([]);
  readonly teamRegistrationsLoading = signal(false);
  readonly teamRegistrationsError = signal<string | null>(null);
  readonly teamRegistrationActionId = signal<string | null>(null);

  readonly limitSaving         = signal(false);
  readonly limitError          = signal<string | null>(null);
  readonly invitationSaving    = signal(false);
  readonly invitationCancelling = signal<string | null>(null);
  readonly invitationError     = signal<string | null>(null);

  readonly rejectForm = this.fb.group({
    comment: ['', [Validators.required, Validators.minLength(5)]],
  });

  readonly editForm = this.fb.group({
    title:            ['', Validators.required],
    description:      ['', [Validators.required, Validators.maxLength(this.descriptionMaxLength)]],
    programTags:      this.fb.control<string[]>([], { nonNullable: true }),
    registrationType: ['DropIn', Validators.required],
    dropInRules:      [''],
    scheduleRequestText: [''],
    coOrganiserCount: [0, [Validators.required, Validators.min(0)]],
  });

  readonly registrationModeForm = this.fb.group({
    registrationMode: ['Individual', Validators.required],
    minTeamSize: [null as number | null, [Validators.min(1)]],
    maxTeamSize: [null as number | null, [Validators.min(1)]],
  });

  readonly availableProgramTags = computed(() =>
    this.edition()?.programTagDefinitions?.map(t => t.name) ?? []
  );
  readonly listLink = computed(() => {
    const editionId = this.routeEditionId ?? this.event()?.editionId ?? null;
    return editionId ? ['/editions', editionId, 'events'] : ['/dashboard'];
  });

  readonly limitForm = this.fb.group({
    limit: [0, [Validators.required, Validators.min(0)]],
  });

  readonly invitationForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
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
  private readonly registrationModeFormValues = toSignal(this.registrationModeForm.valueChanges, {
    initialValue: this.registrationModeForm.value,
  });

  readonly timelineVenueId = computed(() => this.sessionFormValues()?.venueId ?? null);
  readonly teamRegistrationSelected = computed(() =>
    this.registrationModeFormValues()?.registrationMode === 'Team'
  );
  readonly registrationModeInvalid = computed(() => {
    const values = this.registrationModeFormValues();
    if (this.registrationModeForm.invalid) return true;
    if (values?.registrationMode !== 'Team') return false;

    const min = values.minTeamSize;
    const max = values.maxTeamSize;
    return min === null || min === undefined || max === null || max === undefined || max < min;
  });

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
    if (this.routeEditionId) {
      this.editionContext.setActive(this.routeEditionId);
    }

    const id = this.route.snapshot.paramMap.get('eventId')!;
    this.svc.getEvent(id).subscribe({
      next: e => {
        this.event.set(e);
        this.loading.set(false);
        this.populateEditForm(e);
        this.loadOrganiserTicketState(e);
        this.loadTeamRegistrations(e.id);
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

  readonly editingSession = computed(() => {
    const id = this.editingSessionId();
    if (!id) return null;
    return this.event()?.sessions.find(s => s.id === id) ?? null;
  });

  readonly commentStatusLabel = EVENT_COMMENT_STATUS_LABEL;
  readonly teamRegistrationStatusLabel = TEAM_REGISTRATION_STATUS_LABEL;
  readonly teamRegistrationStatusChip = TEAM_REGISTRATION_STATUS_CHIP;

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

  saveRegistrationMode(): void {
    const ev = this.event();
    if (!ev || this.registrationModeInvalid() || this.registrationModeSaving()) return;

    const { registrationMode, minTeamSize, maxTeamSize } = this.registrationModeForm.getRawValue();
    const isTeam = registrationMode === 'Team';

    this.registrationModeSaving.set(true);
    this.svc.configureTeamRegistration(
      ev.id,
      registrationMode!,
      isTeam ? minTeamSize : null,
      isTeam ? maxTeamSize : null
    ).subscribe({
      next: () => { this.registrationModeSaving.set(false); this.reload(); },
      error: err => {
        this.registrationModeSaving.set(false);
        this.error.set(toErrorMessage(err, ERROR.configureTeamRegistration));
      },
    });
  }

  refreshTeamRegistrations(): void {
    const ev = this.event();
    if (!ev) return;
    this.loadTeamRegistrations(ev.id);
  }

  confirmTeamRegistration(registrationId: string): void {
    if (this.teamRegistrationActionId()) return;
    this.teamRegistrationActionId.set(registrationId);
    this.regSvc.confirmTeamRegistration(registrationId).subscribe({
      next: () => {
        this.teamRegistrationActionId.set(null);
        this.refreshTeamRegistrations();
      },
      error: err => {
        this.teamRegistrationActionId.set(null);
        this.teamRegistrationsError.set(toErrorMessage(err, ERROR.confirmTeamRegistration));
      },
    });
  }

  cancelTeamRegistration(registrationId: string): void {
    if (this.teamRegistrationActionId()) return;
    this.teamRegistrationActionId.set(registrationId);
    this.regSvc.cancelTeamRegistration(registrationId).subscribe({
      next: () => {
        this.teamRegistrationActionId.set(null);
        this.refreshTeamRegistrations();
      },
      error: err => {
        this.teamRegistrationActionId.set(null);
        this.teamRegistrationsError.set(toErrorMessage(err, ERROR.cancelTeamRegistration));
      },
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
    this.confirmSvc.confirm({
      title:   this.PAGE.deleteTitle,
      message: this.PAGE.deleteMessage(ev.title || this.PAGE.noName),
    }).subscribe(confirmed => {
      if (!confirmed) return;
      this.deleting.set(true);
      this.svc.deleteEvent(ev.id).subscribe({
        next: () => this.router.navigate(this.listLink()),
        error: err => { this.deleting.set(false); this.error.set(toErrorMessage(err, ERROR.deleteEvent)); },
      });
    });
  }

  // ── Edit draft ──────────────────────────────────────────────────────────

  saveEdit(): void {
    const ev = this.event();
    if (!ev || this.editForm.invalid || this.saving()) return;
    const { title, description, programTags, registrationType, dropInRules, scheduleRequestText, coOrganiserCount } = this.editForm.getRawValue();
    this.saving.set(true);
    this.svc.updateDraft(ev.id, title!, description!, programTags ?? [], registrationType!, dropInRules || null, scheduleRequestText || null, coOrganiserCount!).subscribe({
      next: () => { this.saving.set(false); this.reload(); },
      error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, ERROR.saveDraft)); },
    });
  }

  // ── Session requests ────────────────────────────────────────────────────

  // ── Sessions ────────────────────────────────────────────────────────────

  toggleAddSessionForm(): void {
    const shouldOpen = !this.showAddSessionForm();
    this.showAddSessionForm.set(shouldOpen);
    this.editingSessionId.set(null);

    if (!shouldOpen) {
      this.sessionForm.reset({ maxSeats: 20, startType: 'FixedTime' });
      return;
    }

    const defaults = getSuggestedDateTimeRange(this.edition(), 120);
    this.sessionForm.reset({
      venueId: this.venues()[0]?.id ?? '',
      startTime: defaults?.start ?? '',
      endTime: defaults?.end ?? '',
      maxSeats: 20,
      startType: 'FixedTime',
    });
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

  closePanels(): void {
    this.editingSessionId.set(null);
    this.showAddSessionForm.set(false);
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
      next: () => { this.saving.set(false); this.closePanels(); this.reload(); this.refreshEditionSessions(); },
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
      next: e => { this.event.set(e); this.populateEditForm(e); this.loadOrganiserTicketState(e); this.loadTeamRegistrations(e.id); },
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

  protected readonly ticketTypePriceLabel = formatTicketPrice;

  private populateEditForm(e: EventDto): void {
    this.editForm.patchValue({
      title:            e.title ?? '',
      description:      e.description ?? '',
      programTags:      e.programTags ?? [],
      registrationType: e.registrationType,
      dropInRules:      e.dropInRules ?? '',
      scheduleRequestText: e.scheduleRequestText ?? '',
      coOrganiserCount: e.coOrganiserCount,
    });
    this.limitForm.patchValue({ limit: e.coOrganiserLimit });
    this.registrationModeForm.patchValue({
      registrationMode: e.registrationMode ?? 'Individual',
      minTeamSize: e.minTeamSize,
      maxTeamSize: e.maxTeamSize,
    });
  }

  private loadTeamRegistrations(eventId: string): void {
    this.teamRegistrationsLoading.set(true);
    this.teamRegistrationsError.set(null);
    this.regSvc.listTeamRegistrations(eventId).subscribe({
      next: registrations => {
        this.teamRegistrations.set(registrations);
        this.teamRegistrationsLoading.set(false);
      },
      error: err => {
        this.teamRegistrations.set([]);
        this.teamRegistrationsLoading.set(false);
        this.teamRegistrationsError.set(toErrorMessage(err, ERROR.fetchTeamRegistrations));
      },
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

  // ── Co-organiser invitations ────────────────────────────────────────────

  adjustCoOrganiserLimit(): void {
    const ev = this.event();
    if (!ev || this.limitForm.invalid || this.limitSaving()) return;
    const limit = this.limitForm.getRawValue().limit!;
    this.limitSaving.set(true);
    this.limitError.set(null);
    this.svc.adjustCoOrganiserLimit(ev.id, limit).subscribe({
      next: () => { this.limitSaving.set(false); this.reload(); },
      error: err => { this.limitSaving.set(false); this.limitError.set(toErrorMessage(err, 'Kunde inte uppdatera gränsen.')); },
    });
  }

  createCoOrganiserInvitation(): void {
    const ev = this.event();
    if (!ev || this.invitationForm.invalid || this.invitationSaving()) return;
    const email = this.invitationForm.getRawValue().email!;
    this.invitationSaving.set(true);
    this.invitationError.set(null);
    this.svc.createCoOrganiserInvitation(ev.id, email).subscribe({
      next: () => {
        this.invitationSaving.set(false);
        this.invitationForm.reset({ email: '' });
        this.reload();
      },
      error: err => { this.invitationSaving.set(false); this.invitationError.set(toErrorMessage(err, 'Kunde inte skicka inbjudan.')); },
    });
  }

  cancelCoOrganiserInvitation(invitationId: string): void {
    const ev = this.event();
    if (!ev || this.invitationCancelling() !== null) return;
    this.invitationCancelling.set(invitationId);
    this.invitationError.set(null);
    this.svc.cancelCoOrganiserInvitation(ev.id, invitationId).subscribe({
      next: () => { this.invitationCancelling.set(null); this.reload(); },
      error: err => { this.invitationCancelling.set(null); this.invitationError.set(toErrorMessage(err, 'Kunde inte avbryta inbjudan.')); },
    });
  }

  removeCoOrganiser(personId: string): void {
    const ev = this.event();
    if (!ev || this.saving()) return;

    this.confirmSvc.confirm({
      title: 'Ta bort medarrangör',
      message: 'Är du säker på att du vill ta bort medarrangören från evenemanget?',
    }).subscribe(confirmed => {
      if (!confirmed) return;
      this.saving.set(true);
      this.svc.removeCoOrganiser(ev.id, personId).subscribe({
        next: () => { this.saving.set(false); this.reload(); },
        error: err => { this.saving.set(false); this.error.set(toErrorMessage(err, 'Kunde inte ta bort medarrangören')); },
      });
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
    sortBy(this.event()?.sessions ?? [], this.sessionSort.state(), {
      start: session => session.start,
      end: session => session.end,
      venue: session => this.venueName(session.venueId),
      seats: session => session.maxSeats,
      startType: session => this.startTypeLabel(session.startType),
      status: session => this.sessionStatusLabel(session.status),
    })
  );



  get sessionMin(): string | undefined { return this.edition()?.start.slice(0, 16); }
  get sessionMax(): string | undefined { return this.edition()?.end.slice(0, 16); }

  venueName(venueId: string): string {
    return this.venues().find(v => v.id === venueId)?.name ?? venueId;
  }
}
