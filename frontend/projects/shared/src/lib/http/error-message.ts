import { HttpErrorResponse } from '@angular/common/http';

type ErrorPayload = {
  detail?: unknown;
  title?: unknown;
  message?: unknown;
};

export function toErrorMessage(error: unknown, fallback: string): string {
  const payload = getErrorPayload(error);

  return firstNonEmptyString(payload?.detail)
    ?? firstNonEmptyString(payload?.title)
    ?? firstNonEmptyString(payload?.message)
    ?? fallback;
}

export function toContextErrorMessage(error: unknown, context: string): string {
  const message = toErrorMessage(error, context);
  return message === context ? context : `${context}: ${message}`;
}

function getErrorPayload(error: unknown): ErrorPayload | null {
  if (error instanceof HttpErrorResponse) {
    return isErrorPayload(error.error) ? error.error : null;
  }

  if (!hasErrorPayload(error)) {
    return null;
  }

  return isErrorPayload(error.error) ? error.error : null;
}

function hasErrorPayload(value: unknown): value is { error?: unknown } {
  return typeof value === 'object' && value !== null && 'error' in value;
}

function isErrorPayload(value: unknown): value is ErrorPayload {
  return typeof value === 'object' && value !== null;
}

function firstNonEmptyString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value : null;
}
