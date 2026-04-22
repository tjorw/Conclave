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
import { HttpErrorResponse } from '@angular/common/http';
import {
  AuthService,
  EVENT_COMMENT_STATUS_LABEL,
  EventService, EventDto,
  EVENT_STATUS_LABEL, EVENT_STATUS_CHIP,
  REGISTRATION_KIND_LABEL,
} from 'shared';
import { EditionService } from '../../../../services/edition.service';

type DraftOperation = 'saving' | 'submitting' | 'returning' | 'deleting';

type DraftState = {
  operation: DraftOperation | null;
  saved: boolean;
  error: string | null;
  actionError: string | null;
};

type RequestState = {
  adding: boolean;
  saved: boolean;
  error: string | null;
};

type CommentState = {
  adding: boolean;
  acknowledging: boolean;
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
  readonly requestState  = signal<RequestState>({ adding: false, saved: false, error: null });
  readonly commentState  = signal<CommentState>({ adding: false, acknowledging: false, saved: false, error: null });

  readonly statusLabel = EVENT_STATUS_LABEL;
  readonly statusChip  = EVENT_STATUS_CHIP;
  readonly regKindLabel = REGISTRATION_KIND_LABEL;
  readonly commentStatusLabel = EVENT_COMMENT_STATUS_LABEL;

  readonly registrationTypes = [
    { value: 'DropIn',          label: 'Drop-in' },
    { value: 'PreRegistration', label: 'Föranmälan' },
    { value: 'Combined',        label: 'Kombinerat' },
  ];

  readonly startTypes = [
    { value: 'FixedTime',  label: 'Fast tid' },
    { value: 'Rolling',    label: 'Löpande' },
    { value: 'Tournament', label: 'Turneringsformat' },
  ];

  readonly draftForm = this.fb.group({
    title:            ['', Validators.required],
    description:      ['', Validators.required],
    registrationType: ['DropIn', Validators.required],
    dropInRules:      [''],
  });

  readonly requestForm = this.fb.group({
    description:      ['', Validators.required],
    durationMinutes:  [60, [Validators.required, Validators.min(10)]],
    seats:            [20, [Validators.required, Validators.min(1)]],
    startType:        ['FixedTime', Validators.required],
  });

  readonly commentForm = this.fb.group({
    text: ['', [Validators.required, Validators.minLength(5)]],
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
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  saveDraft(): void {
    if (this.draftForm.invalid || this.draftState().operation !== null) return;
    this.draftState.set({ operation: 'saving', saved: false, error: null, actionError: null });
    const { title, description, registrationType, dropInRules } = this.draftForm.getRawValue();
    this.eventSvc.updateDraft(
      this.eventId, title!, description!, registrationType!, dropInRules || null
    ).subscribe({
      next: () => this.draftState.update(state => ({ ...state, operation: null, saved: true })),
      error: (err: HttpErrorResponse) => {
        this.draftState.update(state => ({
          ...state,
          operation: null,
          error: err.error?.detail ?? err.error?.title ?? 'Kunde inte spara utkastet.',
        }));
      },
    });
  }

  addSessionRequest(): void {
    if (this.requestForm.invalid || this.requestState().adding) return;
    this.requestState.set({ adding: true, saved: false, error: null });
    const { description, durationMinutes, seats, startType } = this.requestForm.getRawValue();
    this.eventSvc.addSessionRequest(
      this.eventId, description!, durationMinutes!, seats!, startType!
    ).subscribe({
      next: () => {
        this.requestState.update(state => ({ ...state, adding: false, saved: true }));
        this.requestForm.reset({ description: '', durationMinutes: 60, seats: 20, startType: 'FixedTime' });
        this.loadEvent();
      },
      error: (err: HttpErrorResponse) => {
        this.requestState.update(state => ({
          ...state,
          adding: false,
          error: err.error?.detail ?? err.error?.title ?? 'Kunde inte lägga till sessionönskemål.',
        }));
      },
    });
  }

  removeSessionRequest(requestId: string): void {
    this.eventSvc.removeSessionRequest(this.eventId, requestId).subscribe({
      next: () => this.loadEvent(),
      error: (err: HttpErrorResponse) => {
        this.draftState.update(state => ({
          ...state,
          actionError: err.error?.detail ?? err.error?.title ?? 'Kunde inte ta bort sessionönskemålet.',
        }));
      },
    });
  }

  submitForReview(): void {
    if (this.draftState().operation !== null) return;
    this.draftState.update(state => ({ ...state, operation: 'submitting', actionError: null }));
    this.eventSvc.submitForReview(this.eventId).subscribe({
      next: () => {
        this.draftState.update(state => ({ ...state, operation: null }));
        this.loadEvent();
      },
      error: (err: HttpErrorResponse) => {
        this.draftState.update(state => ({
          ...state,
          operation: null,
          actionError: err.error?.detail ?? err.error?.title ?? 'Kunde inte skicka in arrangemanget.',
        }));
      },
    });
  }

  returnToDraft(): void {
    if (this.draftState().operation !== null) return;
    this.draftState.update(state => ({ ...state, operation: 'returning', actionError: null }));
    this.eventSvc.returnToDraft(this.eventId).subscribe({
      next: () => {
        this.draftState.update(state => ({ ...state, operation: null }));
        this.loadEvent();
      },
      error: (err: HttpErrorResponse) => {
        this.draftState.update(state => ({
          ...state,
          operation: null,
          actionError: err.error?.detail ?? err.error?.title ?? 'Kunde inte återgå till utkast.',
        }));
      },
    });
  }

  deleteEvent(): void {
    if (this.draftState().operation !== null || !confirm('Ta bort arrangemanget permanent?')) return;
    this.draftState.update(state => ({ ...state, operation: 'deleting', actionError: null }));
    this.eventSvc.deleteEvent(this.eventId).subscribe({
      next: () => this.router.navigate(['/my-pages/events']),
      error: (err: HttpErrorResponse) => {
        this.draftState.update(state => ({
          ...state,
          operation: null,
          actionError: err.error?.detail ?? err.error?.title ?? 'Kunde inte ta bort arrangemanget.',
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
      error: (err: HttpErrorResponse) => {
        this.commentState.update(state => ({
          ...state,
          adding: false,
          error: err.error?.detail ?? err.error?.title ?? 'Kunde inte skicka ändringsförslaget.',
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
      error: (err: HttpErrorResponse) => {
        this.draftState.update(state => ({
          ...state,
          actionError: err.error?.detail ?? err.error?.title ?? 'Kunde inte kvittera kommentaren.',
        }));
        this.commentState.update(state => ({ ...state, acknowledging: false }));
      },
    });
  }
}
