import { Park } from '@app/models/parks/park';
import { PassportVisitQuickCreateDraft } from '../models/passport-visit-quick-create.models';
import { mapParkToPassportOption, mapPassportVisitQuickCreateDraft } from './passport-visit-quick-create.mapper';

describe('passport visit quick-create mapper', () => {
  it('preserves a year-only approximate date without inventing month or day', () => {
    const result = mapPassportVisitQuickCreateDraft(createDraft({
      precision: 'Year',
      year: 1998,
      month: 7,
      day: 12,
      isApproximate: true
    }));

    expect(result.errorKey).toBeNull();
    expect(result.request?.date).toEqual({
      year: 1998,
      month: null,
      day: null,
      precision: 'Year',
      isApproximate: true
    });
  });

  it('rejects impossible calendar days including non-leap February dates', () => {
    const invalidResult = mapPassportVisitQuickCreateDraft(createDraft({
      year: 2025,
      month: 2,
      day: 29
    }));
    const leapResult = mapPassportVisitQuickCreateDraft(createDraft({
      year: 2024,
      month: 2,
      day: 29
    }));

    expect(invalidResult.request).toBeNull();
    expect(invalidResult.errorKey).toBe('passport.quickCreate.validation.dayInvalid');
    expect(leapResult.request?.date.day).toBe(29);
  });

  it('normalizes optional text while preserving the IANA time zone', () => {
    const result = mapPassportVisitQuickCreateDraft(createDraft({
      title: '  First trip  ',
      privateNote: '  Great day  ',
      timeZoneId: ' Europe/Paris '
    }));

    expect(result.request).toEqual(expect.objectContaining({
      timeZoneId: 'Europe/Paris',
      title: 'First trip',
      privateNote: 'Great day'
    }));
  });

  it('maps only parks with opaque identifiers and visitor-facing names', () => {
    const park: Park = {
      id: 'opaque/park',
      name: 'Parc Test',
      city: 'Lille',
      countryCode: 'FR',
      latitude: 0,
      longitude: 0
    };

    expect(mapParkToPassportOption(park)).toEqual({
      id: 'opaque/park',
      name: 'Parc Test',
      location: 'Lille · FR'
    });
    expect(mapParkToPassportOption({ ...park, id: ' ' })).toBeNull();
  });
});

function createDraft(overrides: Partial<PassportVisitQuickCreateDraft> = {}): PassportVisitQuickCreateDraft {
  return {
    parkId: 'park-1',
    precision: 'Day',
    year: 2026,
    month: 9,
    day: 3,
    isApproximate: false,
    timeZoneId: 'Europe/Paris',
    title: '',
    privateNote: '',
    ...overrides
  };
}
