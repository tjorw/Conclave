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
  EVENT_COMMENT_STATUS_LABEL,
  EventService, EventDto,
  EVENT_STATUS_LABEL, EVENT_STATUS_CHIP,
  MarkdownEditorComponent,
  REGISTRATION_KIND_LABEL,
  toErrorMessage,
  CoOrganiserInvitationDto,
} from 'shared';
import { MarkdownComponent } from 'ngx-markdown';
import { switchMap } from 'rxjs';
import { EditionService } from '../../../../services/edition.service';

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
  private readonly destroyRef = inject(DestroyRef);

  readonly loading       = signal(true);
  readonly event         = signal<EventDto | null>(null);
  readonly draftState    = signal<DraftState>({ operation: null, saved: false, error: null, actionError: null });
  readonly commentState     = signal<CommentState>({ adding: false, acknowledging: false, saved: false, error: null });
  readonly invitationState  = signal<InvitationState>({ saving: false, cancelling: null, saved: false, error: null });
  readonly statusLabel = EVENT_STATUS_LABEL;
  readonly statusChip  = EVENT_STATUS_CHIP;
  readonly regKindLabel = REGISTRATION_KIND_LABEL;
  readonly commentStatusLabel = EVENT_COMMENT_STATUS_LABEL;

  readonly registrationTypes = [
    { value: 'DropIn',          label: 'Drop-in' },
    { value: 'PreRegistration', label: 'Föranmälan' },
    { value: 'Combined',        label: 'Kombinerat' },
  ];

  readonly draftForm = this.fb.group({
    title:            ['', Validators.required],
    description:      ['', Validators.required],
    registrationType: ['DropIn', Validators.required],
    dropInRules:      [''],
    scheduleRequestText: [''],
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

  get canManageInvitations(): boolean {
    const status = this.event()?.status;
    return this.isLeadOrganiser && (status === 'Draft' || status === 'UnderReview' || status === 'Published');
  }

  readonly activeInvitations = computed(() =>
    this.event()?.coOrganiserInvitations.filter(i => i.status === 'Active') ?? []
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
          title:            ev.title ?? '',
          description:      ev.description ?? '',
          registrationType: ev.registrationType ?? 'DropIn',
          dropInRules:      ev.dropInRules ?? '',
          scheduleRequestText: ev.scheduleRequestText ?? '',
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
          error: toErrorMessage(err, 'Kunde inte spara utkastet.'),
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
          actionError: toErrorMessage(err, 'Kunde inte skicka in arrangemanget.'),
        }));
      },
    });
  }

  private updateDraftFromForm() {
    const { title, description, registrationType, dropInRules, scheduleRequestText } = this.draftForm.getRawValue();
    return this.eventSvc.updateDraft(
      this.eventId, title!, description!, registrationType!, dropInRules || null, scheduleRequestText || null
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
          actionError: toErrorMessage(err, 'Kunde inte återgå till utkast.'),
        }));
      },
    });
  }

  deleteEvent(): void {
    if (this.draftState().operation !== null || !confirm('Ta bort arrangemanget permanent?')) return;
    this.draftState.update(state => ({ ...state, operation: 'deleting', actionError: null }));
    this.eventSvc.deleteEvent(this.eventId).subscribe({
      next: () => this.router.navigate(['/my-pages/events']),
      error: err => {
        this.draftState.update(state => ({
          ...state,
          operation: null,
          actionError: toErrorMessage(err, 'Kunde inte ta bort arrangemanget.'),
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
          error: toErrorMessage(err, 'Kunde inte skicka ändringsförslaget.'),
        }));
      },
    });
  }

  setCoOrganiserCount(count: number): void {
    if (this.draftState().operation !== null) return;
    this.draftState.update(s => ({ ...s, operation: 'saving', actionError: null }));
    this.eventSvc.setCoOrganiserCount(this.eventId, count).subscribe({
      next: () => {
        this.draftState.update(s => ({ ...s, operation: null }));
        this.loadEvent();
      },
      error: err => {
        this.draftState.update(s => ({
          ...s,
          operation: null,
          actionError: toErrorMessage(err, 'Kunde inte uppdatera antal medarrangörer.'),
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
          error: toErrorMessage(err, 'Kunde inte skicka inbjudan.'),
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
          error: toErrorMessage(err, 'Kunde inte avbryta inbjudan.'),
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
          actionError: toErrorMessage(err, 'Kunde inte kvittera kommentaren.'),
        }));
        this.commentState.update(state => ({ ...state, acknowledging: false }));
      },
    });
  }

}
