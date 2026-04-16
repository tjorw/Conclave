import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { ACTION } from '../../labels/ui.labels';

export interface ConfirmDialogData {
  title:         string;
  message:       string;
  confirmLabel?: string;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>{{ data.message }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">{{ ACTION.cancel }}</button>
      <button mat-button color="warn" [mat-dialog-close]="true" cdkFocusInitial>
        {{ data.confirmLabel ?? ACTION.confirm }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialogComponent {
  readonly data   = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  readonly ACTION = ACTION;
}
