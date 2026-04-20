import { Environment } from 'shared';

export const environment: Environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5127',
  conventionId: '00000000-0000-0000-0000-000000000000',
  multitenancy: {
    enabled: false,
    devTenantId: undefined,
  },
};
