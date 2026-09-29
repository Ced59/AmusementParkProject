import { PublicLiveTarget } from '@app/models/live-data/public-live.models';
import {
  filterPublicLiveTargets,
  isPublicLiveTargetClosed,
  resolvePublicLiveAttributionSources,
  resolvePublicLiveFreshnessReference,
  resolvePublicLiveStatusLabelKey,
  resolvePublicLiveWaitMinutes
} from './public-live-view.helpers';

describe('public live view helpers', () => {
  it('keeps a genuine zero-minute wait distinct from an unknown wait', () => {
    const noWait: PublicLiveTarget = createTarget({ waitTimeMinutes: 0 });
    const unknownWait: PublicLiveTarget = createTarget({ waitTimeMinutes: null });

    expect(resolvePublicLiveWaitMinutes(noWait)).toBe(0);
    expect(resolvePublicLiveWaitMinutes(unknownWait)).toBeNull();
  });

  it('hides stale or closed wait values even when the source supplied a number', () => {
    const closed: PublicLiveTarget = createTarget({ status: 'Closed', waitTimeMinutes: 20 });
    const expired: PublicLiveTarget = createTarget({ availability: 'Expired', waitTimeMinutes: 15 });

    expect(isPublicLiveTargetClosed(closed)).toBe(true);
    expect(resolvePublicLiveWaitMinutes(closed)).toBeNull();
    expect(resolvePublicLiveWaitMinutes(expired)).toBeNull();
    expect(resolvePublicLiveStatusLabelKey(expired)).toBe('liveData.availability.expired');
  });

  it('filters short waits without treating unknown data as zero', () => {
    const zero: PublicLiveTarget = createTarget({ targetId: 'zero', waitTimeMinutes: 0 });
    const short: PublicLiveTarget = createTarget({ targetId: 'short', waitTimeMinutes: 20 });
    const long: PublicLiveTarget = createTarget({ targetId: 'long', waitTimeMinutes: 21 });
    const unknown: PublicLiveTarget = createTarget({ targetId: 'unknown', waitTimeMinutes: null });

    expect(filterPublicLiveTargets([zero, short, long, unknown], 'shortWait').map((item) => item.targetId))
      .toEqual(['zero', 'short']);
  });

  it('includes only open or limited operations in the available filter', () => {
    const open: PublicLiveTarget = createTarget({ targetId: 'open' });
    const limited: PublicLiveTarget = createTarget({
      targetId: 'limited',
      status: 'OperatingWithLimitations'
    });
    const delayed: PublicLiveTarget = createTarget({ targetId: 'delayed', status: 'Delayed' });
    const unknown: PublicLiveTarget = createTarget({ targetId: 'unknown', status: 'Unknown' });
    const missing: PublicLiveTarget = createTarget({ targetId: 'missing', status: null });

    expect(filterPublicLiveTargets([open, limited, delayed, unknown, missing], 'available')
      .map((item: PublicLiveTarget) => item.targetId))
      .toEqual(['open', 'limited']);
  });

  it('attributes displayed child observations and uses their oldest age conservatively', () => {
    const park: PublicLiveTarget = createTarget({ targetId: 'park', ageSeconds: null });
    const recent: PublicLiveTarget = createTarget({
      targetId: 'recent',
      ageSeconds: 60,
      sourceId: 'source-a'
    });
    const older: PublicLiveTarget = createTarget({
      targetId: 'older',
      ageSeconds: 600,
      sourceId: 'source-a'
    });

    expect(resolvePublicLiveAttributionSources(park, [recent, older])).toHaveLength(1);
    expect(resolvePublicLiveAttributionSources(park, [recent, older])[0].attributionText)
      .toBe('Powered by source-a');
    expect(resolvePublicLiveFreshnessReference(park, [recent, older]).targetId).toBe('older');
  });

  it('keeps a just-received child as a known freshness reference when the park age is absent', () => {
    const park: PublicLiveTarget = createTarget({ targetId: 'park', ageSeconds: null });
    const justReceived: PublicLiveTarget = createTarget({ targetId: 'child', ageSeconds: 0 });

    expect(resolvePublicLiveFreshnessReference(park, [justReceived]).targetId).toBe('child');
  });

  it('does not replace an unknown standby wait with another queue or show a closed queue value', () => {
    const unknownStandby: PublicLiveTarget = {
      ...createTarget({ waitTimeMinutes: null }),
      queues: [
        ...createTarget({ waitTimeMinutes: null }).queues,
        { ...createTarget({ waitTimeMinutes: 5 }).queues[0], kind: 'SingleRider' }
      ]
    };
    const closedQueue: PublicLiveTarget = {
      ...createTarget({ waitTimeMinutes: 10 }),
      queues: [{ ...createTarget({ waitTimeMinutes: 10 }).queues[0], availability: 'Closed' }]
    };

    expect(resolvePublicLiveWaitMinutes(unknownStandby)).toBeNull();
    expect(resolvePublicLiveWaitMinutes(closedQueue)).toBeNull();
  });
});

interface TargetOverrides {
  readonly targetId?: string;
  readonly availability?: PublicLiveTarget['availability'];
  readonly status?: PublicLiveTarget['status'];
  readonly waitTimeMinutes?: number | null;
  readonly ageSeconds?: number | null;
  readonly sourceId?: string;
}

function createTarget(overrides: TargetOverrides = {}): PublicLiveTarget {
  return {
    targetId: overrides.targetId ?? 'item-1',
    targetType: 'ParkItem',
    displayName: 'Example attraction',
    parkId: 'park-1',
    parkDisplayName: 'Example park',
    availability: overrides.availability ?? 'Current',
    status: overrides.status === undefined ? 'Open' : overrides.status,
    queues: [{
      kind: 'Standby',
      waitTimeMinutes: overrides.waitTimeMinutes === undefined ? 10 : overrides.waitTimeMinutes,
      isEstimated: false,
      availability: 'Available',
      returnStartUtc: null,
      returnEndUtc: null,
      currentGroupStart: null,
      currentGroupEnd: null,
      nextAllocationUtc: null,
      priceMinorUnits: null,
      currencyCode: null
    }],
    asOfUtc: '2026-09-29T10:00:00Z',
    observedAtUtc: '2026-09-29T09:59:00Z',
    receivedAtUtc: '2026-09-29T09:59:01Z',
    ageSeconds: overrides.ageSeconds === undefined ? 60 : overrides.ageSeconds,
    freshness: 'Fresh',
    expiresAtUtc: '2026-09-29T10:04:00Z',
    source: overrides.sourceId ? {
      id: overrides.sourceId,
      displayName: overrides.sourceId,
      type: 'AuthorizedAggregator',
      attributionText: `Powered by ${overrides.sourceId}`,
      attributionUrl: `https://${overrides.sourceId}.example/`
    } : null,
    confidence: 'High'
  };
}
