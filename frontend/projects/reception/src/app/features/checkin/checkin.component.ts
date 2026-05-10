import {
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { createSearchStream } from '../../shared/search-stream';
import { PersonScheduleDto, PersonSearchResultDto, PersonTicketDto } from '../../models/reception.models';
import { EditionContextService } from '../../services/edition-context.service';
import { ReceptionService } from '../../services/reception.service';
import { SchedulePanelComponent } from './schedule-panel.component';
import { TicketCardComponent } from './ticket-card.component';
import { QrScannerComponent } from './qr-scanner.component';

@Component({
  selector: 'app-checkin',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    TicketCardComponent,
    SchedulePanelComponent,
    QrScannerComponent,
  ],
  templateUrl: './checkin.component.html',
  styleUrl: './checkin.component.scss',
})
export class CheckinComponent implements OnInit {
  private readonly receptionService = inject(ReceptionService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly editionContext = inject(EditionContextService);

  readonly searchControl = new FormControl('');

  readonly searching = signal(false);
  readonly searchResults = signal<PersonSearchResultDto[]>([]);
  readonly selectedPerson = signal<PersonSearchResultDto | null>(null);
  readonly personTickets = signal<PersonTicketDto[] | null>(null);
  readonly personSchedule = signal<PersonScheduleDto | null>(null);
  readonly loadingTickets = signal(false);
  readonly collectingTicketId = signal<string | null>(null);
  readonly showQrScanner = signal(false);

  readonly hasResults = computed(() => this.searchResults().length > 0);
  readonly noResults = computed(
    () => !this.searching() && (this.searchControl.value?.trim().length ?? 0) >= 2 && this.searchResults().length === 0
  );

  ngOnInit(): void {
    createSearchStream(
      this.searchControl,
      term => {
        const editionId = this.editionContext.activeEdition()?.id;
        if (!editionId) return of([]);
        this.searching.set(true);
        this.selectedPerson.set(null);
        this.personTickets.set(null);
        this.personSchedule.set(null);
        return this.receptionService.searchPersons(editionId, term);
      }
    ).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: results => {
        this.searching.set(false);
        if (results === null) {
          this.searchResults.set([]);
        } else {
          this.searchResults.set(results ?? []);
        }
      },
      error: () => {
        this.searching.set(false);
        this.snackBar.open('Sökning misslyckades.', 'OK', { duration: 4000 });
      },
    });
  }

  selectPerson(person: PersonSearchResultDto): void {
    if (this.selectedPerson()?.personId === person.personId) {
      this.selectedPerson.set(null);
      this.personTickets.set(null);
      this.personSchedule.set(null);
      return;
    }
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId) return;

    this.selectedPerson.set(person);
    this.personTickets.set(null);
    this.personSchedule.set(null);
    this.loadingTickets.set(true);

    this.receptionService.getPersonTickets(person.personId, editionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: tickets => {
          this.personTickets.set(tickets);
          this.loadingTickets.set(false);
        },
        error: () => {
          this.loadingTickets.set(false);
          this.snackBar.open('Kunde inte ladda biljetter.', 'OK', { duration: 4000 });
        },
      });

    this.receptionService.getPersonSchedule(person.personId, editionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: schedule => this.personSchedule.set(schedule),
        error: () => { /* schema visas inte om det misslyckas */ },
      });
  }

  collectTicket(ticketId: string): void {
    this.collectingTicketId.set(ticketId);
    this.receptionService.collectTicket(ticketId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.collectingTicketId.set(null);
          this.snackBar.open('Biljett incheckad!', '', { duration: 3000 });
          // Uppdatera biljetter för vald person
          const person = this.selectedPerson();
          const editionId = this.editionContext.activeEdition()?.id;
          if (person && editionId) {
            this.receptionService.getPersonTickets(person.personId, editionId)
              .pipe(takeUntilDestroyed(this.destroyRef))
              .subscribe({ next: tickets => this.personTickets.set(tickets) });
          }
        },
        error: () => {
          this.collectingTicketId.set(null);
          this.snackBar.open('Incheckning misslyckades.', 'OK', { duration: 5000 });
        },
      });
  }

  onQrScanned(ticketId: string): void {
    this.showQrScanner.set(false);
    this.searchControl.setValue(ticketId);
  }

  clearSearch(): void {
    this.searchControl.setValue('');
    this.searchResults.set([]);
    this.selectedPerson.set(null);
    this.personTickets.set(null);
    this.personSchedule.set(null);
  }
}
