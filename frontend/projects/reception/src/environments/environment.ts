import { Environment } from 'shared';

export const environment: Environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5127',
  multitenancy: {
    enabled: false,
    devTenantId: undefined,
  },
};
