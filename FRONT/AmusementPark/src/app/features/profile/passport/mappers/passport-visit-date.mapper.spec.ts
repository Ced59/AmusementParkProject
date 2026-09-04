import { PassportVisitDate } from '@app/models/passport/passport-visit.models';
import { formatPassportVisitDate } from './passport-visit-date.mapper';

describe('formatPassportVisitDate', () => {
  it('preserves year-only precision without inventing a month or day', () => {
    const date: PassportVisitDate = {
      year: 2021,
      month: null,
      day: null,
      precision: 'Year',
      isApproximate: true
    };

    expect(formatPassportVisitDate(date, 'fr')).toBe('≈ 2021');
  });

  it('localizes exact dates in UTC to avoid a timezone day shift', () => {
    const date: PassportVisitDate = {
      year: 2026,
      month: 9,
      day: 3,
      precision: 'Day',
      isApproximate: false
    };

    expect(formatPassportVisitDate(date, 'fr')).toBe('3 septembre 2026');
  });
});
