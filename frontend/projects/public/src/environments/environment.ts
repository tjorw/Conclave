import { Environment } from 'shared';

export const environment: Environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5127',
  conventionId: '00000000-0000-0000-0000-000000000000', // fallback om /convention inte kan laddas
  multitenancy: {
    enabled: false,
    devTenantId: undefined,
  },
};
