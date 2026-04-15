import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
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

  readonly loading       = signal(true);
  readonly event         = signal<EventDto | null>(null);
  readonly savingDraft   = signal(false);
  readonly draftSaved    = signal(false);
  readonly draftError    = signal<string | null>(null);
  readonly submitting    = signal(false);
  readonly returning     = signal(false);
  readonly deleting      = signal(false);
  readonly actionError   = signal<string | null>(null);

  readonly addingRequest  = signal(false);
  readonly requestSaved   = signal(false);
  readonly requestError   = signal<string | null>(null);
  readonly addingComment  = signal(false);
  readonly commentSaved   = signal(false);
  readonly commentError   = signal<string | null>(null);
  readonly acknowledging  = signal(false);

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

  get adminComment(): string | null {
    const ev = this.event();
    if (!ev) return null;
    const adminComments = ev.comments
      .filter(c => !c.requiresHandling)
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    return adminComments[0]?.text ?? null;
  }

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
    this.eventSvc.getEvent(this.eventId).subscribe({
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
    if (this.draftForm.invalid || this.savingDraft()) return;
    this.savingDraft.set(true);
    this.draftSaved.set(false);
    this.draftError.set(null);
    const { title, description, registrationType, dropInRules } = this.draftForm.getRawValue();
    this.eventSvc.updateDraft(
      this.eventId, title!, description!, registrationType!, dropInRules || null
    ).subscribe({
      next: () => { this.savingDraft.set(false); this.draftSaved.set(true); },
      error: (err: HttpErrorResponse) => {
        this.draftError.set(err.error?.detail ?? err.error?.title ?? 'Kunde inte spara utkastet.');
        this.savingDraft.set(false);
      },
    });
  }

  addSessionRequest(): void {
    if (this.requestForm.invalid || this.addingRequest()) return;
    this.addingRequest.set(true);
    this.requestSaved.set(false);
    this.requestError.set(null);
    const { description, durationMinutes, seats, startType } = this.requestForm.getRawValue();
    this.eventSvc.addSessionRequest(
      this.eventId, description!, durationMinutes!, seats!, startType!
    ).subscribe({
      next: () => {
        this.addingRequest.set(false);
        this.requestSaved.set(true);
        this.requestForm.reset({ description: '', durationMinutes: 60, seats: 20, startType: 'FixedTime' });
        this.loadEvent();
      },
      error: (err: HttpErrorResponse) => {
        this.requestError.set(err.error?.detail ?? err.error?.title ?? 'Kunde inte lägga till sessionönskemål.');
        this.addingRequest.set(false);
      },
    });
  }

  removeSessionRequest(requestId: string): void {
    this.eventSvc.removeSessionRequest(this.eventId, requestId).subscribe({
      next: () => this.loadEvent(),
    });
  }

  submitForReview(): void {
    if (this.submitting()) return;
    this.submitting.set(true);
    this.actionError.set(null);
    this.eventSvc.submitForReview(this.eventId).subscribe({
      next: () => { this.submitting.set(false); this.loadEvent(); },
      error: (err: HttpErrorResponse) => {
        this.actionError.set(err.error?.detail ?? err.error?.title ?? 'Kunde inte skicka in arrangemanget.');
        this.submitting.set(false);
      },
    });
  }

  returnToDraft(): void {
    if (this.returning()) return;
    this.returning.set(true);
    this.actionError.set(null);
    this.eventSvc.returnToDraft(this.eventId).subscribe({
      next: () => { this.returning.set(false); this.loadEvent(); },
      error: (err: HttpErrorResponse) => {
        this.actionError.set(err.error?.detail ?? err.error?.title ?? 'Kunde inte återgå till utkast.');
        this.returning.set(false);
      },
    });
  }

  deleteEvent(): void {
    if (this.deleting() || !confirm('Ta bort arrangemanget permanent?')) return;
    this.deleting.set(true);
    this.actionError.set(null);
    this.eventSvc.deleteEvent(this.eventId).subscribe({
      next: () => this.router.navigate(['/mina-sidor/arrangemang']),
      error: (err: HttpErrorResponse) => {
        this.actionError.set(err.error?.detail ?? err.error?.title ?? 'Kunde inte ta bort arrangemanget.');
        this.deleting.set(false);
      },
    });
  }

  addChangeComment(): void {
    if (this.commentForm.invalid || this.addingComment()) return;
    this.addingComment.set(true);
    this.commentSaved.set(false);
    this.commentError.set(null);

    const text = this.commentForm.getRawValue().text!;
    this.eventSvc.addEventComment(this.eventId, text).subscribe({
      next: () => {
        this.addingComment.set(false);
        this.commentSaved.set(true);
        this.commentForm.reset({ text: '' });
        this.loadEvent();
      },
      error: (err: HttpErrorResponse) => {
        this.commentError.set(err.error?.detail ?? err.error?.title ?? 'Kunde inte skicka ändringsförslaget.');
        this.addingComment.set(false);
      },
    });
  }

  acknowledgeComment(commentId: string): void {
    if (this.acknowledging()) return;
    this.acknowledging.set(true);
    this.actionError.set(null);

    this.eventSvc.acknowledgeEventComment(this.eventId, commentId).subscribe({
      next: () => {
        this.acknowledging.set(false);
        this.loadEvent();
      },
      error: (err: HttpErrorResponse) => {
        this.actionError.set(err.error?.detail ?? err.error?.title ?? 'Kunde inte kvittera kommentaren.');
        this.acknowledging.set(false);
      },
    });
  }
}
