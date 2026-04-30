import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { map } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, EditionDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../labels/pages.labels';
import { ACTION } from '../../../labels/ui.labels';
import { ConfirmDialogService } from '../../../shared/confirm-dialog/confirm-dialog.service';
import { EditionContextService } from '../../../services/edition-context.service';

@Component({
  selector: 'app-edition-lifecycle',
  standalone: true,
  imports: [
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './edition-lifecycle.component.html',
  styleUrl: './edition-lifecycle.component.scss',
})
export class EditionLifecycleComponent implements OnInit {
  private readonly route         = inject(ActivatedRoute);
  private readonly svc           = inject(ConventionService);
  private readonly confirmSvc    = inject(ConfirmDialogService);
  private readonly editionContext = inject(EditionContextService);

  readonly edition  = signal<EditionDto | null>(null);
  readonly loading  = signal(true);
  readonly error    = signal<string | null>(null);
  readonly saving   = signal(false);

  readonly PAGE   = EDITION_DETAIL;
  readonly ACTION = ACTION;

  readonly isDraft      = computed(() => this.edition()?.status === 'Draft');
  readonly isPublished  = computed(() => this.edition()?.status === 'Published');
  readonly isActiveEdition = computed(() => {
    const id = this.edition()?.id;
    return !!id && id === this.editionContext.activeEdition()?.id;
  });

  readonly registrationTypes: { type: 'organiser' | 'staff' | 'visitor'; label: string }[] = [
    { type: 'organiser', label: 'Arrangörsregistrering' },
    { type: 'staff',     label: 'Staffregistrering' },
    { type: 'visitor',   label: 'Besökarregistrering' },
  ];

  readonly registrationTypeLabels: Record<'organiser' | 'staff' | 'visitor', string> = {
    organiser: this.PAGE.organiserSubLabel,
    staff:     this.PAGE.staffSubLabel,
    visitor:   this.PAGE.visitorSubLabel,
  };

  readonly registrationPillLabels: Record<'organiser' | 'staff' | 'visitor', string> = {
    organiser: 'Arrangör',
    staff:     'Funktionär',
    visitor:   'Besökare',
  };

  readonly openRegistrationCount = computed(() =>
    this.registrationTypes.filter(t => this.registrationOpen(t.type)).length
  );

  ngOnInit(): void {
    this.editionContext.load();
    this.route.paramMap.pipe(map(p => p.get('id')!)).subscribe(id => this.loadData(id));
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.svc.getEdition(editionId).subscribe({
      next: e => { this.edition.set(e); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchEdition); this.loading.set(false); },
    });
  }

  private reload(): void {
    this.svc.getEdition(this.edition()!.id).subscribe({ next: e => this.edition.set(e) });
  }

  registrationOpen(type: 'organiser' | 'staff' | 'visitor'): boolean {
    const e = this.edition();
    if (!e) return false;
    return { organiser: e.organiserRegistrationOpen, staff: e.staffRegistrationOpen, visitor: e.visitorRegistrationOpen }[type];
  }

  registrationStatusIcon(type: 'organiser' | 'staff' | 'visitor'): string {
    return this.registrationOpen(type) ? 'lock_open' : 'lock';
  }

  setActive(): void {
    this.saving.set(true);
    const editionId = this.edition()!.id;
    this.svc.setActiveEdition(editionId).subscribe({
      next: () => { this.editionContext.setActive(editionId); this.saving.set(false); },
      error: (err) => { this.error.set(toContextErrorMessage(err, ERROR.setActiveEdition)); this.saving.set(false); },
    });
  }

  publish(): void {
    this.confirmSvc.confirm({
      title:        this.PAGE.publishConfirmTitle,
      message:      this.PAGE.publishConfirmMessage,
      confirmLabel: this.PAGE.publishAction,
    }).subscribe(confirmed => {
      if (!confirmed) return;
      this.saving.set(true);
      this.svc.publishEdition(this.edition()!.id).subscribe({
        next: () => { this.reload(); this.saving.set(false); },
        error: (err) => { this.error.set(toContextErrorMessage(err, ERROR.publishEdition)); this.saving.set(false); },
      });
    });
  }

  unpublish(): void {
    this.confirmSvc.confirm({
      title:        this.PAGE.unpublishConfirmTitle,
      message:      this.PAGE.unpublishConfirmMessage,
      confirmLabel: this.PAGE.unpublishAction,
    }).subscribe(confirmed => {
      if (!confirmed) return;
      this.saving.set(true);
      this.svc.unpublishEdition(this.edition()!.id).subscribe({
        next: () => { this.reload(); this.saving.set(false); },
        error: (err) => { this.error.set(toContextErrorMessage(err, ERROR.unpublishEdition)); this.saving.set(false); },
      });
    });
  }

  toggleRegistration(type: 'organiser' | 'staff' | 'visitor'): void {
    if (!this.isPublished()) return;
    this.saving.set(true);
    const open = this.registrationOpen(type);
    const call = open
      ? this.svc.closeRegistration(this.edition()!.id, type)
      : this.svc.openRegistration(this.edition()!.id, type);
    call.subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => { this.error.set(toContextErrorMessage(err, ERROR.toggleRegistration)); this.saving.set(false); },
    });
  }
}
