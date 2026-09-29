import { PublicLiveTarget } from '@app/models/live-data/public-live.models';
import { resolvePublicLiveRefreshDelay } from './public-live-refresh.policy';

describe('resolvePublicLiveRefreshDelay', () => {
  const now: number = Date.parse('2026-09-29T10:00:00Z');

  it('adapts polling to half of the shortest current freshness window', () => {
    expect(resolvePublicLiveRefreshDelay(createTarget('2026-09-29T10:03:00Z'), [], now)).toBe(90_000);
  });

  it('never refreshes aggressively near expiration', () => {
    expect(resolvePublicLiveRefreshDelay(createTarget('2026-09-29T10:00:10Z'), [], now)).toBe(30_000);
  });

  it('backs off when no current information exists', () => {
    expect(resolvePublicLiveRefreshDelay(null, [], now)).toBe(300_000);
    expect(resolvePublicLiveRefreshDelay({ ...createTarget(null), availability: 'Expired' }, [], now)).toBe(120_000);
  });
});

function createTarget(expiresAtUtc: string | null): PublicLiveTarget {
  return {
    targetId: 'item-1',
    targetType: 'ParkItem',
    displayName: 'Example attraction',
    parkId: 'park-1',
    parkDisplayName: 'Example park',
    availability: 'Current',
    status: 'Open',
    queues: [],
    asOfUtc: '2026-09-29T10:00:00Z',
    observedAtUtc: '2026-09-29T09:59:00Z',
    receivedAtUtc: '2026-09-29T09:59:01Z',
    ageSeconds: 60,
    freshness: 'Fresh',
    expiresAtUtc,
    source: null,
    confidence: 'High'
  };
}
