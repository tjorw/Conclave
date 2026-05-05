import { EditionDto } from 'shared';
import {
  getSuggestedDateTimeRange,
  getSuggestedPromotionValidityRange,
} from './schedule-defaults';

describe('schedule-defaults', () => {
  const edition = {
    id: 'edition-1',
    conventionId: 'convention-1',
    name: 'Konvent 2027',
    start: '2027-03-01T00:00:00',
    end: '2027-03-03T23:59:59',
    status: 'Published',
    organiserRegistrationOpen: true,
    staffRegistrationOpen: true,
    visitorRegistrationOpen: true,
    staffCoordinatorId: null,
    eventCoordinatorId: null,
    scheduleDays: [
      { date: '2027-03-01', startTime: '08:30', endTime: '20:00' },
      { date: '2027-03-02', startTime: '09:00', endTime: '18:00' },
    ],
    venues: [],
    staffAreas: [],
    stations: [],
    categories: [],
    programTagDefinitions: [],
  } as EditionDto;

  it('suggests first convention day and duration for date ranges', () => {
    const result = getSuggestedDateTimeRange(edition, 120);

    expect(result).toEqual({
      start: '2027-03-01T08:30',
      end: '2027-03-01T10:30',
    });
  });

  it('suggests promotion validity with schedule-day boundaries', () => {
    const result = getSuggestedPromotionValidityRange(edition);

    expect(result).toEqual({
      validFrom: '2027-03-01T08:30',
      validUntil: '2027-03-01T20:00',
    });
  });

  it('falls back to default times when schedule day times are missing', () => {
    const result = getSuggestedDateTimeRange({
      ...edition,
      scheduleDays: [{ date: '2027-03-01', startTime: null, endTime: null }],
    }, 60);

    expect(result).toEqual({
      start: '2027-03-01T09:00',
      end: '2027-03-01T10:00',
    });
  });
});
