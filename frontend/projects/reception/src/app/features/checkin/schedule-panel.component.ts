import { Component, input, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { PersonScheduleDto } from '../../models/reception.models';

@Component({
  selector: 'app-schedule-panel',
  standalone: true,
  imports: [DatePipe, MatIconModule],
  templateUrl: './schedule-panel.component.html',
  styleUrl: './schedule-panel.component.scss',
})
export class SchedulePanelComponent {
  readonly schedule = input.required<PersonScheduleDto>();

  readonly hasContent = computed(() => {
    const s = this.schedule();
    return s.shifts.length > 0 || s.sessions.length > 0;
  });

  formatHours(hours: number): string {
    const h = Math.floor(hours);
    const m = Math.round((hours - h) * 60);
    if (m === 0) return `${h} h`;
    return `${h} h ${m} min`;
  }

  statusLabel(status: string): string {
    const map: Record<string, string> = {
      Assigned: 'Tilldelad',
      Confirmed: 'Bekräftad',
    };
    return map[status] ?? status;
  }
}
