import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT, PersonDto } from 'shared';

export interface TenantListItem {
  id: string;
  subdomain: string;
  displayName: string;
  status: 'Active' | 'Suspended' | string;
  createdAt: string;
}

export interface TenantConvention {
  id: string;
  name: string;
  slug: string;
}

interface CreateTenantResponse {
  id: string;
}

export interface ProvisionTenantConventionRequest {
  conventionName: string;
  conventionSlug: string;
  adminName: string;
  adminEmail: string;
  adminPassword: string;
}

export interface ProvisionTenantConventionResponse {
  conventionId: string;
  adminUserId: string;
}

export interface CreateSystemTenantRequest {
  subdomain: string;
  displayName: string;
  adminName: string;
  adminEmail: string;
  adminPassword: string;
}

@Injectable({ providedIn: 'root' })
export class SystemTenantService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  list() {
    return this.http.get<TenantListItem[]>(`${this.env.apiBaseUrl}/system/tenants`);
  }

  create(request: CreateSystemTenantRequest) {
    return this.http.post<CreateTenantResponse>(`${this.env.apiBaseUrl}/system/tenants`, request);
  }

  suspend(tenantId: string) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/system/tenants/${tenantId}/suspend`, null);
  }

  restore(tenantId: string) {
    return this.http.put<void>(`${this.env.apiBaseUrl}/system/tenants/${tenantId}/restore`, null);
  }

  listConventions(tenantId: string) {
    return this.http.get<TenantConvention[]>(`${this.env.apiBaseUrl}/system/tenants/${tenantId}/conventions`);
  }

  listConventionPersons(tenantId: string, conventionId: string) {
    return this.http.get<PersonDto[]>(
      `${this.env.apiBaseUrl}/system/tenants/${tenantId}/conventions/${conventionId}/persons`,
    );
  }

  addConventionAdministrator(tenantId: string, conventionId: string, personId: string) {
    return this.http.post<void>(
      `${this.env.apiBaseUrl}/system/tenants/${tenantId}/conventions/${conventionId}/administrators`,
      { personId },
    );
  }

  removeConventionAdministrator(tenantId: string, conventionId: string, personId: string) {
    return this.http.delete<void>(
      `${this.env.apiBaseUrl}/system/tenants/${tenantId}/conventions/${conventionId}/administrators/${personId}`,
    );
  }

  provision(tenantId: string, request: ProvisionTenantConventionRequest) {
    return this.http.post<ProvisionTenantConventionResponse>(
      `${this.env.apiBaseUrl}/system/tenants/${tenantId}/provision`,
      request,
    );
  }
}
