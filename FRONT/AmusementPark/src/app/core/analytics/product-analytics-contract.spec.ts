import {
  PRODUCT_ANALYTICS_EVENT_CONTRACTS,
  PRODUCT_ANALYTICS_SCHEMA_VERSION,
  serializeProductAnalyticsEvent
} from './product-analytics-contract';

describe('product analytics contract', () => {
  it('serializes a passport event with its schema version and bounded properties', () => {
    expect(
      serializeProductAnalyticsEvent({
        type: 'ride_occurrence_added',
        source: 'authenticated',
        countBucket: 'two-to-five'
      })
    ).toEqual({
      schemaVersion: PRODUCT_ANALYTICS_SCHEMA_VERSION,
      family: 'passport',
      channel: 'matomo-consented',
      route: 'passport',
      category: 'Passport',
      actionName: 'Passport product event',
      action: 'ride_occurrence_added',
      label: 'schema-version=1;source=authenticated;count-bucket=two-to-five'
    });
  });

  it('routes share events through the same contract', () => {
    const payload = serializeProductAnalyticsEvent({
      type: 'share_opened',
      recapType: 'profile-comparison'
    });

    expect(payload?.family).toBe('share');
    expect(payload?.route).toBe('share');
    expect(payload?.label).toBe('schema-version=1;recap-type=profile-comparison');
  });

  it('covers the eight product families with an exhaustive event vocabulary', () => {
    const families: string[] = [
      ...new Set(
        Object.values(PRODUCT_ANALYTICS_EVENT_CONTRACTS).map(
          (contract): string => contract.family
        )
      )
    ].sort();

    expect(Object.keys(PRODUCT_ANALYTICS_EVENT_CONTRACTS)).toHaveLength(54);
    expect(families).toEqual([
      'fit',
      'history',
      'live',
      'passport',
      'rank',
      'share',
      'trip',
      'watch'
    ]);
  });

  it('keeps first-party and technical events outside the Matomo channel', () => {
    expect(
      serializeProductAnalyticsEvent({ type: 'park_fit_search_started' })?.channel
    ).toBe('first-party');
    expect(
      serializeProductAnalyticsEvent({
        type: 'duplicate_delivery_prevented',
        deliveryChannel: 'email'
      })?.channel
    ).toBe('technical');
  });

  it('rejects an undeclared property instead of leaking it into Matomo', () => {
    expect(
      serializeProductAnalyticsEvent({
        type: 'passport_opened',
        source: 'authenticated',
        visitId: 'private-identifier'
      })
    ).toBeNull();
  });

  it('rejects missing, unknown and invalid property values', () => {
    expect(serializeProductAnalyticsEvent({ type: 'passport_opened' })).toBeNull();
    expect(
      serializeProductAnalyticsEvent({ type: 'passport_opened', source: 'unexpected' })
    ).toBeNull();
    expect(
      serializeProductAnalyticsEvent({ type: 'invented_event', source: 'authenticated' })
    ).toBeNull();
  });

  it('accepts only short public method versions', () => {
    expect(
      serializeProductAnalyticsEvent({
        type: 'live_forecast_method_opened',
        methodVersion: 'live-forecast-2026-01'
      })
    ).not.toBeNull();
    expect(
      serializeProductAnalyticsEvent({
        type: 'live_forecast_method_opened',
        methodVersion: 'private/hash/42'
      })
    ).toBeNull();
  });
});
