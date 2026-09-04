import { isSupportedPassportAnonymousDraft } from './passport-anonymous-draft-validation';
import { PassportAnonymousDraft } from './passport-anonymous-draft.models';

describe('isSupportedPassportAnonymousDraft', () => {
  it('accepts a complete versioned local draft', () => {
    expect(isSupportedPassportAnonymousDraft(createDraft())).toBe(true);
  });

  it('rejects an unsupported schema and malformed nested data', () => {
    expect(isSupportedPassportAnonymousDraft({ ...createDraft(), schemaVersion: 2 })).toBe(false);
    expect(isSupportedPassportAnonymousDraft({
      ...createDraft(),
      rides: [{ ...createDraft().rides[0], count: 0 }]
    })).toBe(false);
    expect(isSupportedPassportAnonymousDraft({
      ...createDraft(),
      visit: { ...createDraft().visit, date: { ...createDraft().visit.date, day: 31, month: 2 } }
    })).toBe(false);
  });

  it('rejects drafts whose expanded ride total exceeds the local safety limit', () => {
    const rides = Array.from({ length: 21 }, (_value: unknown, index: number) => ({
      ...createDraft().rides[0],
      id: `ride-${index}`,
      count: 100
    }));

    expect(isSupportedPassportAnonymousDraft({ ...createDraft(), rides })).toBe(false);
  });
});

function createDraft(): PassportAnonymousDraft {
  return {
    schemaVersion: 1,
    id: 'draft-1',
    visitOperationId: 'visit-operation-1',
    rideOperationId: 'ride-operation-1',
    parkName: 'Parc test',
    visit: {
      parkId: 'park-1',
      date: { year: 2026, month: 9, day: 4, precision: 'Day', isApproximate: false },
      timeZoneId: 'Europe/Paris',
      serviceDayConvention: 'VisitStartLocalDate',
      title: null,
      privateNote: null
    },
    rides: [{
      id: 'ride-1',
      parkItemId: 'item-1',
      attractionName: 'Attraction test',
      moment: { localTime: '10:30', isApproximate: false },
      status: 'Completed',
      privateNote: null,
      confirmHistoricalConflict: false,
      count: 2
    }],
    createdAtUtc: '2026-09-04T10:00:00.000Z',
    updatedAtUtc: '2026-09-04T10:00:00.000Z'
  };
}
