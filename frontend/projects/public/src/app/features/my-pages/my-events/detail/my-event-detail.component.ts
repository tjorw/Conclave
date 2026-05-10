import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  AuthService,
  EventService, EventDto,
  EVENT_STATUS_CHIP,
  MarkdownEditorComponent,
  toErrorMessage,
} from 'shared';
import { MarkdownComponent } from 'ngx-markdown';
import { switchMap } from 'rxjs';
import { EditionService } from '../../../../services/edition.service';
import { LocaleService, SupportedLocale } from '../../../../services/locale.service';

const DETAIL_TEXT: Record<SupportedLocale, {
  backToMyEvents: string;
  unnamedEvent: string;
  adminCommentPrefix: string;
  returnToDraft: string;
  eventDetails: string;
  saved: string;
  title: string;
  description: string;
  scheduleRequest: string;
  requestedCoOrganisers: string;
  registrationType: string;
  dropInRulesOptional: string;
  saveDraft: string;
  submitForReview: string;
  submissionsClosed: string;
  delete: string;
  deleteConfirm: string;
  category: string;
  registration: string;
  noScheduleRequestsYet: string;
  coOrganisers: string;
  noCoOrganisersYet: string;
  approvedCountPrefix: string;
  approvedCountNotSet: string;
  invitationSent: string;
  invitedCoOrganiserEmail: string;
  sendInvitation: string;
  inviteWhenLimitSet: string;
  allApprovedSlotsUsed: string;
  pendingInvitations: string;
  sentOn: string;
  cancel: string;
  sessions: string;
  registered: string;
  suggestChange: string;
  suggestChangeHelp: string;
  suggestionSent: string;
  comment: string;
  sendSuggestion: string;
  comments: string;
  adminReplyPrefix: string;
  acknowledgeHandled: string;
  statusDraft: string;
  statusUnderReview: string;
  statusPublished: string;
  statusCancelled: string;
  registrationDropIn: string;
  registrationPreRegistration: string;
  registrationCombined: string;
  sessionActive: string;
  sessionInactive: string;
  commentNew: string;
  commentInProgress: string;
  commentResponded: string;
  commentAcknowledged: string;
  errSaveDraft: string;
  errSubmitForReview: string;
  errReturnToDraft: string;
  errDelete: string;
  errAddSuggestion: string;
  errCreateInvitation: string;
  errCancelInvitation: string;
  errAcknowledge: string;
}> = {
  sv: {
    backToMyEvents: '\u2190 Mina arrangemang',
    unnamedEvent: '(Namnlöst arrangemang)',
    adminCommentPrefix: 'Kommentar från administratör:',
    returnToDraft: 'Dra tillbaka till utkast',
    eventDetails: 'Arrangemangsuppgifter',
    saved: 'Sparat.',
    title: 'Titel',
    description: 'Beskrivning',
    scheduleRequest: 'Schemaönskemål',
    requestedCoOrganisers: 'Önskat antal medarrangörer',
    registrationType: 'Registreringstyp',
    dropInRulesOptional: 'Drop-in-regler (valfritt)',
    saveDraft: 'Spara utkast',
    submitForReview: 'Skicka in för granskning',
    submissionsClosed: 'Arrangemangsansökan är för tillfället stängd.',
    delete: 'Ta bort',
    deleteConfirm: 'Ta bort arrangemanget permanent?',
    category: 'Kategori',
    registration: 'Registrering',
    noScheduleRequestsYet: 'Inga schemaönskemål än.',
    coOrganisers: 'Medarrangörer',
    noCoOrganisersYet: 'Inga medarrangörer än.',
    approvedCountPrefix: 'Godkänt antal:',
    approvedCountNotSet: 'Gränsen har inte fastställts av admin ännu.',
    invitationSent: 'Inbjudan skickad.',
    invitedCoOrganiserEmail: 'E-postadress till inbjuden medarrangör',
    sendInvitation: 'Skicka inbjudan',
    inviteWhenLimitSet: 'Du kan skicka inbjudningar när admin har fastställt godkänt antal.',
    allApprovedSlotsUsed: 'Alla godkända platser är utnyttjade.',
    pendingInvitations: 'Väntande inbjudningar',
    sentOn: 'Skickad',
    cancel: 'Avbryt',
    sessions: 'Sessioner',
    registered: 'anmälda',
    suggestChange: 'Föreslå ändring',
    suggestChangeHelp: 'Lämna en kommentar om du vill föreslå en justering av schema eller upplägg.',
    suggestionSent: 'Ändringsförslag skickat.',
    comment: 'Kommentar',
    sendSuggestion: 'Skicka ändringsförslag',
    comments: 'Kommentarer',
    adminReplyPrefix: 'Admins svar:',
    acknowledgeHandled: 'Kvittera som hanterad',
    statusDraft: 'Utkast',
    statusUnderReview: 'Under granskning',
    statusPublished: 'Publicerat',
    statusCancelled: 'Inställt',
    registrationDropIn: 'Drop-in',
    registrationPreRegistration: 'Föranmälan',
    registrationCombined: 'Kombinerat',
    sessionActive: 'Aktiv',
    sessionInactive: 'Inaktiv',
    commentNew: 'Ny',
    commentInProgress: 'Under behandling',
    commentResponded: 'Besvarad',
    commentAcknowledged: 'Kvitterad',
    errSaveDraft: 'Kunde inte spara utkastet.',
    errSubmitForReview: 'Kunde inte skicka in arrangemanget.',
    errReturnToDraft: 'Kunde inte återgå till utkast.',
    errDelete: 'Kunde inte ta bort arrangemanget.',
    errAddSuggestion: 'Kunde inte skicka ändringsförslaget.',
    errCreateInvitation: 'Kunde inte skicka inbjudan.',
    errCancelInvitation: 'Kunde inte avbryta inbjudan.',
    errAcknowledge: 'Kunde inte kvittera kommentaren.',
  },
  en: {
    backToMyEvents: '\u2190 My events',
    unnamedEvent: '(Unnamed event)',
    adminCommentPrefix: 'Comment from administrator:',
    returnToDraft: 'Return to draft',
    eventDetails: 'Event details',
    saved: 'Saved.',
    title: 'Title',
    description: 'Description',
    scheduleRequest: 'Schedule request',
    requestedCoOrganisers: 'Requested number of co-organisers',
    registrationType: 'Registration type',
    dropInRulesOptional: 'Drop-in rules (optional)',
    saveDraft: 'Save draft',
    submitForReview: 'Submit for review',
    submissionsClosed: 'Event submissions are currently closed.',
    delete: 'Delete',
    deleteConfirm: 'Delete this event permanently?',
    category: 'Category',
    registration: 'Registration',
    noScheduleRequestsYet: 'No schedule request yet.',
    coOrganisers: 'Co-organisers',
    noCoOrganisersYet: 'No co-organisers yet.',
    approvedCountPrefix: 'Approved count:',
    approvedCountNotSet: 'The limit has not been set by admin yet.',
    invitationSent: 'Invitation sent.',
    invitedCoOrganiserEmail: 'Email address for invited co-organiser',
    sendInvitation: 'Send invitation',
    inviteWhenLimitSet: 'You can send invitations when admin has set the approved count.',
    allApprovedSlotsUsed: 'All approved slots are already used.',
    pendingInvitations: 'Pending invitations',
    sentOn: 'Sent',
    cancel: 'Cancel',
    sessions: 'Sessions',
    registered: 'registered',
    suggestChange: 'Suggest change',
    suggestChangeHelp: 'Leave a comment if you want to suggest an adjustment to schedule or setup.',
    suggestionSent: 'Change request sent.',
    comment: 'Comment',
    sendSuggestion: 'Send change request',
    comments: 'Comments',
    adminReplyPrefix: 'Admin reply:',
    acknowledgeHandled: 'Acknowledge as handled',
    statusDraft: 'Draft',
    statusUnderReview: 'Under review',
    statusPublished: 'Published',
    statusCancelled: 'Cancelled',
    registrationDropIn: 'Drop-in',
    registrationPreRegistration: 'Pre-registration',
    registrationCombined: 'Combined',
    sessionActive: 'Active',
    sessionInactive: 'Inactive',
    commentNew: 'New',
    commentInProgress: 'In progress',
    commentResponded: 'Responded',
    commentAcknowledged: 'Acknowledged',
    errSaveDraft: 'Could not save the draft.',
    errSubmitForReview: 'Could not submit the event.',
    errReturnToDraft: 'Could not return to draft.',
    errDelete: 'Could not delete the event.',
    errAddSuggestion: 'Could not send the change request.',
    errCreateInvitation: 'Could not send invitation.',
    errCancelInvitation: 'Could not cancel invitation.',
    errAcknowledge: 'Could not acknowledge the comment.',
  },
};

type DraftOperation = 'saving' | 'submitting' | 'returning' | 'deleting';

type DraftState = {
  operation: DraftOperation | null;
  saved: boolean;
  error: string | null;
  actionError: string | null;
};

type CommentState = {
  adding: boolean;
  acknowledging: boolean;
  saved: boolean;
  error: string | null;
};

type InvitationState = {
  saving: boolean;
  cancelling: string | null;
  saved: boolean;
  error: string | null;
};


@Component({
  selector: 'app-my-event-detail',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MarkdownComponent,
    MarkdownEditorComponent,
  ],
  templateUrl: './my-event-detail.component.html',
  styleUrl: './my-event-detail.component.scss',
})
export class MyEventDetailComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly eventSvc   = inject(EventService);
  private readonly authSvc    = inject(AuthService);
  private readonly fb         = inject(FormBuilder);
  private readonly editionSvc = inject(EditionService);
  private readonly localeSvc  = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading       = signal(true);
  readonly event         = signal<EventDto | null>(null);
  readonly draftState    = signal<DraftState>({ operation: null, saved: false, error: null, actionError: null });
  readonly commentState     = signal<CommentState>({ adding: false, acknowledging: false, saved: false, error: null });
  readonly invitationState  = signal<InvitationState>({ saving: false, cancelling: null, saved: false, error: null });
  readonly statusChip  = EVENT_STATUS_CHIP;
  readonly detailText = computed(() => DETAIL_TEXT[this.localeSvc.locale()]);

  readonly registrationTypes = computed(() => {
    const t = this.detailText();
    return [
      { value: 'DropIn',          label: t.registrationDropIn },
      { value: 'PreRegistration', label: t.registrationPreRegistration },
      { value: 'Combined',        label: t.registrationCombined },
    ];
  });

  readonly draftForm = this.fb.group({
    title:               ['', Validators.required],
    description:         ['', Validators.required],
    registrationType:    ['DropIn', Validators.required],
    dropInRules:         [''],
    scheduleRequestText: [''],
    coOrganiserCount:    [0, [Validators.required, Validators.min(0)]],
  });

  readonly commentForm = this.fb.group({
    text: ['', [Validators.required, Validators.minLength(5)]],
  });

  readonly invitationForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  get eventId(): string {
    return this.route.snapshot.paramMap.get('id')!;
  }

  get isDraft(): boolean {
    return this.event()?.status === 'Draft';
  }

  readonly adminComment = computed(() => {
    const ev = this.event();
    if (!ev) return null;
    const adminComments = ev.comments
      .filter(c => !c.requiresHandling)
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    return adminComments[0]?.text ?? null;
  });

  get currentPersonId(): string | null {
    return this.authSvc.personId();
  }

  get eventSubmissionsOpen(): boolean {
    return this.editionSvc.edition()?.organiserRegistrationOpen ?? false;
  }

  get canDelete(): boolean {
    return this.event()?.status === 'Draft';
  }

  get isLeadOrganiser(): boolean {
    return this.event()?.leadOrganiserId === this.currentPersonId;
  }

  get isCoOrganiser(): boolean {
    return this.event()?.coOrganisers.some(c => c.personId === this.currentPersonId) ?? false;
  }

  get canSeeCoOrganiserSection(): boolean {
    const status = this.event()?.status;
    return (this.isLeadOrganiser || this.isCoOrganiser) &&
      (status === 'Draft' || status === 'UnderReview' || status === 'Published');
  }

  get canManageInvitations(): boolean {
    return this.isLeadOrganiser && this.canSeeCoOrganiserSection;
  }

  readonly activeInvitations = computed(() =>
    this.event()?.coOrganiserInvitations ?? []
  );

  get canCommentOnPublishedEvent(): boolean {
    return this.event()?.status === 'Published';
  }

  ngOnInit(): void {
    this.loadEvent();
  }

  private loadEvent(): void {
    this.loading.set(true);
    this.eventSvc.getEvent(this.eventId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ev => {
        this.event.set(ev);
        this.draftForm.patchValue({
          title:               ev.title ?? '',
          description:         ev.description ?? '',
          registrationType:    ev.registrationType ?? 'DropIn',
          dropInRules:         ev.dropInRules ?? '',
          scheduleRequestText: ev.scheduleRequestText ?? '',
          coOrganiserCount:    ev.coOrganiserCount ?? 0,
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  saveDraft(): void {
    if (this.draftForm.invalid || this.draftState().operation !== null) return;
    this.draftState.set({ operation: 'saving', saved: false, error: null, actionError: null });
    this.updateDraftFromForm().subscribe({
      next: () => this.draftState.update(state => ({ ...state, operation: null, saved: true })),
      error: err => {
        this.draftState.update(state => ({
          ...state,
          operation: null,
          error: toErrorMessage(err, this.detailText().errSaveDraft),
        }));
      },
    });
  }

  submitForReview(): void {
    if (this.draftForm.invalid || this.draftState().operation !== null) return;
    this.draftState.update(state => ({ ...state, operation: 'submitting', saved: false, error: null, actionError: null }));
    this.updateDraftFromForm().pipe(
      switchMap(() => this.eventSvc.submitForReview(this.eventId))
    ).subscribe({
      next: () => {
        this.draftState.update(state => ({ ...state, operation: null }));
        this.loadEvent();
      },
      error: err => {
        this.draftState.update(state => ({
          ...state,
          operation: null,
          actionError: toErrorMessage(err, this.detailText().errSubmitForReview),
        }));
      },
    });
  }

  private updateDraftFromForm() {
    const { title, description, registrationType, dropInRules, scheduleRequestText, coOrganiserCount } = this.draftForm.getRawValue();
    const programTags = this.event()?.programTags ?? [];
    return this.eventSvc.updateDraft(
      this.eventId, title!, description!, programTags, registrationType!, dropInRules || null, scheduleRequestText || null, coOrganiserCount ?? 0
    );
  }

  returnToDraft(): void {
    if (this.draftState().operation !== null) return;
    this.draftState.update(state => ({ ...state, operation: 'returning', actionError: null }));
    this.eventSvc.returnToDraft(this.eventId).subscribe({
      next: () => {
        this.draftState.update(state => ({ ...state, operation: null }));
        this.loadEvent();
      },
      error: err => {
        this.draftState.update(state => ({
          ...state,
          operation: null,
          actionError: toErrorMessage(err, this.detailText().errReturnToDraft),
        }));
      },
    });
  }

  deleteEvent(): void {
    if (this.draftState().operation !== null || !confirm(this.detailText().deleteConfirm)) return;
    this.draftState.update(state => ({ ...state, operation: 'deleting', actionError: null }));
    this.eventSvc.deleteEvent(this.eventId).subscribe({
      next: () => this.router.navigate(['/my-pages/events']),
      error: err => {
        this.draftState.update(state => ({
          ...state,
          operation: null,
          actionError: toErrorMessage(err, this.detailText().errDelete),
        }));
      },
    });
  }

  addChangeComment(): void {
    if (this.commentForm.invalid || this.commentState().adding) return;
    this.commentState.set({ adding: true, acknowledging: false, saved: false, error: null });

    const text = this.commentForm.getRawValue().text!;
    this.eventSvc.addEventComment(this.eventId, text).subscribe({
      next: () => {
        this.commentState.update(state => ({ ...state, adding: false, saved: true }));
        this.commentForm.reset({ text: '' });
        this.loadEvent();
      },
      error: err => {
        this.commentState.update(state => ({
          ...state,
          adding: false,
          error: toErrorMessage(err, this.detailText().errAddSuggestion),
        }));
      },
    });
  }

  createInvitation(): void {
    if (this.invitationForm.invalid || this.invitationState().saving) return;
    this.invitationState.set({ saving: true, cancelling: null, saved: false, error: null });
    const email = this.invitationForm.getRawValue().email!;
    this.eventSvc.createCoOrganiserInvitation(this.eventId, email).subscribe({
      next: () => {
        this.invitationState.update(s => ({ ...s, saving: false, saved: true }));
        this.invitationForm.reset({ email: '' });
        this.loadEvent();
      },
      error: err => {
        this.invitationState.update(s => ({
          ...s,
          saving: false,
          error: toErrorMessage(err, this.detailText().errCreateInvitation),
        }));
      },
    });
  }

  cancelInvitation(invitationId: string): void {
    if (this.invitationState().cancelling !== null) return;
    this.invitationState.update(s => ({ ...s, cancelling: invitationId, error: null }));
    this.eventSvc.cancelCoOrganiserInvitation(this.eventId, invitationId).subscribe({
      next: () => {
        this.invitationState.update(s => ({ ...s, cancelling: null }));
        this.loadEvent();
      },
      error: err => {
        this.invitationState.update(s => ({
          ...s,
          cancelling: null,
          error: toErrorMessage(err, this.detailText().errCancelInvitation),
        }));
      },
    });
  }

  acknowledgeComment(commentId: string): void {
    if (this.commentState().acknowledging) return;
    this.commentState.update(state => ({ ...state, acknowledging: true }));
    this.draftState.update(state => ({ ...state, actionError: null }));

    this.eventSvc.acknowledgeEventComment(this.eventId, commentId).subscribe({
      next: () => {
        this.commentState.update(state => ({ ...state, acknowledging: false }));
        this.loadEvent();
      },
      error: err => {
        this.draftState.update(state => ({
          ...state,
          actionError: toErrorMessage(err, this.detailText().errAcknowledge),
        }));
        this.commentState.update(state => ({ ...state, acknowledging: false }));
      },
    });
  }

  eventStatusLabel(status: string): string {
    const t = this.detailText();
    const map: Record<string, string> = {
      Draft: t.statusDraft,
      UnderReview: t.statusUnderReview,
      Published: t.statusPublished,
      Cancelled: t.statusCancelled,
    };
    return map[status] ?? status;
  }

  registrationKindLabel(value: string): string {
    const t = this.detailText();
    const map: Record<string, string> = {
      DropIn: t.registrationDropIn,
      PreRegistration: t.registrationPreRegistration,
      Combined: t.registrationCombined,
    };
    return map[value] ?? value;
  }

  commentStatusText(value: string): string {
    const t = this.detailText();
    const map: Record<string, string> = {
      New: t.commentNew,
      InProgress: t.commentInProgress,
      Responded: t.commentResponded,
      Acknowledged: t.commentAcknowledged,
    };
    return map[value] ?? value;
  }

  sessionStatusText(value: string): string {
    const t = this.detailText();
    const map: Record<string, string> = {
      Active: t.sessionActive,
      Inactive: t.sessionInactive,
    };
    return map[value] ?? value;
  }

  coOrganiserTargetText(count: number): string {
    if (this.localeSvc.locale() === 'en') {
      return `You requested ${count} co-organiser${count === 1 ? '' : 's'}.`;
    }

    return `Du har angett att du vill ha ${count} medarrangör${count === 1 ? '' : 'er'}.`;
  }

}
