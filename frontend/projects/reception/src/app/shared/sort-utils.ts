export type SortDirection = 'asc' | 'desc';

export interface SortState<K extends string> {
  key: K;
  direction: SortDirection;
}

type SortValue = string | number | boolean | Date | null | undefined;

export type SortSelectors<T, K extends string> = Record<K, (item: T) => SortValue>;

export function nextSort<K extends string>(current: SortState<K>, key: K): SortState<K> {
  if (current.key !== key) {
    return { key, direction: 'asc' };
  }
  return { key, direction: current.direction === 'asc' ? 'desc' : 'asc' };
}

export function sortIcon<K extends string>(current: SortState<K>, key: K): string {
  if (current.key !== key) return 'unfold_more';
  return current.direction === 'asc' ? 'arrow_upward' : 'arrow_downward';
}

export function sortBy<T, K extends string>(
  items: readonly T[],
  state: SortState<K>,
  selectors: SortSelectors<T, K>
): T[] {
  const selector = selectors[state.key];
  const direction = state.direction === 'asc' ? 1 : -1;
  return [...items].sort((a, b) => compareSortValues(selector(a), selector(b)) * direction);
}

function compareSortValues(a: SortValue, b: SortValue): number {
  if (a === b) return 0;
  if (a === null || a === undefined) return 1;
  if (b === null || b === undefined) return -1;

  if (a instanceof Date || b instanceof Date) {
    return toTime(a) - toTime(b);
  }

  if (typeof a === 'number' || typeof b === 'number') {
    return Number(a) - Number(b);
  }

  if (typeof a === 'boolean' || typeof b === 'boolean') {
    return Number(a) - Number(b);
  }

  const aString = String(a);
  const bString = String(b);
  const aTime = Date.parse(aString);
  const bTime = Date.parse(bString);

  if (!Number.isNaN(aTime) && !Number.isNaN(bTime)) {
    return aTime - bTime;
  }

  return aString.localeCompare(bString, 'sv-SE', { sensitivity: 'base', numeric: true });
}

function toTime(value: SortValue): number {
  if (value instanceof Date) return value.getTime();
  if (typeof value === 'number') return value;
  return Date.parse(String(value));
}
