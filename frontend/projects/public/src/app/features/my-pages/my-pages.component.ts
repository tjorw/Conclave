import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { EditionService } from '../../services/edition.service';
import {
  AuthService,
  EventService,
  EventSummaryDto,
  MyVisitorRegistrationDto,
  MySessionRegistrationSummaryDto,
  MyStaffApplicationDto,
  RegistrationService,
} from 'shared';

@Component({
  selector: 'app-my-pages',
  standalone: true,
  imports: [DatePipe, RouterLink, MatProgressSpinnerModule],
  templateUrl: './my-pages.component.html',
  styleUrl: './my-pages.component.scss',
})
export class MyPagesComponent implements OnInit {
  private readonly editionSvc = inject(EditionService);
  private readonly eventSvc   = inject(EventService);
  private readonly regSvc     = inject(RegistrationService);
  private readonly authSvc    = inject(AuthService);

  readonly loading       = signal(true);
  readonly userName      = signal<string | null>(null);
  readonly myEvents      = signal<EventSummaryDto[]>([]);
  readonly myTickets     = signal<MyVisitorRegistrationDto[]>([]);
  readonly mySessions    = signal<MySessionRegistrationSummaryDto[]>([]);
  readonly myApplication = signal<MyStaffApplicationDto | null>(null);

  ngOnInit(): void {
    const editionId = this.editionSvc.editionId();
    if (!editionId) {
      this.loading.set(false);
      return;
    }

    forkJoin({
      profile:     this.authSvc.getProfile().pipe(catchError(() => of(null))),
      events:      this.eventSvc.getMyEvents(editionId).pipe(catchError(() => of([]))),
      tickets:     this.regSvc.getMyVisitorRegistration(editionId).pipe(catchError(() => of([] as MyVisitorRegistrationDto[]))),
      sessions:    this.regSvc.getMySessionRegistrations(editionId).pipe(catchError(() => of([]))),
      application: this.regSvc.getMyStaffApplication(editionId).pipe(catchError(() => of(null))),
    }).subscribe(result => {
      this.userName.set(result.profile?.name ?? null);
      this.myEvents.set(result.events);
      this.myTickets.set(result.tickets);
      this.mySessions.set([...result.sessions].sort((a, b) =>
        new Date(a.start).getTime() - new Date(b.start).getTime()
      ));
      this.myApplication.set(result.application);
      this.loading.set(false);
    });
  }

  get pendingCommentCount(): number {
    return this.myEvents().reduce((sum, e) => sum + (e.pendingCommentCount ?? 0), 0);
  }

  latestTicketPriceLabel(): string {
    const latest = this.myTickets()[0]?.ticketPrice ?? null;
    return this.priceLabel(latest);
  }

  totalTicketPriceLabel(): string {
    const total = this.myTickets().reduce((sum, ticket) => sum + (ticket.ticketPrice ?? 0), 0);
    return this.priceLabel(total);
  }

  private priceLabel(priceInOre: number | null): string {
    if (priceInOre === null) {
      return 'Ej angivet';
    }

    return new Intl.NumberFormat('sv-SE', {
      style: 'currency',
      currency: 'SEK',
      maximumFractionDigits: 0,
    }).format(priceInOre / 100);
  }
}
