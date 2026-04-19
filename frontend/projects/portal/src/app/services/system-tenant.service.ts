import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from 'shared';

export interface TenantListItem {
  id: string;
  subdomain: string;
  displayName: string;
  status: 'Active' | 'Suspended' | string;
  createdAt: string;
}

interface CreateTenantResponse {
  id: string;
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
}
