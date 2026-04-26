import { Component, input, output, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { DatePipe } from '@angular/common';
import { PersonTicketDto } from '../../models/reception.models';

@Component({
  selector: 'app-ticket-card',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule,
    DatePipe,
  ],
  templateUrl: './ticket-card.component.html',
  styleUrl: './ticket-card.component.scss',
})
export class TicketCardComponent {
  readonly ticket = input.required<PersonTicketDto>();
  readonly collecting = input(false);
  readonly collect = output<string>();

  readonly confirmMode = signal(false);

  get canCollect(): boolean {
    return this.ticket().status === 'Paid';
  }

  get statusLabel(): string {
    const map: Record<string, string> = {
      Reserved: 'Reserverad',
      Paid: 'Betald',
      Collected: 'Hämtad',
      Cancelled: 'Avbokad',
      Revoked: 'Återkallad',
    };
    return map[this.ticket().status] ?? this.ticket().status;
  }

  get statusClass(): string {
    const map: Record<string, string> = {
      Reserved: 'status--reserved',
      Paid: 'status--paid',
      Collected: 'status--collected',
      Cancelled: 'status--cancelled',
      Revoked: 'status--revoked',
    };
    return map[this.ticket().status] ?? '';
  }

  get categoryLabel(): string {
    const map: Record<string, string> = {
      Visitor: 'Besökare',
      Organiser: 'Arrangör',
      Staff: 'Funktionär',
    };
    return map[this.ticket().ticketTypeCategory] ?? this.ticket().ticketTypeCategory;
  }

  requestCollect(): void {
    this.confirmMode.set(true);
  }

  confirmCollect(): void {
    this.confirmMode.set(false);
    this.collect.emit(this.ticket().ticketId);
  }

  cancelConfirm(): void {
    this.confirmMode.set(false);
  }
}
