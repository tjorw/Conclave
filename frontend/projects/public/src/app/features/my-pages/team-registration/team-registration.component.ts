import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EventFeedDto, FeedService, RegistrationService, toErrorMessage } from 'shared';

@Component({
  selector: 'app-team-registration',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './team-registration.component.html',
  styleUrl: './team-registration.component.scss',
})
export class TeamRegistrationComponent implements OnInit {
  private readonly route   = inject(ActivatedRoute);
  private readonly feedSvc = inject(FeedService);
  private readonly regSvc  = inject(RegistrationService);
  private readonly fb      = inject(FormBuilder);

  readonly event   = signal<EventFeedDto | null>(null);
  readonly loading = signal(true);
  readonly saving  = signal(false);
  readonly error   = signal<string | null>(null);
  readonly success = signal(false);

  readonly form = this.fb.group({
    teamName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
  });

  ngOnInit(): void {
    const eventId = this.route.snapshot.paramMap.get('eventId')!;
    this.feedSvc.getEvent(eventId).subscribe({
      next: ev => { this.event.set(ev); this.loading.set(false); },
      error: () => { this.error.set('Evenemanget hittades inte.'); this.loading.set(false); },
    });
  }

  submit(): void {
    const ev = this.event();
    if (!ev || this.form.invalid || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);
    const { teamName } = this.form.getRawValue();
    this.regSvc.registerTeamForEvent(ev.id, ev.editionId, teamName!).subscribe({
      next: () => { this.saving.set(false); this.success.set(true); },
      error: err => {
        this.error.set(toErrorMessage(err, 'Laganmälan misslyckades. Försök igen.'));
        this.saving.set(false);
      },
    });
  }

  teamSizeHint(): string {
    const ev = this.event();
    if (!ev || ev.registrationMode !== 'Team') return '';
    const min = ev.minTeamSize;
    const max = ev.maxTeamSize;
    if (min && max) return `${min}–${max} deltagare`;
    if (min) return `Minst ${min} deltagare`;
    if (max) return `Upp till ${max} deltagare`;
    return '';
  }
}
