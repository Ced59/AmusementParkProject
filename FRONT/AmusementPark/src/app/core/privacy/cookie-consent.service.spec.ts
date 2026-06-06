import { CookieConsentService } from './cookie-consent.service';

describe('CookieConsentService', () => {
  const storageKey: string = 'amusementpark.cookie-consent.v1';
  const legacyStorageKey: string = 'amusementpark.analytics-consent.v1';

  beforeEach(() => {
    window.localStorage.removeItem(storageKey);
    window.localStorage.removeItem(legacyStorageKey);
  });

  afterEach(() => {
    window.localStorage.removeItem(storageKey);
    window.localStorage.removeItem(legacyStorageKey);
  });

  it('starts with no decision and shows the banner in the browser when consent is required', () => {
    const service = new CookieConsentService('browser' as unknown as object);

    expect(service.decision()).toBeNull();
    expect(service.hasAcceptedOptionalCookies()).toBeFalse();
    expect(service.isBannerVisible()).toBeTrue();
  });

  it('stores an accepted optional cookie decision', () => {
    const service = new CookieConsentService('browser' as unknown as object);

    service.acceptOptionalCookies();

    expect(service.decision()).toBe('accepted');
    expect(service.hasAcceptedOptionalCookies()).toBeTrue();
    expect(service.isBannerVisible()).toBeFalse();
    expect(JSON.parse(window.localStorage.getItem(storageKey) ?? '{}').decision).toBe('accepted');
  });

  it('stores a refused decision when the user continues with necessary cookies only', () => {
    const service = new CookieConsentService('browser' as unknown as object);

    service.continueWithNecessaryCookiesOnly();

    expect(service.decision()).toBe('refused');
    expect(service.hasAcceptedOptionalCookies()).toBeFalse();
    expect(JSON.parse(window.localStorage.getItem(storageKey) ?? '{}').decision).toBe('refused');
  });

  it('migrates a legacy analytics consent value and removes the old key', () => {
    window.localStorage.setItem(legacyStorageKey, JSON.stringify({ decision: 'accepted' }));

    const service = new CookieConsentService('browser' as unknown as object);

    expect(service.decision()).toBe('accepted');
    expect(window.localStorage.getItem(legacyStorageKey)).toBeNull();
    expect(JSON.parse(window.localStorage.getItem(storageKey) ?? '{}').decision).toBe('accepted');
  });

  it('removes malformed stored JSON and keeps the decision empty', () => {
    window.localStorage.setItem(storageKey, '{broken-json');

    const service = new CookieConsentService('browser' as unknown as object);

    expect(service.decision()).toBeNull();
    expect(window.localStorage.getItem(storageKey)).toBeNull();
  });

  it('resets both current and legacy choices', () => {
    window.localStorage.setItem(storageKey, JSON.stringify({ decision: 'accepted' }));
    window.localStorage.setItem(legacyStorageKey, JSON.stringify({ decision: 'refused' }));
    const service = new CookieConsentService('browser' as unknown as object);

    service.resetCookieChoice();

    expect(service.decision()).toBeNull();
    expect(window.localStorage.getItem(storageKey)).toBeNull();
    expect(window.localStorage.getItem(legacyStorageKey)).toBeNull();
  });

  it('does not access browser storage on the server platform', () => {
    window.localStorage.setItem(storageKey, JSON.stringify({ decision: 'accepted' }));

    const service = new CookieConsentService('server' as unknown as object);
    service.acceptOptionalCookies();
    service.resetCookieChoice();

    expect(service.decision()).toBeNull();
    expect(window.localStorage.getItem(storageKey)).not.toBeNull();
  });
});
