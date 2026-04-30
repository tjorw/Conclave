import { signal, WritableSignal } from '@angular/core';

export interface AsyncState {
  loading: WritableSignal<boolean>;
  saving: WritableSignal<boolean>;
  error: WritableSignal<string | null>;
}

/**
 * Skapar grupperade signals för asynkrona operationer.
 * @param initialLoading - Om loading ska starta som true (t.ex. detaljvyer som laddar data direkt).
 */
export function createAsyncState(initialLoading = false): AsyncState {
  return {
    loading: signal(initialLoading),
    saving:  signal(false),
    error:   signal<string | null>(null),
  };
}
