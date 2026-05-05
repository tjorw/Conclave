import { describe, expect, it } from 'vitest';
import { routes } from './app.routes';

function getShellChildPaths(): string[] {
  const shellRoute = routes.find(route => route.path === '');
  if (!shellRoute?.children) {
    return [];
  }

  return shellRoute.children
    .map(child => child.path)
    .filter((path): path is string => typeof path === 'string');
}

describe('Admin routes (R-ADM01)', () => {
  it('keeps edition-dependent views under editions/:id', () => {
    const paths = getShellChildPaths();

    expect(paths).toContain('editions/:id/events');
    expect(paths).toContain('editions/:id/events/:eventId');
    expect(paths).toContain('editions/:id/sessions');
    expect(paths).toContain('editions/:id/persons/visitors');
    expect(paths).toContain('editions/:id/persons/organisers');
    expect(paths).toContain('editions/:id/persons/staff');
    expect(paths).toContain('editions/:id/persons/reception-staff');
    expect(paths).toContain('editions/:id/registrations/visitors');
    expect(paths).toContain('editions/:id/registrations/promotion-codes');
    expect(paths).toContain('editions/:id/staffing/function-areas');
    expect(paths).toContain('editions/:id/staffing/schedule');
  });

  it('does not expose old top-level routes for edition-dependent views', () => {
    const paths = getShellChildPaths();

    expect(paths).not.toContain('events');
    expect(paths).not.toContain('events/:eventId');
    expect(paths).not.toContain('sessions');
    expect(paths).not.toContain('visitors');
    expect(paths).not.toContain('organisers');
    expect(paths).not.toContain('staff');
    expect(paths).not.toContain('reception-staff');
    expect(paths).not.toContain('registrations/visitors');
    expect(paths).not.toContain('registrations/promotion-codes');
    expect(paths).not.toContain('staffing/function-areas');
    expect(paths).not.toContain('staffing/schedule');
  });
});
