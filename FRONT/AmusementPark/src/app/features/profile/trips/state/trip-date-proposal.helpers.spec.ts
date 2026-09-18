import { areTripDateInputsValid, buildConfirmedTripDates, enumerateTripDates } from './trip-date-proposal.helpers';

describe('trip date proposal helpers', () => {
  it('keeps a trip without dates as an explicit undecided proposal', () => {
    expect(buildConfirmedTripDates(' ', '')).toEqual({
      kind: 'None',
      startDate: null,
      endDate: null,
      candidateDates: []
    });
  });

  it('uses the start date as the end date for a one-day trip', () => {
    expect(buildConfirmedTripDates('2026-10-03', '')).toEqual({
      kind: 'Fixed',
      startDate: '2026-10-03',
      endDate: '2026-10-03',
      candidateDates: []
    });
  });

  it('rejects an end date without a start date and a reversed range', () => {
    expect(areTripDateInputsValid('', '2026-10-05')).toBe(false);
    expect(areTripDateInputsValid('2026-10-05', '2026-10-03')).toBe(false);
    expect(areTripDateInputsValid('2026-10-03', '2026-10-05')).toBe(true);
    expect((): void => { buildConfirmedTripDates('', '2026-10-05'); }).toThrow();
  });

  it('enumerates every fixed day inclusively without depending on the browser time zone', () => {
    expect(enumerateTripDates({
      kind: 'Fixed',
      startDate: '2026-10-03',
      endDate: '2026-10-05',
      candidateDates: []
    })).toEqual(['2026-10-03', '2026-10-04', '2026-10-05']);
  });
});
