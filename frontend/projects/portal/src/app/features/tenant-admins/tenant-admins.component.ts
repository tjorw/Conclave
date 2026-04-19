import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { PersonDto } from 'shared';
import { SystemTenantService, TenantConvention } from '../../services/system-tenant.service';

@Component({
  selector: 'app-tenant-admins',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatProgressBarModule,
    MatSelectModule,
  ],
  templateUrl: './tenant-admins.component.html',
  styleUrl: './tenant-admins.component.scss',
})
export class TenantAdminsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(SystemTenantService);

  readonly tenantId = signal<string>('');
  readonly loadingConventions = signal(true);
  readonly loadingPersons = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly conventions = signal<TenantConvention[]>([]);
  readonly selectedConventionId = signal<string | null>(null);
  readonly persons = signal<PersonDto[]>([]);

  ngOnInit(): void {
    const tenantId = this.route.snapshot.paramMap.get('tenantId');
    if (!tenantId) {
      this.error.set('Saknar tenant-id i URL.');
      this.loadingConventions.set(false);
      return;
    }

    this.tenantId.set(tenantId);
    this.loadConventions();
  }

  onConventionChange(conventionId: string): void {
    this.selectedConventionId.set(conventionId);
    this.loadPersons();
  }

  makeAdmin(personId: string): void {
    const conventionId = this.selectedConventionId();
    if (!conventionId || this.saving()) return;

    this.saving.set(true);
    this.error.set(null);

    this.service.addConventionAdministrator(this.tenantId(), conventionId, personId).subscribe({
      next: () => {
        this.saving.set(false);
        this.loadPersons();
      },
      error: (err) => this.handleError('Kunde inte lägga till admin', err),
    });
  }

  removeAdmin(personId: string): void {
    const conventionId = this.selectedConventionId();
    if (!conventionId || this.saving()) return;

    this.saving.set(true);
    this.error.set(null);

    this.service.removeConventionAdministrator(this.tenantId(), conventionId, personId).subscribe({
      next: () => {
        this.saving.set(false);
        this.loadPersons();
      },
      error: (err) => this.handleError('Kunde inte ta bort admin', err),
    });
  }

  private loadConventions(): void {
    this.loadingConventions.set(true);
    this.error.set(null);

    this.service.listConventions(this.tenantId()).subscribe({
      next: (conventions) => {
        const sorted = [...conventions].sort((a, b) => a.name.localeCompare(b.name, 'sv'));
        this.conventions.set(sorted);
        const first = sorted[0]?.id ?? null;
        this.selectedConventionId.set(first);
        this.loadingConventions.set(false);

        if (first) {
          this.loadPersons();
        } else {
          this.persons.set([]);
        }
      },
      error: (err) => this.handleError('Kunde inte hämta konvent', err, true),
    });
  }

  private loadPersons(): void {
    const conventionId = this.selectedConventionId();
    if (!conventionId) return;

    this.loadingPersons.set(true);
    this.error.set(null);

    this.service.listConventionPersons(this.tenantId(), conventionId).subscribe({
      next: (persons) => {
        this.persons.set([...persons].sort((a, b) => a.name.localeCompare(b.name, 'sv')));
        this.loadingPersons.set(false);
      },
      error: (err) => this.handleError('Kunde inte hämta personer', err, false, true),
    });
  }

  private handleError(context: string, err: unknown, resetConventionLoading = false, resetPersonsLoading = false): void {
    const detail = (err as { error?: { detail?: string; title?: string } })?.error?.detail
      ?? (err as { error?: { title?: string } })?.error?.title;

    this.error.set(detail ? `${context}: ${detail}` : context);
    this.saving.set(false);
    if (resetConventionLoading) this.loadingConventions.set(false);
    if (resetPersonsLoading) this.loadingPersons.set(false);
  }
}
