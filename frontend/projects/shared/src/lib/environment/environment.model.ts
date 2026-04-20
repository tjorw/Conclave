export interface MultitenancyEnvironment {
  enabled: boolean;
  devTenantId?: string;
}

export interface Environment {
  production: boolean;
  apiBaseUrl: string;
  conventionId?: string;
  multitenancy?: MultitenancyEnvironment;
}
