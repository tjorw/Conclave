import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { PersonDto, toContextErrorMessage } from 'shared';
import { SystemTenantService, TenantConvention, TenantListItem } from '../../services/system-tenant.service';

@Component({
  selector: 'app-tenant-detail',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatProgressBarModule,
    MatSelectModule,
  ],
  templateUrl: './tenant-detail.component.html',
  styleUrl: './tenant-detail.component.scss',
})
export class TenantDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(SystemTenantService);

  readonly tenantId = signal<string>('');
  readonly loadingTenant = signal(true);
  readonly loadingConventions = signal(true);
  readonly loadingPersons = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly tenant = signal<TenantListItem | null>(null);
  readonly conventions = signal<TenantConvention[]>([]);
  readonly selectedConventionId = signal<string | null>(null);
  readonly persons = signal<PersonDto[]>([]);

  readonly selectedConvention = computed(
    () => this.conventions().find(convention => convention.id === this.selectedConventionId()) ?? null,
  );

  readonly canProvision = computed(() => this.tenant()?.status === 'Active');

  ngOnInit(): void {
    const tenantId = this.route.snapshot.paramMap.get('tenantId');
    if (!tenantId) {
      this.error.set('Saknar tenant-id i URL.');
      this.loadingTenant.set(false);
      this.loadingConventions.set(false);
      return;
    }

    this.tenantId.set(tenantId);
    this.loadTenant();
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
      error: err => this.handleError('Kunde inte lägga till admin', err),
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
      error: err => this.handleError('Kunde inte ta bort admin', err),
    });
  }

  private loadTenant(): void {
    this.loadingTenant.set(true);
    this.error.set(null);

    this.service.list().subscribe({
      next: tenants => {
        const tenant = tenants.find(item => item.id === this.tenantId()) ?? null;
        this.tenant.set(tenant);

        if (!tenant) {
          this.error.set('Tenanten kunde inte hittas.');
        }

        this.loadingTenant.set(false);
      },
      error: err => this.handleError('Kunde inte hämta tenant', err, true),
    });
  }

  private loadConventions(): void {
    this.loadingConventions.set(true);
    this.error.set(null);

    this.service.listConventions(this.tenantId()).subscribe({
      next: conventions => {
        const sorted = [...conventions].sort((a, b) => a.name.localeCompare(b.name, 'sv'));
        this.conventions.set(sorted);
        const selectedConventionId = this.selectedConventionId();
        const hasSelectedConvention = selectedConventionId && sorted.some(convention => convention.id === selectedConventionId);
        const nextConventionId = hasSelectedConvention ? selectedConventionId : (sorted[0]?.id ?? null);

        this.selectedConventionId.set(nextConventionId);
        this.loadingConventions.set(false);

        if (nextConventionId) {
          this.loadPersons();
        } else {
          this.persons.set([]);
          this.loadingPersons.set(false);
        }
      },
      error: err => this.handleError('Kunde inte hämta konvent', err, false, true),
    });
  }

  private loadPersons(): void {
    const conventionId = this.selectedConventionId();
    if (!conventionId) return;

    this.loadingPersons.set(true);
    this.error.set(null);

    this.service.listConventionPersons(this.tenantId(), conventionId).subscribe({
      next: persons => {
        this.persons.set([...persons].sort((a, b) => a.name.localeCompare(b.name, 'sv')));
        this.loadingPersons.set(false);
      },
      error: err => this.handleError('Kunde inte hämta personer', err, false, false, true),
    });
  }

  private handleError(
    context: string,
    err: unknown,
    resetTenantLoading = false,
    resetConventionLoading = false,
    resetPersonsLoading = false,
  ): void {
    this.error.set(toContextErrorMessage(err, context));
    this.saving.set(false);
    if (resetTenantLoading) this.loadingTenant.set(false);
    if (resetConventionLoading) this.loadingConventions.set(false);
    if (resetPersonsLoading) this.loadingPersons.set(false);
  }
}
