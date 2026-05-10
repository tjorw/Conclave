import {
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { createSearchStream } from '../../shared/search-stream';
import {
  PersonSearchResultDto,
  PersonTicketDto,
  VisitorTicketTypeDto,
} from '../../models/reception.models';
import { EditionContextService } from '../../services/edition-context.service';
import { ReceptionService } from '../../services/reception.service';

type WalkupStep = 'person' | 'tickettype' | 'confirm' | 'done';

@Component({
  selector: 'app-walkup',
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
  ],
  templateUrl: './walkup.component.html',
  styleUrl: './walkup.component.scss',
})
export class WalkupComponent implements OnInit {
  private readonly receptionService = inject(ReceptionService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly editionContext = inject(EditionContextService);

  readonly step = signal<WalkupStep>('person');

  // Steg 1: Personsökning
  readonly searchControl = new FormControl('');
  readonly searching = signal(false);
  readonly searchResults = signal<PersonSearchResultDto[]>([]);
  readonly showCreateForm = signal(false);
  readonly noSearchResults = computed(
    () =>
      !this.searching() &&
      (this.searchControl.value?.trim().length ?? 0) >= 2 &&
      this.searchResults().length === 0 &&
      !this.showCreateForm()
  );

  readonly createForm = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(2)]),
    email: new FormControl('', [Validators.required, Validators.email]),
    phone: new FormControl(''),
  });
  readonly creating = signal(false);

  // Vald person
  readonly selectedPerson = signal<{ id: string; name: string; email: string } | null>(null);

  // Steg 2: Biljetttyp
  readonly ticketTypes = signal<VisitorTicketTypeDto[]>([]);
  readonly loadingTicketTypes = signal(false);
  readonly selectedTicketType = signal<VisitorTicketTypeDto | null>(null);

  // Steg 3+4: Registrering
  readonly registering = signal(false);
  readonly registerError = signal<string | null>(null);
  readonly completedTicketId = signal<string | null>(null);
  readonly completedDescription = signal<string | null>(null);

  ngOnInit(): void {
    createSearchStream(
      this.searchControl,
      term => {
        const editionId = this.editionContext.activeEdition()?.id;
        if (!editionId) return of(null);
        this.searching.set(true);
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
      error: () => this.searching.set(false),
    });
  }

  selectExistingPerson(person: PersonSearchResultDto): void {
    this.selectedPerson.set({ id: person.personId, name: person.name, email: person.email });
    this.goToTicketType();
  }

  submitCreatePerson(): void {
    if (this.createForm.invalid) return;
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId) return;

    const { name, email, phone } = this.createForm.value;
    this.creating.set(true);

    this.receptionService.createWalkupPerson(editionId, name!, email!, phone ?? null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.creating.set(false);
          this.selectedPerson.set({ id: res.id, name: name!, email: email! });
          this.goToTicketType();
        },
        error: (err) => {
          this.creating.set(false);
          const msg = err?.error?.message ?? err?.error ?? 'Kunde inte skapa person.';
          this.snackBar.open(typeof msg === 'string' ? msg : 'Kunde inte skapa person.', 'OK', { duration: 6000 });
        },
      });
  }

  private goToTicketType(): void {
    const editionId = this.editionContext.activeEdition()?.id;
    if (!editionId) return;

    this.step.set('tickettype');
    this.loadingTicketTypes.set(true);

    this.receptionService.listWalkupTicketTypes(editionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: types => {
          this.ticketTypes.set(types);
          this.loadingTicketTypes.set(false);
        },
        error: () => {
          this.loadingTicketTypes.set(false);
          this.snackBar.open('Kunde inte ladda biljetttyper.', 'OK', { duration: 4000 });
        },
      });
  }

  selectTicketType(tt: VisitorTicketTypeDto): void {
    this.selectedTicketType.set(tt);
    this.step.set('confirm');
    this.registerError.set(null);
  }

  register(): void {
    const editionId = this.editionContext.activeEdition()?.id;
    const person = this.selectedPerson();
    const ticketType = this.selectedTicketType();
    if (!editionId || !person || !ticketType) return;

    this.registering.set(true);
    this.registerError.set(null);

    this.receptionService.walkupRegister(editionId, person.id, ticketType.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.receptionService.collectTicket(res.ticketId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: () => {
                this.completedTicketId.set(res.ticketId);
                // Hämta biljetten för att visa förmåner
                this.receptionService.getPersonTickets(person.id, editionId)
                  .pipe(takeUntilDestroyed(this.destroyRef))
                  .subscribe({
                    next: tickets => {
                      const ticket = tickets.find(t => t.ticketId === res.ticketId);
                      this.completedDescription.set(ticket?.description ?? null);
                      this.registering.set(false);
                      this.step.set('done');
                    },
                    error: () => {
                      this.completedDescription.set(null);
                      this.registering.set(false);
                      this.step.set('done');
                    },
                  });
              },
              error: () => {
                this.registering.set(false);
                this.registerError.set('Registrering lyckades men incheckning misslyckades. Checka in manuellt i incheckningsflödet.');
              },
            });
        },
        error: (err) => {
          this.registering.set(false);
          const detail = err?.error ?? 'Registrering misslyckades.';
          this.registerError.set(typeof detail === 'string' ? detail : 'Registrering misslyckades.');
        },
      });
  }

  reset(): void {
    this.step.set('person');
    this.searchControl.setValue('');
    this.searchResults.set([]);
    this.showCreateForm.set(false);
    this.createForm.reset();
    this.selectedPerson.set(null);
    this.selectedTicketType.set(null);
    this.ticketTypes.set([]);
    this.completedTicketId.set(null);
    this.completedDescription.set(null);
    this.registerError.set(null);
  }

  backToPerson(): void {
    this.step.set('person');
    this.selectedTicketType.set(null);
  }

  backToTicketType(): void {
    this.step.set('tickettype');
    this.registerError.set(null);
  }
}
