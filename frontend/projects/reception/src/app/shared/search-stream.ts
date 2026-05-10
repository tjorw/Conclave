import { AbstractControl } from '@angular/forms';
import { Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

export function createSearchStream<T>(
  control: AbstractControl,
  fetch: (term: string) => Observable<T[] | null>,
  options?: { minLength?: number; debounce?: number }
): Observable<T[] | null> {
  const minLength = options?.minLength ?? 2;
  const debounceMs = options?.debounce ?? 300;

  return control.valueChanges.pipe(
    debounceTime(debounceMs),
    distinctUntilChanged(),
    switchMap((value: unknown) => {
      const term = (typeof value === 'string' ? value : '').trim();
      if (term.length < minLength) return of(null);
      return fetch(term);
    }),
  );
}
