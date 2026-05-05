import { Component, inject, OnInit, signal } from '@angular/core';
import { map } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EditionContentService, EDITION_CONTENT_KEYS } from 'shared';

@Component({
  selector: 'app-edition-content',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <div class="page-header">
      <h1>Innehållsinställningar</h1>
    </div>

    @if (loading()) {
      <div class="spinner-container">
        <mat-spinner diameter="40" />
      </div>
    } @else {
      @if (error()) {
        <div class="error-banner">{{ error() }}</div>
      }

      <p class="hint">
        Texterna visas på konventets publika startsida. Lämna ett fält tomt för att använda
        standardtexten.
      </p>

      <form [formGroup]="form" (ngSubmit)="save()" class="content-form">
        <mat-form-field appearance="outline">
          <mat-label>Hero-rubrik</mat-label>
          <input matInput formControlName="heroTitle" maxlength="500" />
          <mat-hint>Visas som stor rubrik på startsidan. Exempel: "Välkommen till Spelkonvent 2027"</mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Hero-ingress</mat-label>
          <textarea matInput formControlName="heroIngress" rows="3" maxlength="500"></textarea>
          <mat-hint>Kort text under rubriken.</mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Hero-knapptext</mat-label>
          <input matInput formControlName="heroPrimaryActionLabel" maxlength="500" />
          <mat-hint>Exempel: "Se programmet"</mat-hint>
        </mat-form-field>

        <h2>Uppmaningsknappar</h2>

        <mat-form-field appearance="outline">
          <mat-label>Etikett – Besökarregistrering</mat-label>
          <input matInput formControlName="ctaVisitorLabel" maxlength="500" />
          <mat-hint>Exempel: "Bli besökare"</mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Etikett – Arrangörsregistrering</mat-label>
          <input matInput formControlName="ctaOrganiserLabel" maxlength="500" />
          <mat-hint>Exempel: "Arrangera ett evenemang"</mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Etikett – Funktionärsregistrering</mat-label>
          <input matInput formControlName="ctaStaffLabel" maxlength="500" />
          <mat-hint>Exempel: "Bli funktionär"</mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Beskrivning – Besökarregistrering</mat-label>
          <textarea matInput formControlName="ctaVisitorDescription" rows="2" maxlength="500"></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Beskrivning – Arrangörsregistrering</mat-label>
          <textarea matInput formControlName="ctaOrganiserDescription" rows="2" maxlength="500"></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Beskrivning – Funktionärsregistrering</mat-label>
          <textarea matInput formControlName="ctaStaffDescription" rows="2" maxlength="500"></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Knapptext (öppen) – Besökarregistrering</mat-label>
          <input matInput formControlName="ctaVisitorOpenLabel" maxlength="500" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Knapptext (öppen) – Arrangörsregistrering</mat-label>
          <input matInput formControlName="ctaOrganiserOpenLabel" maxlength="500" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Knapptext (öppen) – Funktionärsregistrering</mat-label>
          <input matInput formControlName="ctaStaffOpenLabel" maxlength="500" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Statusetikett (stängd) – Besökarregistrering</mat-label>
          <input matInput formControlName="ctaVisitorClosedLabel" maxlength="500" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Statusetikett (stängd) – Arrangörsregistrering</mat-label>
          <input matInput formControlName="ctaOrganiserClosedLabel" maxlength="500" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Statusetikett (stängd) – Funktionärsregistrering</mat-label>
          <input matInput formControlName="ctaStaffClosedLabel" maxlength="500" />
        </mat-form-field>

        <h2>Utvalda evenemang</h2>

        <mat-form-field appearance="outline">
          <mat-label>Sektionsrubrik</mat-label>
          <input matInput formControlName="featuredSectionTitle" maxlength="500" />
          <mat-hint>Exempel: "Utvalda evenemang"</mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Knapptext – visa hela programmet</mat-label>
          <input matInput formControlName="featuredViewAllLabel" maxlength="500" />
          <mat-hint>Exempel: "Visa hela programmet"</mat-hint>
        </mat-form-field>

        @if (saved()) {
          <div class="success-inline">
            <mat-icon>check_circle</mat-icon>
            Innehållet sparades.
          </div>
        }

        <div class="action-bar">
          <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving()">
            @if (saving()) { <mat-spinner diameter="20" /> } @else { Spara }
          </button>
        </div>
      </form>
    }
  `,
  styles: [`
    .page-header { margin-bottom: 1.5rem; }
    h1 { margin: 0; }
    h2 { margin: 1.5rem 0 0.5rem; font-size: 1rem; font-weight: 500; color: var(--brand-text-muted); }
    .hint { color: var(--brand-text-muted); margin-bottom: 1.5rem; }
    .content-form { display: flex; flex-direction: column; gap: 1rem; max-width: 640px; }
    .action-bar { display: flex; gap: 1rem; padding-top: 0.5rem; }
    .success-inline { display: flex; align-items: center; gap: 0.5rem; color: green; }
    mat-spinner { display: inline-block; }
  `],
})
export class EditionContentComponent implements OnInit {
  private readonly route          = inject(ActivatedRoute);
  private readonly fb             = inject(FormBuilder);
  private readonly contentSvc     = inject(EditionContentService);

  private editionId = '';

  readonly loading  = signal(true);
  readonly saving   = signal(false);
  readonly saved    = signal(false);
  readonly error    = signal<string | null>(null);

  readonly form = this.fb.group({
    heroTitle:         ['', Validators.maxLength(500)],
    heroIngress:       ['', Validators.maxLength(500)],
    heroPrimaryActionLabel: ['', Validators.maxLength(500)],
    ctaVisitorLabel:   ['', Validators.maxLength(500)],
    ctaOrganiserLabel: ['', Validators.maxLength(500)],
    ctaStaffLabel:     ['', Validators.maxLength(500)],
    ctaVisitorDescription: ['', Validators.maxLength(500)],
    ctaOrganiserDescription: ['', Validators.maxLength(500)],
    ctaStaffDescription: ['', Validators.maxLength(500)],
    ctaVisitorOpenLabel: ['', Validators.maxLength(500)],
    ctaOrganiserOpenLabel: ['', Validators.maxLength(500)],
    ctaStaffOpenLabel: ['', Validators.maxLength(500)],
    ctaVisitorClosedLabel: ['', Validators.maxLength(500)],
    ctaOrganiserClosedLabel: ['', Validators.maxLength(500)],
    ctaStaffClosedLabel: ['', Validators.maxLength(500)],
    featuredSectionTitle: ['', Validators.maxLength(500)],
    featuredViewAllLabel: ['', Validators.maxLength(500)],
  });

  ngOnInit(): void {
    this.route.paramMap.pipe(map(p => p.get('id')!)).subscribe(id => {
      this.editionId = id;
      this.loadContent(id);
    });
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    this.saved.set(false);
    this.error.set(null);

    const v = this.form.getRawValue();
    const items = [
      { key: EDITION_CONTENT_KEYS.heroTitle,         value: v.heroTitle ?? '' },
      { key: EDITION_CONTENT_KEYS.heroIngress,       value: v.heroIngress ?? '' },
      { key: EDITION_CONTENT_KEYS.heroPrimaryActionLabel, value: v.heroPrimaryActionLabel ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaVisitorLabel,   value: v.ctaVisitorLabel ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaOrganiserLabel, value: v.ctaOrganiserLabel ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaStaffLabel,     value: v.ctaStaffLabel ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaVisitorDescription, value: v.ctaVisitorDescription ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaOrganiserDescription, value: v.ctaOrganiserDescription ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaStaffDescription, value: v.ctaStaffDescription ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaVisitorOpenLabel, value: v.ctaVisitorOpenLabel ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaOrganiserOpenLabel, value: v.ctaOrganiserOpenLabel ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaStaffOpenLabel, value: v.ctaStaffOpenLabel ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaVisitorClosedLabel, value: v.ctaVisitorClosedLabel ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaOrganiserClosedLabel, value: v.ctaOrganiserClosedLabel ?? '' },
      { key: EDITION_CONTENT_KEYS.ctaStaffClosedLabel, value: v.ctaStaffClosedLabel ?? '' },
      { key: EDITION_CONTENT_KEYS.featuredSectionTitle, value: v.featuredSectionTitle ?? '' },
      { key: EDITION_CONTENT_KEYS.featuredViewAllLabel, value: v.featuredViewAllLabel ?? '' },
    ];

    this.contentSvc.setContent(this.editionId, items).subscribe({
      next: () => { this.saving.set(false); this.saved.set(true); },
      error: () => { this.saving.set(false); this.error.set('Det gick inte att spara innehållet. Försök igen.'); },
    });
  }

  private loadContent(editionId: string): void {
    this.loading.set(true);
    this.contentSvc.getContent(editionId).subscribe({
      next: items => {
        const byKey = Object.fromEntries(items.map(i => [i.key, i.value]));
        this.form.patchValue({
          heroTitle:         byKey[EDITION_CONTENT_KEYS.heroTitle]         ?? '',
          heroIngress:       byKey[EDITION_CONTENT_KEYS.heroIngress]       ?? '',
          heroPrimaryActionLabel: byKey[EDITION_CONTENT_KEYS.heroPrimaryActionLabel] ?? '',
          ctaVisitorLabel:   byKey[EDITION_CONTENT_KEYS.ctaVisitorLabel]   ?? '',
          ctaOrganiserLabel: byKey[EDITION_CONTENT_KEYS.ctaOrganiserLabel] ?? '',
          ctaStaffLabel:     byKey[EDITION_CONTENT_KEYS.ctaStaffLabel]     ?? '',
          ctaVisitorDescription: byKey[EDITION_CONTENT_KEYS.ctaVisitorDescription] ?? '',
          ctaOrganiserDescription: byKey[EDITION_CONTENT_KEYS.ctaOrganiserDescription] ?? '',
          ctaStaffDescription: byKey[EDITION_CONTENT_KEYS.ctaStaffDescription] ?? '',
          ctaVisitorOpenLabel: byKey[EDITION_CONTENT_KEYS.ctaVisitorOpenLabel] ?? '',
          ctaOrganiserOpenLabel: byKey[EDITION_CONTENT_KEYS.ctaOrganiserOpenLabel] ?? '',
          ctaStaffOpenLabel: byKey[EDITION_CONTENT_KEYS.ctaStaffOpenLabel] ?? '',
          ctaVisitorClosedLabel: byKey[EDITION_CONTENT_KEYS.ctaVisitorClosedLabel] ?? '',
          ctaOrganiserClosedLabel: byKey[EDITION_CONTENT_KEYS.ctaOrganiserClosedLabel] ?? '',
          ctaStaffClosedLabel: byKey[EDITION_CONTENT_KEYS.ctaStaffClosedLabel] ?? '',
          featuredSectionTitle: byKey[EDITION_CONTENT_KEYS.featuredSectionTitle] ?? '',
          featuredViewAllLabel: byKey[EDITION_CONTENT_KEYS.featuredViewAllLabel] ?? '',
        });
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Det gick inte att hämta innehållsinställningar.');
        this.loading.set(false);
      },
    });
  }
}
