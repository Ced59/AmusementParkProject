import { PassportVisit } from '@app/models/passport/passport-visit.models';
import {
  createPassportVisitMetadataDraft,
  mapPassportVisitMetadataDraft
} from './passport-visit-metadata.mapper';

describe('passport visit metadata mapper', () => {
  const visit: PassportVisit = {
    id: 'visit-1', parkId: 'park-1',
    date: { year: 2026, month: 9, day: 3, precision: 'Day', isApproximate: false },
    timeZoneId: 'Europe/Paris', serviceDayConvention: 'VisitStartLocalDate',
    status: 'Draft', privacy: 'Private', title: ' Journée ', privateNote: ' Privé ',
    version: 2, createdAtUtc: '2026-09-03T10:00:00Z', updatedAtUtc: '2026-09-03T10:00:00Z',
    completedAtUtc: null
  };

  it('round-trips editable metadata and keeps the optimistic version', () => {
    const result = mapPassportVisitMetadataDraft(createPassportVisitMetadataDraft(visit), 2);

    expect(result.errorKey).toBeNull();
    expect(result.request).toEqual({
      date: visit.date,
      timeZoneId: 'Europe/Paris',
      serviceDayConvention: 'VisitStartLocalDate',
      title: 'Journée',
      privateNote: 'Privé',
      expectedVersion: 2
    });
  });

  it('does not invent calendar precision and rejects an impossible day', () => {
    const draft = createPassportVisitMetadataDraft(visit);
    expect(mapPassportVisitMetadataDraft({ ...draft, precision: 'Year' }, 2).request?.date)
      .toEqual({ year: 2026, month: null, day: null, precision: 'Year', isApproximate: false });
    expect(mapPassportVisitMetadataDraft({ ...draft, year: 2025, month: 2, day: 29 }, 2))
      .toEqual({ request: null, errorKey: 'passport.editor.visit.validation.dayInvalid' });
  });

  it('preserves a stored user-selected service-day convention', () => {
    const userSelectedVisit: PassportVisit = {
      ...visit,
      serviceDayConvention: 'UserSelectedServiceDate'
    };

    const result = mapPassportVisitMetadataDraft(
      createPassportVisitMetadataDraft(userSelectedVisit),
      2);

    expect(result.request?.serviceDayConvention).toBe('UserSelectedServiceDate');
  });
});
