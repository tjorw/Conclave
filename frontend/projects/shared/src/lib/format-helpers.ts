const LOCALE = 'sv-SE';

const SEK_FORMAT = new Intl.NumberFormat(LOCALE, {
  style: 'currency',
  currency: 'SEK',
  maximumFractionDigits: 0,
});

/** ISO-sträng → lokalt datumformat, t.ex. '2025-04-30'. */
export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(LOCALE);
}

/** Date → 'YYYY-MM-DD' utan tidzon-påverkan. */
export function formatDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, '0');
  const d = `${date.getDate()}`.padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** Date → 'onsdag 30 apr' (veckodag lång, dag numerisk, månad kort). */
export function formatDayLabel(date: Date): string {
  return date.toLocaleDateString(LOCALE, { weekday: 'long', day: 'numeric', month: 'short' });
}

/**
 * ISO-start + ISO-slut → 'tor 30 apr 14:00–16:00'
 * Vid olika dagar: 'tor 30 apr 14:00 – fre 1 maj 10:00'.
 */
export function formatTimeRange(start: string, end: string): string {
  const s = new Date(start);
  const e = new Date(end);
  const sameDay = s.toDateString() === e.toDateString();
  const dateLabel = s.toLocaleDateString(LOCALE, { weekday: 'short', day: 'numeric', month: 'short' });
  const sTime = s.toLocaleTimeString(LOCALE, { hour: '2-digit', minute: '2-digit' });
  const eTime = e.toLocaleTimeString(LOCALE, { hour: '2-digit', minute: '2-digit' });

  if (sameDay) {
    return `${dateLabel} ${sTime}–${eTime}`;
  }

  const eDateLabel = e.toLocaleDateString(LOCALE, { weekday: 'short', day: 'numeric', month: 'short' });
  return `${dateLabel} ${sTime} – ${eDateLabel} ${eTime}`;
}

/** Öre → 'Kostnadsfri' (0) eller '150 kr'. */
export function formatTicketPrice(priceInOre: number): string {
  if (priceInOre === 0) return 'Kostnadsfri';
  return SEK_FORMAT.format(priceInOre / 100);
}

/** Öre → '150 kr'. Ingen nollkontroll – använd för generisk prisformatering. */
export function formatSekPrice(priceInOre: number): string {
  return SEK_FORMAT.format(priceInOre / 100);
}
