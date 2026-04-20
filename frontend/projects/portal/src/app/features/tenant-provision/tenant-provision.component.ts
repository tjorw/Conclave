import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
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

  private latestAdminEmail: string | null = null;

  readonly form = this.fb.group({
    conventionName: ['', Validators.required],
    conventionSlug: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]],
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
      conventionName: this.form.value.conventionName!,
      conventionSlug: this.form.value.conventionSlug!,
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
        this.form.reset();
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
        } else if (!this.form.dirty && !this.provisionResult()) {
          this.form.patchValue({
            conventionName: tenant.displayName,
            conventionSlug: tenant.subdomain,
          });
        }

        this.loading.set(false);
      },
      error: err => this.handleError('Kunde inte hämta tenant', err, true),
    });
  }

  private handleError(context: string, err: unknown, resetLoading = false): void {
    const detail = (err as { error?: { detail?: string; title?: string } })?.error?.detail
      ?? (err as { error?: { title?: string } })?.error?.title;

    this.error.set(detail ? `${context}: ${detail}` : context);
    this.saving.set(false);
    if (resetLoading) this.loading.set(false);
  }
}

type ProvisionTenantConventionResult =
  ProvisionTenantConventionResponse & { adminEmail: string | null };
