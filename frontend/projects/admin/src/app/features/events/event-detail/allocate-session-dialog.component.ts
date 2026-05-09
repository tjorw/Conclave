import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { SessionDto } from 'shared';
import { EVENT_DETAIL } from '../../../labels/pages.labels';

export interface AllocateSessionDialogData {
  session: SessionDto;
}

export interface AllocateSessionDialogResult {
  strategy: string;
}

@Component({
  selector: 'app-allocate-session-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title>{{ PAGE.allocateDialogTitle }}</h2>
    <mat-dialog-content>
      <p class="hint">{{ PAGE.allocateDialogHint }}</p>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ PAGE.allocateStrategyField }}</mat-label>
          <mat-select formControlName="strategy">
            @for (s of strategies; track s.value) {
              <mat-option [value]="s.value">{{ s.label }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ PAGE.allocateCancel }}</button>
      <button mat-flat-button color="primary"
        [disabled]="form.invalid"
        (click)="confirm()">
        {{ PAGE.allocateConfirm }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .hint { font-size: 0.88rem; color: var(--c-text-subtle, #666); margin-bottom: 16px; }
    .form { min-width: 320px; }
    .full-width { width: 100%; }
  `],
})
export class AllocateSessionDialogComponent {
  protected readonly PAGE = EVENT_DETAIL;

  private readonly ref = inject(MatDialogRef<AllocateSessionDialogComponent>);
  private readonly fb = inject(FormBuilder);

  readonly strategies = [
    { value: 'FirstComeFirstServed', label: 'Först till kvarn' },
    { value: 'Lottery',              label: 'Lottning' },
    { value: 'Manual',               label: 'Manuell (lämnar resterande i kö)' },
  ];

  readonly form = this.fb.group({
    strategy: ['FirstComeFirstServed', Validators.required],
  });

  confirm(): void {
    if (this.form.invalid) return;
    this.ref.close({ strategy: this.form.getRawValue().strategy! } satisfies AllocateSessionDialogResult);
  }
}
