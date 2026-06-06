import { AnalyticsConsentService } from './analytics-consent.service';

describe('AnalyticsConsentService', () => {
  const storageKey: string = 'amusementpark.analytics-consent.v1';

  beforeEach(() => {
    window.localStorage.removeItem(storageKey);
  });

  afterEach(() => {
    window.localStorage.removeItem(storageKey);
  });

  it('starts undecided and exposes banner visibility in the browser', () => {
    const service = new AnalyticsConsentService('browser' as unknown as object);

    expect(service.decision()).toBeNull();
    expect(service.hasAcceptedAnalytics()).toBeFalse();
    expect(service.isBannerVisible()).toBeTrue();
  });

  it('stores accepted analytics consent', () => {
    const service = new AnalyticsConsentService('browser' as unknown as object);

    service.acceptAnalytics();

    expect(service.decision()).toBe('accepted');
    expect(service.hasAcceptedAnalytics()).toBeTrue();
    expect(JSON.parse(window.localStorage.getItem(storageKey) ?? '{}').decision).toBe('accepted');
  });

  it('stores refused consent and hides the banner', () => {
    const service = new AnalyticsConsentService('browser' as unknown as object);

    service.continueWithoutAnalytics();

    expect(service.decision()).toBe('refused');
    expect(service.hasAcceptedAnalytics()).toBeFalse();
    expect(service.isBannerVisible()).toBeFalse();
  });

  it('restores accepted and refused decisions from local storage', () => {
    window.localStorage.setItem(storageKey, JSON.stringify({ decision: 'accepted', decidedAt: '2026-01-01T00:00:00Z' }));
    expect(new AnalyticsConsentService('browser' as unknown as object).decision()).toBe('accepted');

    window.localStorage.setItem(storageKey, JSON.stringify({ decision: 'refused', decidedAt: '2026-01-01T00:00:00Z' }));
    expect(new AnalyticsConsentService('browser' as unknown as object).decision()).toBe('refused');
  });

  it('removes malformed stored consent values', () => {
    window.localStorage.setItem(storageKey, 'not-json');

    const service = new AnalyticsConsentService('browser' as unknown as object);

    expect(service.decision()).toBeNull();
    expect(window.localStorage.getItem(storageKey)).toBeNull();
  });

  it('resets the current choice', () => {
    const service = new AnalyticsConsentService('browser' as unknown as object);
    service.acceptAnalytics();

    service.resetAnalyticsChoice();

    expect(service.decision()).toBeNull();
    expect(window.localStorage.getItem(storageKey)).toBeNull();
  });

  it('does nothing on the server platform', () => {
    const service = new AnalyticsConsentService('server' as unknown as object);

    service.acceptAnalytics();
    service.continueWithoutAnalytics();

    expect(service.decision()).toBeNull();
    expect(window.localStorage.getItem(storageKey)).toBeNull();
  });
});
