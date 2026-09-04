import { PassportVisit } from '@app/models/passport/passport-visit.models';
import { mapPassportVisitOverviewItem } from './passport-visits-overview.mapper';

describe('mapPassportVisitOverviewItem', () => {
  it('uses the hydrated park name and keeps private notes as a presence flag', () => {
    const result = mapPassportVisitOverviewItem(createVisit({
      parkName: ' Parc Astérix ',
      privateNote: 'Souvenir privé'
    }), 'fr');

    expect(result.parkName).toBe('Parc Astérix');
    expect(result.hasPrivateNote).toBe(true);
    expect(result).not.toHaveProperty('privateNote');
  });

  it('falls back to the historical park identifier when the park no longer resolves', () => {
    const result = mapPassportVisitOverviewItem(createVisit({ parkName: null }), 'en');

    expect(result.parkName).toBe('park-1');
    expect(result.statusLabelKey).toBe('passport.overview.status.Draft');
  });
});

function createVisit(overrides: Partial<PassportVisit> = {}): PassportVisit {
  return {
    id: 'visit-1',
    parkId: 'park-1',
    parkName: 'Park',
    date: { year: 2026, month: 9, day: 3, precision: 'Day', isApproximate: false },
    timeZoneId: null,
    serviceDayConvention: 'VisitStartLocalDate',
    status: 'Draft',
    privacy: 'Private',
    title: null,
    privateNote: null,
    version: 1,
    createdAtUtc: '2026-09-03T12:00:00Z',
    updatedAtUtc: '2026-09-03T12:00:00Z',
    completedAtUtc: null,
    ...overrides
  };
}
