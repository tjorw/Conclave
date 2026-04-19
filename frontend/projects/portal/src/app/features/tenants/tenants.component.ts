import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { SystemTenantService, TenantListItem } from '../../services/system-tenant.service';

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './tenants.component.html',
  styleUrl: './tenants.component.scss',
})
export class TenantsComponent implements OnInit {
  private readonly service = inject(SystemTenantService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly tenants = signal<TenantListItem[]>([]);
  readonly searchQuery = signal('');

  readonly createForm = this.fb.group({
    subdomain: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]],
    displayName: ['', Validators.required],
  });

  readonly filteredTenants = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    if (!query) return this.tenants();

    return this.tenants().filter(t =>
      t.displayName.toLowerCase().includes(query) ||
      t.subdomain.toLowerCase().includes(query) ||
      t.status.toLowerCase().includes(query));
  });

  ngOnInit(): void {
    this.reload();
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
  }

  createTenant(): void {
    if (this.createForm.invalid || this.saving()) return;

    this.saving.set(true);
    this.error.set(null);

    this.service.create({
      subdomain: this.createForm.value.subdomain!,
      displayName: this.createForm.value.displayName!,
    }).subscribe({
      next: () => {
        this.createForm.reset();
        this.saving.set(false);
        this.reload();
      },
      error: (err) => {
        this.handleError('Kunde inte skapa tenant', err);
      },
    });
  }

  suspendTenant(tenantId: string): void {
    if (this.saving()) return;

    this.saving.set(true);
    this.error.set(null);

    this.service.suspend(tenantId).subscribe({
      next: () => {
        this.saving.set(false);
        this.reload();
      },
      error: (err) => {
        this.handleError('Kunde inte suspendera tenant', err);
      },
    });
  }

  restoreTenant(tenantId: string): void {
    if (this.saving()) return;

    this.saving.set(true);
    this.error.set(null);

    this.service.restore(tenantId).subscribe({
      next: () => {
        this.saving.set(false);
        this.reload();
      },
      error: (err) => {
        this.handleError('Kunde inte återaktivera tenant', err);
      },
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.error.set(null);

    this.service.list().subscribe({
      next: (tenants) => {
        const sorted = [...tenants].sort((a, b) => a.displayName.localeCompare(b.displayName, 'sv'));
        this.tenants.set(sorted);
        this.loading.set(false);
      },
      error: (err) => {
        this.handleError('Kunde inte hämta tenants', err, true);
      },
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
