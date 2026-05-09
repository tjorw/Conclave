import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule } from '@angular/material/radio';
import { SessionDto } from 'shared';
import { EVENT_DETAIL } from '../../../labels/pages.labels';

export interface AssignSessionDialogData {
  sessions: SessionDto[];
}

@Component({
  selector: 'app-assign-session-dialog',
  standalone: true,
  imports: [DatePipe, MatDialogModule, MatButtonModule, MatRadioModule],
  template: `
    <h2 mat-dialog-title>{{ PAGE.assignSessionDialogTitle }}</h2>
    <mat-dialog-content>
      @if (activeSessions().length === 0) {
        <p class="empty-state">{{ PAGE.assignSessionDialogEmpty }}</p>
      } @else {
        <mat-radio-group class="session-radio-group" [value]="selectedId()" (change)="selectedId.set($event.value)">
          @for (s of activeSessions(); track s.id) {
            <mat-radio-button [value]="s.id" class="session-radio-option">
              <span class="session-radio-label">
                <span class="session-time">{{ s.start | date:'yyyy-MM-dd HH:mm' }} – {{ s.end | date:'HH:mm' }}</span>
                @if (s.venueName) {
                  <span class="session-venue">{{ s.venueName }}</span>
                }
              </span>
            </mat-radio-button>
          }
        </mat-radio-group>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ PAGE.assignSessionDialogCancel }}</button>
      <button mat-flat-button color="primary"
        [disabled]="!selectedId()"
        (click)="confirm()">
        {{ PAGE.assignSessionDialogConfirm }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .session-radio-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
      min-width: 340px;
    }
    .session-radio-option { display: flex; }
    .session-radio-label {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .session-time { font-size: 0.9rem; }
    .session-venue { font-size: 0.78rem; color: var(--c-text-subtle, #666); }
    .empty-state { color: var(--c-text-subtle, #666); font-size: 0.9rem; }
  `],
})
export class AssignSessionDialogComponent {
  protected readonly PAGE = EVENT_DETAIL;
  private readonly ref = inject(MatDialogRef<AssignSessionDialogComponent>);
  private readonly data = inject<AssignSessionDialogData>(MAT_DIALOG_DATA);

  readonly selectedId = signal<string | null>(null);
  readonly activeSessions = signal(
    this.data.sessions.filter(s => s.status === 'Active').sort((a, b) => a.start.localeCompare(b.start))
  );

  confirm(): void {
    const id = this.selectedId();
    if (id) this.ref.close(id);
  }
}
