import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpErrorResponse } from '@angular/common/http';
import {
  EventService, EventDto,
  EVENT_STATUS_LABEL, EVENT_STATUS_CHIP,
  REGISTRATION_KIND_LABEL,
} from 'shared';

@Component({
  selector: 'app-my-event-detail',
  standalone: true,
  imports: [
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
  private readonly route    = inject(ActivatedRoute);
  private readonly eventSvc = inject(EventService);
  private readonly fb       = inject(FormBuilder);

  readonly loading       = signal(true);
  readonly event         = signal<EventDto | null>(null);
  readonly savingDraft   = signal(false);
  readonly draftSaved    = signal(false);
  readonly draftError    = signal<string | null>(null);
  readonly submitting    = signal(false);
  readonly returning     = signal(false);
  readonly actionError   = signal<string | null>(null);

  readonly addingRequest  = signal(false);
  readonly requestSaved   = signal(false);
  readonly requestError   = signal<string | null>(null);

  readonly statusLabel = EVENT_STATUS_LABEL;
  readonly statusChip  = EVENT_STATUS_CHIP;
  readonly regKindLabel = REGISTRATION_KIND_LABEL;

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

  get eventId(): string {
    return this.route.snapshot.paramMap.get('id')!;
  }

  get isDraft(): boolean {
    return this.event()?.status === 'Draft';
  }

  get adminComment(): string | null {
    const ev = this.event();
    if (!ev) return null;
    const comments = [...ev.comments].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
    return comments[0]?.text ?? null;
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
}
