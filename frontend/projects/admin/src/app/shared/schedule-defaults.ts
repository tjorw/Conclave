import { EditionDto, EditionScheduleDayDto } from 'shared';

const FALLBACK_START_TIME = '09:00';
const FALLBACK_END_TIME = '17:00';

export interface SuggestedDateTimeRange {
  start: string;
  end: string;
}

export interface SuggestedValidityRange {
  validFrom: string;
  validUntil: string;
}

export function getSuggestedDateTimeRange(
  edition: Pick<EditionDto, 'start' | 'scheduleDays'> | null,
  durationMinutes: number,
): SuggestedDateTimeRange | null {
  if (!edition) {
    return null;
  }

  const date = getFirstConventionDate(edition);
  const startTime = getScheduleBoundaryTime(edition.scheduleDays, date, 'start');
  const start = `${date}T${startTime}`;

  return {
    start,
    end: addMinutesToLocalDateTime(start, durationMinutes),
  };
}

export function getSuggestedPromotionValidityRange(
  edition: Pick<EditionDto, 'start' | 'scheduleDays'> | null,
): SuggestedValidityRange | null {
  if (!edition) {
    return null;
  }

  const date = getFirstConventionDate(edition);
  const startTime = getScheduleBoundaryTime(edition.scheduleDays, date, 'start');
  const endTime = getScheduleBoundaryTime(edition.scheduleDays, date, 'end');

  return {
    validFrom: `${date}T${startTime}`,
    validUntil: `${date}T${endTime}`,
  };
}

function getFirstConventionDate(edition: Pick<EditionDto, 'start' | 'scheduleDays'>): string {
  const sortedDays = [...(edition.scheduleDays ?? [])]
    .map(day => normalizeDate(day.date))
    .sort((left, right) => left.localeCompare(right));

  if (sortedDays.length > 0) {
    return sortedDays[0];
  }

  return normalizeDate(edition.start);
}

function getScheduleBoundaryTime(
  scheduleDays: EditionScheduleDayDto[] | null | undefined,
  date: string,
  boundary: 'start' | 'end',
): string {
  const scheduleDay = (scheduleDays ?? []).find(day => normalizeDate(day.date) === date);
  const time = boundary === 'start' ? scheduleDay?.startTime : scheduleDay?.endTime;

  if (time && time.length >= 5) {
    return time.slice(0, 5);
  }

  return boundary === 'start' ? FALLBACK_START_TIME : FALLBACK_END_TIME;
}

function addMinutesToLocalDateTime(value: string, minutes: number): string {
  const date = new Date(value);
  date.setMinutes(date.getMinutes() + minutes);

  const pad = (segment: number) => String(segment).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function normalizeDate(value: string): string {
  return value.slice(0, 10);
}
