import { Environment } from 'shared';

export const environment: Environment = {
  production: true,
  apiBaseUrl: '',
  multitenancy: {
    enabled: false,
    devTenantId: undefined,
  },
};
