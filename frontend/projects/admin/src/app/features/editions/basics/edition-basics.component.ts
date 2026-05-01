import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { map } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ConventionService, EditionDto, PersonDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { FIELD } from '../../../labels/ui.labels';
import { HelpTooltipComponent } from '../../../../help/components/help-tooltip/help-tooltip.component';

@Component({
  selector: 'app-edition-basics',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    HelpTooltipComponent,
  ],
  templateUrl: './edition-basics.component.html',
  styleUrl: './edition-basics.component.scss',
})
export class EditionBasicsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly fb   = inject(FormBuilder);
  private readonly svc  = inject(ConventionService);

  readonly edition  = signal<EditionDto | null>(null);
  readonly persons  = signal<PersonDto[]>([]);
  readonly loading  = signal(true);
  readonly error    = signal<string | null>(null);
  readonly saving   = signal(false);
  readonly scheduleDayTimes = signal<Record<string, { startTime: string | null; endTime: string | null }>>({});

  readonly FIELD = FIELD;

  readonly form = this.fb.group({
    name:               ['', Validators.required],
    startDate:          ['', Validators.required],
    endDate:            ['', Validators.required],
    staffCoordinatorId: ['', Validators.required],
    eventCoordinatorId: ['', Validators.required],
  });

  readonly editionDayOptions = computed(() => {
    const e = this.edition();
    if (!e) return [] as { value: string; label: string }[];
    const start = new Date(`${e.start.substring(0, 10)}T00:00:00`);
    const end   = new Date(`${e.end.substring(0, 10)}T00:00:00`);
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || start > end) {
      return [] as { value: string; label: string }[];
    }
    const options: { value: string; label: string }[] = [];
    for (const cur = new Date(start); cur <= end; cur.setDate(cur.getDate() + 1)) {
      const value = `${cur.getFullYear()}-${String(cur.getMonth() + 1).padStart(2, '0')}-${String(cur.getDate()).padStart(2, '0')}`;
      const label = new Intl.DateTimeFormat('sv-SE', { weekday: 'long', day: 'numeric', month: 'long' }).format(cur);
      options.push({ value, label });
    }
    return options;
  });

  ngOnInit(): void {
    this.route.paramMap.pipe(map(p => p.get('id')!)).subscribe(id => this.loadData(id));
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.svc.getEdition(editionId).subscribe({
      next: e => { this.edition.set(e); this.syncForm(e); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchEdition); this.loading.set(false); },
    });
    this.svc.listPersons().subscribe({ next: p => this.persons.set(p.filter(x => x.isActive)) });
  }

  private reload(): void {
    this.svc.getEdition(this.edition()!.id).subscribe({
      next: e => { this.edition.set(e); this.syncForm(e); },
    });
  }

  private syncForm(e: EditionDto): void {
    this.form.setValue({
      name:               e.name,
      startDate:          e.start.substring(0, 10),
      endDate:            e.end.substring(0, 10),
      staffCoordinatorId: e.staffCoordinatorId ?? '',
      eventCoordinatorId: e.eventCoordinatorId ?? '',
    });
    this.scheduleDayTimes.set(Object.fromEntries(
      (e.scheduleDays ?? []).map(day => [
        day.date.substring(0, 10),
        { startTime: day.startTime?.substring(0, 5) ?? null, endTime: day.endTime?.substring(0, 5) ?? null },
      ])
    ));
  }

  scheduleStartTime(date: string): string { return this.scheduleDayTimes()[date]?.startTime ?? ''; }
  scheduleEndTime(date: string): string   { return this.scheduleDayTimes()[date]?.endTime ?? ''; }

  setScheduleStartTime(date: string, startTime: string): void {
    this.scheduleDayTimes.update(days => ({
      ...days,
      [date]: { startTime: startTime || null, endTime: days[date]?.endTime ?? null },
    }));
  }

  setScheduleEndTime(date: string, endTime: string): void {
    this.scheduleDayTimes.update(days => ({
      ...days,
      [date]: { startTime: days[date]?.startTime ?? null, endTime: endTime || null },
    }));
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    this.saving.set(true);
    this.svc.updateEdition(this.edition()!.id, {
      name:               v.name!,
      startDate:          v.startDate!,
      endDate:            v.endDate!,
      staffCoordinatorId: v.staffCoordinatorId!,
      eventCoordinatorId: v.eventCoordinatorId!,
      scheduleDays: this.editionDayOptions().map(day => ({
        date:      day.value,
        startTime: this.toApiTime(this.scheduleDayTimes()[day.value]?.startTime),
        endTime:   this.toApiTime(this.scheduleDayTimes()[day.value]?.endTime),
      })),
    }).subscribe({
      next: () => { this.reload(); this.saving.set(false); },
      error: (err) => { this.error.set(toContextErrorMessage(err, ERROR.updateEdition)); this.saving.set(false); },
    });
  }

  private toApiTime(value: string | null | undefined): string | null {
    if (!value) return null;
    return value.length === 5 ? `${value}:00` : value;
  }
}
