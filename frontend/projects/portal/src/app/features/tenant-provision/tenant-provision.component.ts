import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { PersonDto, toContextErrorMessage } from 'shared';
import {
  ProvisionTenantConventionResponse,
  SystemTenantService,
  TenantListItem,
} from '../../services/system-tenant.service';

@Component({
  selector: 'app-tenant-provision',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './tenant-provision.component.html',
  styleUrl: './tenant-provision.component.scss',
})
export class TenantProvisionComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(SystemTenantService);
  private readonly fb = inject(FormBuilder);

  readonly tenantId = signal('');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly tenant = signal<TenantListItem | null>(null);
  readonly provisionResult = signal<ProvisionTenantConventionResult | null>(null);
  readonly existingConventionId = signal<string | null>(null);
  readonly existingAdmin = signal<PersonDto | null>(null);

  private latestAdminEmail: string | null = null;

  readonly form = this.fb.group({
    adminName: ['', Validators.required],
    adminEmail: ['', [Validators.required, Validators.email]],
    adminPassword: ['', [Validators.required, Validators.minLength(8)]],
  });

  readonly canProvision = computed(() => this.tenant()?.status === 'Active');

  ngOnInit(): void {
    const tenantId = this.route.snapshot.paramMap.get('tenantId');
    if (!tenantId) {
      this.error.set('Saknar tenant-id i URL.');
      this.loading.set(false);
      return;
    }

    this.tenantId.set(tenantId);
    this.loadTenant();
  }

  submit(): void {
    if (this.form.invalid || this.saving() || !this.canProvision()) return;

    this.saving.set(true);
    this.error.set(null);
    this.provisionResult.set(null);
    this.latestAdminEmail = this.form.value.adminEmail ?? null;

    this.service.provision(this.tenantId(), {
      adminName: this.form.value.adminName!,
      adminEmail: this.form.value.adminEmail!,
      adminPassword: this.form.value.adminPassword!,
    }).subscribe({
      next: result => {
        this.provisionResult.set({
          ...result,
          adminEmail: this.latestAdminEmail,
        });
        this.saving.set(false);
        this.form.patchValue({ adminPassword: '' });
        this.loadTenant(false);
      },
      error: err => this.handleError('Kunde inte provisionera tenant', err),
    });
  }

  private loadTenant(resetLoading = true): void {
    if (resetLoading) this.loading.set(true);

    this.service.list().subscribe({
      next: tenants => {
        const tenant = tenants.find(x => x.id === this.tenantId()) ?? null;
        this.tenant.set(tenant);

        if (!tenant) {
          this.error.set('Tenanten kunde inte hittas.');
          this.loading.set(false);
          return;
        }

        this.loadExistingProvisioning();
      },
      error: err => this.handleError('Kunde inte hämta tenant', err, true),
    });
  }

  private loadExistingProvisioning(): void {
    this.service.listConventions(this.tenantId()).subscribe({
      next: conventions => {
        const convention = conventions[0] ?? null;
        this.existingConventionId.set(convention?.id ?? null);

        if (!convention) {
          this.existingAdmin.set(null);
          this.loading.set(false);
          return;
        }

        this.service.listConventionPersons(this.tenantId(), convention.id).subscribe({
          next: persons => {
            const admin = persons.find(person => person.isAdmin) ?? null;
            this.existingAdmin.set(admin);

            if (admin) {
              const adminNameControl = this.form.get('adminName');
              const adminEmailControl = this.form.get('adminEmail');

              if (adminNameControl?.pristine) {
                adminNameControl.patchValue(admin.name);
              }

              if (adminEmailControl?.pristine) {
                adminEmailControl.patchValue(admin.email);
              }
            }

            this.loading.set(false);
          },
          error: err => this.handleError('Kunde inte hämta tenant-admin', err, true),
        });
      },
      error: err => this.handleError('Kunde inte hämta tenantens konvent', err, true),
    });
  }

  private handleError(context: string, err: unknown, resetLoading = false): void {
    this.error.set(toContextErrorMessage(err, context));
    this.saving.set(false);
    if (resetLoading) this.loading.set(false);
  }
}

type ProvisionTenantConventionResult =
  ProvisionTenantConventionResponse & { adminEmail: string | null };
