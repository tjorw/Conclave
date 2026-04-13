import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Subscription } from 'rxjs';

function toDateTimeLocal(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

@Component({
  selector: 'app-date-time-range',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule],
  template: `
    <mat-form-field appearance="outline">
      <mat-label>Start</mat-label>
      <input matInput type="datetime-local" [formControl]="startCtrl"
             [attr.min]="min || null" [attr.max]="max || null" />
    </mat-form-field>
    <mat-form-field appearance="outline">
      <mat-label>Slut</mat-label>
      <input matInput type="datetime-local" [formControl]="endCtrl"
             [attr.min]="endMin" [attr.max]="max || null" />
    </mat-form-field>
  `,
  styles: [`:host { display: contents; } mat-form-field { min-width: 160px; flex: 1; }`],
})
export class DateTimeRangeComponent implements OnInit, OnDestroy {
  @Input({ required: true }) group!: FormGroup;
  @Input() startKey = 'startTime';
  @Input() endKey   = 'endTime';
  /** Används för krav 1: auto-sätt sluttid om den är tom. */
  @Input() defaultDurationMinutes = 60;
  /** ISO-sträng 'YYYY-MM-DDTHH:MM' – begränsar valbara datum (krav 3). */
  @Input() min?: string;
  @Input() max?: string;

  private sub?: Subscription;
  private previousStart: string | null = null;

  get startCtrl(): FormControl { return this.group.get(this.startKey) as FormControl; }
  get endCtrl():   FormControl { return this.group.get(this.endKey)   as FormControl; }
  /** Sluttiden kan aldrig väljas före aktuell starttid. */
  get endMin(): string | null  { return this.startCtrl.value || this.min || null; }

  ngOnInit(): void {
    this.previousStart = this.startCtrl.value || null;

    this.sub = this.startCtrl.valueChanges.subscribe((newStart: string) => {
      if (!newStart) { this.previousStart = null; return; }

      const currentEnd = this.endCtrl.value as string | null;

      if (!currentEnd) {
        // Krav 1: sluttid ej satt → sätt start + defaultDuration
        const autoEnd = new Date(new Date(newStart).getTime() + this.defaultDurationMinutes * 60_000);
        this.endCtrl.setValue(toDateTimeLocal(autoEnd), { emitEvent: false });
      } else if (this.previousStart) {
        // Krav 2: sluttid satt → bibehåll duration
        const duration = new Date(currentEnd).getTime() - new Date(this.previousStart).getTime();
        const newEnd   = new Date(new Date(newStart).getTime() + duration);
        this.endCtrl.setValue(toDateTimeLocal(newEnd), { emitEvent: false });
      }

      this.previousStart = newStart;
    });
  }

  ngOnDestroy(): void { this.sub?.unsubscribe(); }
}
