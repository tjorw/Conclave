import { HttpErrorResponse } from '@angular/common/http';
import { toContextErrorMessage, toErrorMessage } from './error-message';

describe('toErrorMessage', () => {
  it('prefers detail over title and message', () => {
    const error = new HttpErrorResponse({
      error: { detail: 'Detalj', title: 'Titel', message: 'Meddelande' },
    });

    expect(toErrorMessage(error, 'Fallback')).toBe('Detalj');
  });

  it('falls back to title and then message', () => {
    expect(toErrorMessage({ error: { title: 'Titel', message: 'Meddelande' } }, 'Fallback')).toBe('Titel');
    expect(toErrorMessage({ error: { message: 'Meddelande' } }, 'Fallback')).toBe('Meddelande');
  });

  it('uses fallback when no usable server message exists', () => {
    expect(toErrorMessage(new Error('Nope'), 'Fallback')).toBe('Fallback');
    expect(toErrorMessage({ error: { detail: '   ' } }, 'Fallback')).toBe('Fallback');
  });
});

describe('toContextErrorMessage', () => {
  it('prefixes server messages with context', () => {
    expect(toContextErrorMessage({ error: { detail: 'Detalj' } }, 'Kunde inte spara')).toBe('Kunde inte spara: Detalj');
  });

  it('does not duplicate context when fallback is used', () => {
    expect(toContextErrorMessage(new Error('Nope'), 'Kunde inte spara')).toBe('Kunde inte spara');
  });
});
